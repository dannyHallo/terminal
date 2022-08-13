using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Jobs;

public struct BakeJob : IJobParallelFor
{
    private NativeArray<int> meshIds;

    public BakeJob(NativeArray<int> meshIds)
    {
        this.meshIds = meshIds;
    }

    public void Execute(int index)
    {
        Physics.BakeMesh(meshIds[index], false);
    }
}

[ExecuteInEditMode]
[RequireComponent(typeof(NoiseDensity))]
[RequireComponent(typeof(ModelGrass))]
[RequireComponent(typeof(ColourGenerator2D))]
public class TerrainMesh : MonoBehaviour
{
    const int threadGroupSize = 8;
    private NoiseDensity noiseDensity
    {
        get
        {
            return gameObject.GetComponent<NoiseDensity>();
        }
    }

    public ModelGrass modelGrass;
    public AudioProcessor audioProcessor;
    public float windWeight;


    [Header("General Settings")]
    public bool fixedMapSize;

    // Nums of chunks to generate
    // Show this variable when fixedMapSize is true
    public Vector3Int numChunks = Vector3Int.one;
    public Transform viewer;

    public ChunkMeshProperty chunkMeshProperty;
    public ComputeShader shader;
    public Material mat;

    // [Header("Gizmos")]
    private Color boundsGizmoCol = Color.white;

    private bool showBoundsGizmo = true;
    public bool generateColliders = true;
    private string chunkHolderName = "ChunkHolder";

    GameObject chunkHolder;
    List<Chunk> chunks;
    List<Chunk> activeChunks;
    List<Chunk> chunksNeedsToUpdateCollider;
    Dictionary<Vector3Int, Chunk> existingChunks;
    List<Vector3Int> chunkCoordsNeededToBeRendered;
    Dictionary<Vector3Int, float[]> existingChunkVolumeData;
    Queue<Chunk> recycleableChunks;

    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer additionalPointsBuffer;
    ComputeBuffer triCountBuffer;

    [Range(0, 1.0f)] public float meshPaintingFac = 0.1f;
    private float meshPaintingFinal = 0.1f;

    bool settingsUpdated;
    int maxChunksInViewHoriDisappear;
    int maxChunksInViewVertDisappear;
    int maxChunksInViewHori;
    int maxChunksInViewVert;

    [Header("Debug")]
    public bool editorUpdate = false;

    [System.Serializable]
    public struct ChunkMeshProperty
    {
        public int numPointsPerAxis;
        public int numGrassesPerAxis;
        public float boundSize;
    }

    public int viewDistanceHori;
    public int viewDistanceVert;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            DestroyOldChunks();
            InitVariableChunkStructures();
            PrecalculateChunkBounds();

            if (fixedMapSize)
            {
                CreateBuffers();
                LoadBoundedChunks();
            }
        }
    }
    private const MeshUpdateFlags MESH_UPDATE_FLAGS =
    MeshUpdateFlags.DontValidateIndices |
    MeshUpdateFlags.DontNotifyMeshUsers |
    MeshUpdateFlags.DontRecalculateBounds |
    MeshUpdateFlags.DontResetBoneBounds;

    void DestroyOldChunks()
    {
        var oldChunks = FindObjectsOfType<Chunk>(true);
        for (int i = 0; i < oldChunks.Length; i++)
        {
            oldChunks[i].DestroyAndClearBuffer();
        }
    }

    void ReleaseExistingChunkBuffers()
    {
        var oldChunks = FindObjectsOfType<Chunk>(true);
        for (int i = 0; i < oldChunks.Length; i++)
        {
            oldChunks[i].FreeBuffers();
        }
    }

    private void Update()
    {
        // Playing update
        if (Application.isPlaying)
            RuntimeUpdatePerFrame();

        // Editor update
        else if (settingsUpdated)
        {
            if (!Application.isPlaying)
                RequestMeshUpdate();

            settingsUpdated = false;
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;

        // DRAW GRASS THIS FRAME
        modelGrass.DrawAllGrass(activeChunks, audioProcessor.loudness * windWeight);

        // GENERATE COLLIDERS
        if (chunksNeedsToUpdateCollider.Count != 0)
        {
            NativeArray<int> meshIds = new NativeArray<int>(chunksNeedsToUpdateCollider.Count, Allocator.TempJob);
            for (int i = 0; i < meshIds.Length; ++i)
            {
                meshIds[i] = chunksNeedsToUpdateCollider[i].GetMeshInstanceId();
            }
            var job = new BakeJob(meshIds);
            job.Schedule(meshIds.Length, 1).Complete(); // Each core will update one collider
            meshIds.Dispose();

            foreach (Chunk c in chunksNeedsToUpdateCollider)
            {
                c.UpdateColliderSharedMesh();
            }
            chunksNeedsToUpdateCollider.Clear();
        }
    }

    private void RuntimeUpdatePerFrame()
    {

    }

    public void RequestMeshUpdate()
    {
        ReleaseBuffers();
        CreateBuffers();

        InitChunks();
        UpdateAllChunks();

        ReleaseBuffers();
    }

    void InitVariableChunkStructures()
    {
        recycleableChunks = new Queue<Chunk>();
        chunks = new List<Chunk>();
        activeChunks = new List<Chunk>();
        chunksNeedsToUpdateCollider = new List<Chunk>();
        chunkCoordsNeededToBeRendered = new List<Vector3Int>();
        existingChunks = new Dictionary<Vector3Int, Chunk>();
        existingChunkVolumeData = new Dictionary<Vector3Int, float[]>();
    }

    /// <returns>The chunk the viewer is inside</returns>
    private Vector3Int GetViewerCoord()
    {
        Vector3 p = viewer.position;
        Vector3 ps = p / chunkMeshProperty.boundSize;

        // Indicates which chunk the viewer in in
        Vector3Int viewerCoord = new Vector3Int(
            Mathf.RoundToInt(ps.x),
            Mathf.RoundToInt(ps.y),
            Mathf.RoundToInt(ps.z)
        );

        return viewerCoord;
    }

    private void PrecalculateChunkBounds()
    {
        maxChunksInViewHori = Mathf.CeilToInt(viewDistanceHori / chunkMeshProperty.boundSize);
        maxChunksInViewVert = Mathf.CeilToInt(viewDistanceVert / chunkMeshProperty.boundSize);

        // maxChunksInViewHoriMustLoad = Mathf.CeilToInt(lodSetup.viewDistanceHori * 0.5f / boundSize);
        // maxChunksInViewVertMustLoad = Mathf.CeilToInt(lodSetup.viewDistanceVert * 0.5f / boundSize);

        maxChunksInViewHoriDisappear = Mathf.CeilToInt(
            viewDistanceHori * 1.5f / chunkMeshProperty.boundSize
        );
        maxChunksInViewVertDisappear = Mathf.CeilToInt(
            viewDistanceVert * 1.5f / chunkMeshProperty.boundSize
        );
    }

    // INFINITE MAP
    void UpdateSurroundingChunks()
    {
        if (chunks == null)
            return;

        CreateChunkHolderIfNeeded();

        Vector3Int viewerCoord = GetViewerCoord();

        // Kick existing chunks out of range
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            Chunk chunk = chunks[i];
            Vector3Int chunkCoord = chunk.coord;

            if (Mathf.Abs(viewerCoord.x - chunkCoord.x) > maxChunksInViewHoriDisappear ||
                Mathf.Abs(viewerCoord.y - chunkCoord.y) > maxChunksInViewVertDisappear ||
                Mathf.Abs(viewerCoord.z - chunkCoord.z) > maxChunksInViewHoriDisappear)
            {
                existingChunks.Remove(chunk.coord);
                chunk.DestroyAndClearBuffer();
                chunks.RemoveAt(i);
                if (activeChunks.Contains(chunk))
                {
                    activeChunks.Remove(chunk);
                }
            }
        }

        // Kick out-of-bound chunks from preloadig list
        for (int i = chunkCoordsNeededToBeRendered.Count - 1; i >= 0; i--)
        {
            Vector3Int currentCoord = chunkCoordsNeededToBeRendered[i];
            if (
                (
                    Mathf.Pow(currentCoord.x - viewerCoord.x, 2)
                    + Mathf.Pow(currentCoord.z - viewerCoord.z, 2)
                ) > Mathf.Pow(maxChunksInViewHori, 2)
            )
            {
                chunkCoordsNeededToBeRendered.Remove(currentCoord);
            }
        }
        int tryCount = 0;

        for (int xzBound = 0; xzBound <= maxChunksInViewHori; xzBound++)
        {
            int yBound = maxChunksInViewVert;
            if (AllBoundCornersAreLoaded(xzBound, yBound))
                continue;
            for (int x = -xzBound; x <= xzBound; x++)
            {
                for (int z = -xzBound; z <= xzBound; z++)
                {
                    for (int y = -yBound; y <= yBound; y++)
                    {
                        Vector3Int coord = new Vector3Int(
                            x + viewerCoord.x,
                            y + viewerCoord.y,
                            z + viewerCoord.z
                        );

                        // Keep the chunk unchanged
                        if (existingChunks.ContainsKey(coord))
                            continue;

                        if ((RenderChunk(coord) != 0) || tryCount > 10)
                            goto renderedOnce;

                        tryCount++;
                    }
                }
            }
        }
    renderedOnce:
        return;
    }

    // INFINITE MAP GEN STRATAGY
    private bool AllBoundCornersAreLoaded(int xzBound, int yBound)
    {
        Vector3Int viewerCoord = GetViewerCoord();
        Vector3Int coord;
        coord = new Vector3Int(
            xzBound + viewerCoord.x,
            yBound + viewerCoord.y,
            xzBound + viewerCoord.z
        );
        if (!existingChunks.ContainsKey(coord))
        {
            return false;
        }

        return true;
    }

    // INFINITE MAP RENDER STRATAGY
    private int RenderChunk(Vector3Int coord)
    {
        if (existingChunks.ContainsKey(coord))
        {
            print("Try to render " + coord + " but it already exists! ");
            return 0;
        }

        int numPoints =
            chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis;

        Chunk chunk = CreateChunk(coord);
        chunk.BindMaterialAndCollider(mat, generateColliders);

        additionalPointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
        if (!existingChunkVolumeData.ContainsKey(coord))
        {
            float[] chunkVolumeData = new float[numPoints];
            additionalPointsBuffer.SetData(chunkVolumeData);
        }
        else
        {
            additionalPointsBuffer.SetData(existingChunkVolumeData[coord]);
        }

        int chunkUseFlag = 0;
        chunkUseFlag += UpdateChunkMesh(chunk, additionalPointsBuffer, true);
        additionalPointsBuffer.Release();

        existingChunks.Add(coord, chunk);
        chunks.Add(chunk);
        if (chunkUseFlag == 1)
            activeChunks.Add(chunk);

        return chunkUseFlag;
    }

    private void LoadBoundedChunks()
    {
        if (chunks == null)
            return;

        CreateChunkHolderIfNeeded();

        int fixedChunksHori = numChunks.x;
        int fixedChunksVert = numChunks.y;

        for (int x = 0; x < fixedChunksHori; x++)
        {
            for (int y = 0; y < fixedChunksVert; y++)
            {
                for (int z = 0; z < fixedChunksHori; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);

                    // Keep the chunk unchanged

                    if (existingChunks.ContainsKey(coord))
                        continue;

                    int numPoints =
                        chunkMeshProperty.numPointsPerAxis
                        * chunkMeshProperty.numPointsPerAxis
                        * chunkMeshProperty.numPointsPerAxis;

                    Chunk chunk = CreateChunk(coord);
                    chunk.BindMaterialAndCollider(mat, generateColliders);

                    existingChunks.Add(coord, chunk);
                    chunks.Add(chunk);

                    additionalPointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
                    if (!existingChunkVolumeData.ContainsKey(coord))
                    {
                        float[] chunkVolumeData = new float[numPoints];
                        additionalPointsBuffer.SetData(chunkVolumeData);
                    }
                    else
                    {
                        additionalPointsBuffer.SetData(existingChunkVolumeData[coord]);
                    }

                    int chunkUseFlag = 0;
                    chunkUseFlag = UpdateChunkMesh(chunk, additionalPointsBuffer, true);
                    additionalPointsBuffer.Release();

                    if (chunkUseFlag == 1)
                        activeChunks.Add(chunk);
                }
            }
        }
    }

    void ChangeVolumeData(Vector3Int chunkCoord, int id, float rangeFactor)
    {
        // Error Handler: Chunk does not exist
        if (!existingChunks.ContainsKey(chunkCoord))
            return;

        if (!existingChunkVolumeData.ContainsKey(chunkCoord))
        {
            int numPoints =
                chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis;
            float[] chunkVolumeData = new float[numPoints];
            existingChunkVolumeData.Add(chunkCoord, chunkVolumeData);
        }
        existingChunkVolumeData[chunkCoord][id] += meshPaintingFinal * rangeFactor;
    }

    public void DrawOnChunk(
        Vector3 hitPoint,   // The directional hit piont
        int range,          // Affact range
        float strength,
        int drawType
    ) // 0: dig, 1: add
    {
        if (drawType == 0)
        {
            meshPaintingFinal = -Mathf.Abs(meshPaintingFac * strength);
        }
        else if (drawType == 1)
        {
            meshPaintingFinal = Mathf.Abs(meshPaintingFac * strength);
        }

        int numPointsPerAxis = chunkMeshProperty.numPointsPerAxis;
        List<Vector3Int> chunksNeedToBeUpdated = new List<Vector3Int>();
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        Vector3 ps = hitPoint / chunkMeshProperty.boundSize;

        Vector3Int originalHittingCoord = new Vector3Int(
            Mathf.RoundToInt(ps.x),
            Mathf.RoundToInt(ps.y),
            Mathf.RoundToInt(ps.z)
        );
        Vector3 centre = CentreFromCoord(originalHittingCoord);
        float pointSpacing = chunkMeshProperty.boundSize / (numPointsPerAxis - 1);
        // The exact chunk the player is drawing at

        // Original hit chunk id
        Vector3Int IdVector = new Vector3Int(
            Mathf.RoundToInt(((hitPoint - centre).x + chunkMeshProperty.boundSize / 2) / pointSpacing),
            Mathf.RoundToInt(((hitPoint - centre).y + chunkMeshProperty.boundSize / 2) / pointSpacing),
            Mathf.RoundToInt(((hitPoint - centre).z + chunkMeshProperty.boundSize / 2) / pointSpacing)
        );

        // Create a cube region of vectors
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                for (int z = -range; z <= range; z++)
                {
                    // Chunk id with offset
                    Vector3Int idInThisChunk = new Vector3Int(
                        ((IdVector.x + x) + (numPointsPerAxis - 1)) % (numPointsPerAxis - 1),
                        ((IdVector.y + y) + (numPointsPerAxis - 1)) % (numPointsPerAxis - 1),
                        ((IdVector.z + z) + (numPointsPerAxis - 1)) % (numPointsPerAxis - 1)
                    );

                    // Chunk offset vector
                    Vector3Int chunkOffset = new Vector3Int(
                        (int)
                            Mathf.Floor(
                                ((float)(IdVector.x + x) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)
                            ) - 1,
                        (int)
                            Mathf.Floor(
                                ((float)(IdVector.y + y) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)
                            ) - 1,
                        (int)
                            Mathf.Floor(
                                ((float)(IdVector.z + z) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)
                            ) - 1
                    );

                    float rangeFactor =
                        range - Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2) + Mathf.Pow(z, 2));

                    if (rangeFactor >= 0)
                    {
                        Vector3Int currentProcessingCoord =
                            originalHittingCoord + chunkOffset;

                        if (!existingChunks.ContainsKey(currentProcessingCoord))
                        {
                            continue;
                        }

                        // Add chunk in update list
                        if (!chunksNeedToBeUpdated.Contains(currentProcessingCoord))
                        {
                            chunksNeedToBeUpdated.Add(currentProcessingCoord);
                        }

                        int currentId = PosToIndex(idInThisChunk);

                        // On 8 vertexs of a cube
                        if (
                            idInThisChunk.x == 0
                            && idInThisChunk.y == 0
                            && idInThisChunk.z == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    numPointsPerAxis - 1,
                                    numPointsPerAxis - 1,
                                    numPointsPerAxis - 1
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, -1, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == numPointsPerAxis - 1
                            && idInThisChunk.y == 0
                            && idInThisChunk.z == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(0, numPointsPerAxis - 1, numPointsPerAxis - 1)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, -1, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == 0
                            && idInThisChunk.y == numPointsPerAxis - 1
                            && idInThisChunk.z == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(numPointsPerAxis - 1, 0, numPointsPerAxis - 1)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 1, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == 0
                            && idInThisChunk.y == 0
                            && idInThisChunk.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(numPointsPerAxis - 1, numPointsPerAxis - 1, 0)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, -1, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == numPointsPerAxis - 1
                            && idInThisChunk.y == numPointsPerAxis - 1
                            && idInThisChunk.z == 0
                        )
                        {
                            int id = PosToIndex(new Vector3(0, 0, numPointsPerAxis - 1));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 1, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == numPointsPerAxis - 1
                            && idInThisChunk.y == 0
                            && idInThisChunk.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(0, numPointsPerAxis - 1, 0));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, -1, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == 0
                            && idInThisChunk.y == numPointsPerAxis - 1
                            && idInThisChunk.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, 0, 0));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 1, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == numPointsPerAxis - 1
                            && idInThisChunk.y == numPointsPerAxis - 1
                            && idInThisChunk.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(0, 0, 0));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 1, 1),
                                id,
                                rangeFactor
                            );
                        }

                        // On 12 edges of a cube
                        if (idInThisChunk.y == 0 && idInThisChunk.z == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    idInThisChunk.x,
                                    numPointsPerAxis - 1,
                                    numPointsPerAxis - 1
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, -1, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.y == 0
                            && idInThisChunk.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(idInThisChunk.x, numPointsPerAxis - 1, 0)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, -1, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.y == numPointsPerAxis - 1
                            && idInThisChunk.z == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(idInThisChunk.x, 0, numPointsPerAxis - 1)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 1, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.y == numPointsPerAxis - 1
                            && idInThisChunk.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(idInThisChunk.x, 0, 0));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 1, 1),
                                id,
                                rangeFactor
                            );
                        }

                        if (idInThisChunk.x == 0 && idInThisChunk.y == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    numPointsPerAxis - 1,
                                    numPointsPerAxis - 1,
                                    idInThisChunk.z
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, -1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == 0
                            && idInThisChunk.y == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(numPointsPerAxis - 1, 0, idInThisChunk.z)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == numPointsPerAxis - 1
                            && idInThisChunk.y == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(0, numPointsPerAxis - 1, idInThisChunk.z)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, -1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == numPointsPerAxis - 1
                            && idInThisChunk.y == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(0, 0, idInThisChunk.z));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 1, 0),
                                id,
                                rangeFactor
                            );
                        }

                        if (idInThisChunk.x == 0 && idInThisChunk.z == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    numPointsPerAxis - 1,
                                    idInThisChunk.y,
                                    numPointsPerAxis - 1
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 0, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == 0
                            && idInThisChunk.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(numPointsPerAxis - 1, idInThisChunk.y, 0)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 0, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == numPointsPerAxis - 1
                            && idInThisChunk.z == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(0, idInThisChunk.y, numPointsPerAxis - 1)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 0, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            idInThisChunk.x == numPointsPerAxis - 1
                            && idInThisChunk.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(0, idInThisChunk.y, 0));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 0, 1),
                                id,
                                rangeFactor
                            );
                        }

                        // On 6 faces of a cube
                        if (idInThisChunk.x == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    numPointsPerAxis - 1,
                                    idInThisChunk.y,
                                    idInThisChunk.z
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 0, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (idInThisChunk.x == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(
                                new Vector3(0, idInThisChunk.y, idInThisChunk.z)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 0, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (idInThisChunk.z == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    idInThisChunk.x,
                                    idInThisChunk.y,
                                    numPointsPerAxis - 1
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 0, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (idInThisChunk.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(
                                new Vector3(idInThisChunk.x, idInThisChunk.y, 0)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 0, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (idInThisChunk.y == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    idInThisChunk.x,
                                    numPointsPerAxis - 1,
                                    idInThisChunk.z
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, -1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (idInThisChunk.y == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(
                                new Vector3(idInThisChunk.x, 0, idInThisChunk.z)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        ChangeVolumeData(currentProcessingCoord, currentId, rangeFactor);
                    }
                }
            }
        }
        additionalPointsBuffer = new ComputeBuffer(
            numPointsPerAxis * numPointsPerAxis * numPointsPerAxis,
            sizeof(float)
        );

        // Mesh will be updated more than once if chunk edge is met
        for (int i = 0; i < chunksNeedToBeUpdated.Count; i++)
        {
            additionalPointsBuffer.SetData(existingChunkVolumeData[chunksNeedToBeUpdated[i]]);
            UpdateChunkMesh(existingChunks[chunksNeedToBeUpdated[i]], additionalPointsBuffer, false);
        }
        // print("chunksNeedToBeUpdated.Count = " + chunksNeedToBeUpdated.Count);

        additionalPointsBuffer.Release();
    }

    /// <summary>
    /// Update chunk mesh by the chunk's position
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="additionalPointsBuffer"></param>
    /// <returns>0: Inactive chunk, 1: Active chunk</returns>
    public int UpdateChunkMesh(Chunk chunk, ComputeBuffer additionalPointsBuffer, bool generateColliderNow)
    {
        int numVoxelsPerAxis = chunkMeshProperty.numPointsPerAxis - 1;
        float isoLevel = 0;
        // A thread contains several mini threads
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        float meshPointSpacing = chunkMeshProperty.boundSize / (chunkMeshProperty.numPointsPerAxis - 1);

        Vector3Int coord = chunk.coord;
        Vector3 centre = chunk.centre;

        // Indecator of the points are full or empty
        int[] pointStatusData = new int[2];
        pointStatusData[0] = 0;
        pointStatusData[1] = 0;
        ComputeBuffer pointsStatus = new ComputeBuffer(2, sizeof(int));
        pointsStatus.SetData(pointStatusData);

        Vector3 worldSize = new Vector3(numChunks.x, numChunks.y, numChunks.z) * chunkMeshProperty.boundSize;

        // Gerenate individual noise value using compute shader， modifies pointsBuffer
        noiseDensity.CalculateChunkNoise(
            chunk,
            pointsBuffer,
            additionalPointsBuffer,
            pointsStatus,
            chunkMeshProperty.numPointsPerAxis,
            chunkMeshProperty.boundSize,
            centre,
            meshPointSpacing,
            isoLevel,
            worldSize
        );
        pointsStatus.GetData(pointStatusData);
        pointsStatus.Release();

        // If all empty/ all full
        if (pointStatusData[0] == 0 || pointStatusData[1] == 0)
        {
            chunk.gameObject.SetActive(false);
            return 0;
        }
        else if (!chunk.gameObject.activeInHierarchy)
            chunk.gameObject.SetActive(true);

        triangleBuffer.SetCounterValue(0);
        shader.SetBuffer(0, "points", pointsBuffer);
        shader.SetBuffer(0, "triangles", triangleBuffer);
        shader.SetInt("numPointsPerAxis", chunkMeshProperty.numPointsPerAxis);
        shader.SetFloat("isoLevel", isoLevel);

        shader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        Mesh mesh = chunk.mesh;
        mesh.Clear();

        Vector3[] vertices = new Vector3[numTris * 3];

        int[] meshTriangles = new int[numTris * 3];

        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < numTris; i++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals(MESH_UPDATE_FLAGS);

        if (generateColliderNow)
            chunk.UpdateColliderSharedMesh();
        else
            chunksNeedsToUpdateCollider.Add(chunk);

        if (Application.isPlaying)
        {
            modelGrass.CalculateChunkGrassPosition(chunk);
        }

        return 1;
    }

    // Editor Preview
    public void UpdateAllChunks()
    {
        additionalPointsBuffer = new ComputeBuffer(
            chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis,
            sizeof(float)
        );
        additionalPointsBuffer.SetData(
            new float[
                chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis
            ]
        );

        // Create mesh for each chunk
        foreach (Chunk chunk in chunks)
        {
            UpdateChunkMesh(chunk, additionalPointsBuffer, true);
        }
        additionalPointsBuffer.Release();
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    void CreateBuffers()
    {
        int numPoints;
        int numVoxelsPerAxis;
        int numVoxels;
        int maxTriangleCount;

        if (!Application.isPlaying || triangleBuffer == null)
        {
            // if (Application.isPlaying)
            //     ReleaseBuffers();

            numVoxelsPerAxis = Application.isPlaying
                ? chunkMeshProperty.numPointsPerAxis - 1
                : chunkMeshProperty.numPointsPerAxis - 1;

            numPoints = Application.isPlaying
                ? (int)Mathf.Pow(chunkMeshProperty.numPointsPerAxis, 3)
                : (int)Mathf.Pow(chunkMeshProperty.numPointsPerAxis, 3);
            // Voxels(mini cubes) in a volume
            numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            // Max triangles to be create per voxel is 5
            maxTriangleCount = numVoxels * 5;

            // ComputeBuffer(Num of elements, size per element, type of the buffer)
            // 3 points, x, y, z, stored in float
            triangleBuffer = new ComputeBuffer(
                maxTriangleCount,
                sizeof(float) * 3 * 3,
                ComputeBufferType.Append
            );
            // stores x, y, z, volumeValue
            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            // A int to store total count of triangles
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }
    }

    void ReleaseBuffers()
    {
        // print("Buffer released");

        modelGrass.ClearGrassBufferIfNeeded();
        ReleaseExistingChunkBuffers();

        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();

            triangleBuffer = null;
            pointsBuffer = null;
            triangleBuffer = null;
        }
    }

    Vector3 CentreFromCoord(Vector3Int coord)
    {
        // Centre entire map at origin
        if (fixedMapSize || !Application.isPlaying)
        {
            Vector3 totalBounds = (Vector3)numChunks * chunkMeshProperty.boundSize;
            return -totalBounds / 2 + (Vector3)coord * chunkMeshProperty.boundSize + Vector3.one * chunkMeshProperty.boundSize / 2;
        }

        return new Vector3(coord.x, coord.y, coord.z) * chunkMeshProperty.boundSize;
    }

    void CreateChunkHolderIfNeeded()
    {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (chunkHolder == null)
        {
            if (GameObject.Find(chunkHolderName))
            {
                chunkHolder = GameObject.Find(chunkHolderName);
            }
            else
            {
                chunkHolder = new GameObject(chunkHolderName);
            }
        }
    }

    public static List<Chunk> FindChunkInChildWithTag(GameObject parent)
    {
        Transform t = parent.transform;
        List<Chunk> tempChunkList = new List<Chunk>();
        foreach (Transform tr in t)
        {
            if (tr.tag == "Chunk")
            {
                tempChunkList.Add(tr.GetComponent<Chunk>());
            }
        }
        return tempChunkList;
    }

    // Create/get references to all chunks
    void InitChunks()
    {
        // Create a folder
        CreateChunkHolderIfNeeded();

        if (chunks != null)
            chunks.Clear();
        else
            chunks = new List<Chunk>();

        if (activeChunks != null)
            activeChunks.Clear();
        else
            activeChunks = new List<Chunk>();

        DestroyOldChunks();

        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    var newChunk = CreateChunk(coord);
                    chunks.Add(newChunk);
                    chunks[chunks.Count - 1].BindMaterialAndCollider(mat, generateColliders);
                }
            }
        }
    }

    Chunk CreateChunk(Vector3Int coord)
    {
        GameObject chunk = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk>();

        // newChunk.FreeBuffers();
        newChunk.SetupConfig(
            numPointsPerAxis: chunkMeshProperty.numPointsPerAxis,
            numGrassesPerAxis: chunkMeshProperty.numGrassesPerAxis,
            coord: coord,
            centre: CentreFromCoord(coord),
            boundSize: chunkMeshProperty.boundSize,
            pointSpacing: chunkMeshProperty.boundSize / (chunkMeshProperty.numPointsPerAxis - 1));

        newChunk.CreateBuffers();

        return newChunk;
    }

    int PosToIndex(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        int z = Mathf.RoundToInt(pos.z);

        return z * chunkMeshProperty.numPointsPerAxis * chunkMeshProperty.numPointsPerAxis
            + y * chunkMeshProperty.numPointsPerAxis
            + x;
    }

    // Run every time settings in MeshGenerator changed
    void OnValidate()
    {
        if (editorUpdate && !Application.isPlaying)
        {
            settingsUpdated = true;
        }
    }

    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (showBoundsGizmo)
        {
            Gizmos.color = boundsGizmoCol;

            List<Chunk> chunks =
                (this.chunks == null) ? new List<Chunk>(FindObjectsOfType<Chunk>(false)) : this.chunks;
            foreach (var chunk in chunks)
            {
                Bounds bounds = new Bounds(CentreFromCoord(chunk.coord), Vector3.one * chunkMeshProperty.boundSize);
                Gizmos.color = boundsGizmoCol;
                Gizmos.DrawWireCube(CentreFromCoord(chunk.coord), Vector3.one * chunkMeshProperty.boundSize);
            }
        }
    }
}
