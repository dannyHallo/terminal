using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainMesh : MonoBehaviour
{
    const int threadGroupSize = 8;
    public NoiseDensity noiseDensity;
    public ModelGrass modelGrass;

    [Header("General Settings")]
    public bool fixedMapSize;
    public bool drawGrass;

    // Nums of chunks to generate
    // Show this variable when fixedMapSize is true
    public Vector3Int numChunks = Vector3Int.one;
    public Transform viewer;

    public LodSetup lodSetup;
    public ComputeShader shader;
    public Material mat;

    [Header("Voxel Settings")]
    public float boundSize = 20;
    Vector3 offset = Vector3.zero;

    // [Header("Gizmos")]
    private Color boundsGizmoCol = Color.white;

    private bool showBoundsGizmo = true;
    private bool generateColliders = true;
    private string chunkHolderName = "ChunkHolder";

    GameObject chunkHolder;
    List<Chunk> chunks;
    List<Chunk> activeChunks;
    Dictionary<Vector3Int, Chunk> existingChunks;
    List<Vector3Int> chunkCoordsNeededToBeRendered;
    Dictionary<Vector3Int, float[]> existingChunkVolumeData;
    Queue<Chunk> recycleableChunks;

    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer additionalPointsBuffer;
    ComputeBuffer triCountBuffer;

    float changeFactor = -0.6f;
    bool settingsUpdated;
    bool boundedMapGenerated = false;
    int maxChunksInViewHoriMustLoad;
    int maxChunksInViewVertMustLoad;
    int maxChunksInViewHoriDisappear;
    int maxChunksInViewVertDisappear;
    int maxChunksInViewHori;
    int maxChunksInViewVert;

    [System.Serializable]
    public struct LodSetup
    {
        [Header("8n")]
        public int numPointsPerAxis;
        public int viewDistanceHori;
        public int viewDistanceVert;
        public int fixedDistanceHori;
        public int fixedDistanceVert;
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            DestroyOldChunks();
            InitVariableChunkStructures();
            PrecalculateChunkBounds();
        }
    }

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

    void Update()
    {
        // Playing update
        if (Application.isPlaying)
            RuntimeUpdatePerFrame();

        // Editor update
        else if (settingsUpdated)
        {
            RequestMeshUpdate();
            settingsUpdated = false;
        }
    }

    private void RuntimeUpdatePerFrame()
    {
        CreateBuffers();
        modelGrass.InitIfNeeded(boundSize, lodSetup.numPointsPerAxis);

        if (fixedMapSize && !boundedMapGenerated)
        {
            LoadBoundedChunks();
            return;
        }

        if (!fixedMapSize)
        {
            UpdateSurroundingChunks();
            if (drawGrass)
                modelGrass.DrawAllGrass(activeChunks);
            return;
        }
    }

    public void RequestMeshUpdate()
    {
        ReleaseBuffers();
        CreateBuffers();

        modelGrass.InitIfNeeded(boundSize, lodSetup.numPointsPerAxis);
        InitChunks();
        UpdateAllChunks();
        // modelGrass.DrawAllGrass(chunks);

        ReleaseBuffers();
    }

    void InitVariableChunkStructures()
    {
        recycleableChunks = new Queue<Chunk>();
        chunks = new List<Chunk>();
        activeChunks = new List<Chunk>();
        chunkCoordsNeededToBeRendered = new List<Vector3Int>();
        existingChunks = new Dictionary<Vector3Int, Chunk>();
        existingChunkVolumeData = new Dictionary<Vector3Int, float[]>();
    }

    /// <returns>The chunk the viewer is inside</returns>
    private Vector3Int GetViewerCoord()
    {
        Vector3 p = viewer.position;
        Vector3 ps = p / boundSize;

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
        maxChunksInViewHori = Mathf.CeilToInt(lodSetup.viewDistanceHori / boundSize);
        maxChunksInViewVert = Mathf.CeilToInt(lodSetup.viewDistanceVert / boundSize);

        // maxChunksInViewHoriMustLoad = Mathf.CeilToInt(lodSetup.viewDistanceHori * 0.5f / boundSize);
        // maxChunksInViewVertMustLoad = Mathf.CeilToInt(lodSetup.viewDistanceVert * 0.5f / boundSize);

        maxChunksInViewHoriDisappear = Mathf.CeilToInt(
            lodSetup.viewDistanceHori * 1.5f / boundSize
        );
        maxChunksInViewVertDisappear = Mathf.CeilToInt(
            lodSetup.viewDistanceVert * 1.5f / boundSize
        );
    }

    /// <summary>
    /// Load one unloaded chunk, searching from centre
    /// </summary>
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

    /// <param name="coord"></param>
    /// <returns>The chunk is worth render (1) or not (0)</returns>
    private int RenderChunk(Vector3Int coord)
    {
        if (existingChunks.ContainsKey(coord))
        {
            print("Try to render " + coord + " but it already exists! ");
            return 0;
        }
        int renderedChunks = 0;

        int numPoints =
            lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis;
        float[] chunkVolumeData = new float[numPoints];

        Chunk chunk = CreateChunk(coord);

        chunk.coord = coord;
        chunk.SetUp(mat, generateColliders);

        additionalPointsBuffer = new ComputeBuffer(numPoints, sizeof(float));

        if (!existingChunkVolumeData.ContainsKey(coord))
        {
            additionalPointsBuffer.SetData(chunkVolumeData);
            renderedChunks += UpdateChunkMesh(chunk, additionalPointsBuffer);
        }
        else
        {
            additionalPointsBuffer.SetData(existingChunkVolumeData[coord]);
            renderedChunks += UpdateChunkMesh(chunk, additionalPointsBuffer);
        }
        additionalPointsBuffer.Release();

        existingChunks.Add(coord, chunk);
        chunks.Add(chunk);
        if (renderedChunks == 1)
        {
            activeChunks.Add(chunk);
        }
        return renderedChunks;
    }

    private void LoadBoundedChunks()
    {
        if (chunks == null)
            return;

        CreateChunkHolderIfNeeded();

        // All chunks, no matter lod
        int fixedChunksHori = Mathf.CeilToInt(lodSetup.fixedDistanceHori / boundSize);
        int fixedChunksVert = Mathf.CeilToInt(lodSetup.fixedDistanceVert / boundSize);

        int updatedChunks = 0;
        for (int x = -fixedChunksHori; x <= fixedChunksHori; x++)
        {
            for (int y = -fixedChunksVert; y <= fixedChunksVert; y++)
            {
                for (int z = -fixedChunksHori; z <= fixedChunksHori; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);

                    // Keep the chunk unchanged
                    if (existingChunks.ContainsKey(coord))
                        continue;
                    updatedChunks++;
                    int numPoints =
                        lodSetup.numPointsPerAxis
                        * lodSetup.numPointsPerAxis
                        * lodSetup.numPointsPerAxis;
                    Chunk chunk = CreateChunk(coord);
                    float[] chunkVolumeData = new float[numPoints];

                    chunk.coord = coord;
                    chunk.SetUp(mat, generateColliders);
                    existingChunks.Add(coord, chunk);
                    chunks.Add(chunk);

                    additionalPointsBuffer = new ComputeBuffer(numPoints, sizeof(float));

                    if (!existingChunkVolumeData.ContainsKey(coord))
                    {
                        additionalPointsBuffer.SetData(chunkVolumeData);
                        UpdateChunkMesh(chunk, additionalPointsBuffer);
                    }
                    else
                    {
                        additionalPointsBuffer.SetData(existingChunkVolumeData[coord]);
                        UpdateChunkMesh(chunk, additionalPointsBuffer);
                    }
                    additionalPointsBuffer.Release();

                    break;
                }
            }
        }
        if (updatedChunks == 0)
        {
            boundedMapGenerated = true;
        }
    }

    void ChangeVolumeData(Vector3Int chunkCoord, int id, float rangeFactor)
    {
        if (!existingChunkVolumeData.ContainsKey(chunkCoord))
        {
            int numPoints =
                lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis;
            float[] chunkVolumeData = new float[numPoints];
            existingChunkVolumeData.Add(chunkCoord, chunkVolumeData);
        }
        existingChunkVolumeData[chunkCoord][id] += changeFactor * rangeFactor;
    }

    public void DrawOnChunk(
        Vector3 hitPoint, // The directional hit piont
        int range, // Affact range
        int drawType
    ) // 0: dig, 1: add
    {
        if (drawType == 0)
        {
            changeFactor = -Mathf.Abs(changeFactor);
        }
        else if (drawType == 1)
        {
            changeFactor = Mathf.Abs(changeFactor);
        }

        int numPointsPerAxis = lodSetup.numPointsPerAxis;
        List<Vector3Int> chunksNeedToBeUpdated = new List<Vector3Int>();
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        Vector3 ps = hitPoint / boundSize;
        Vector3Int originalHittingCoord = new Vector3Int(
            Mathf.RoundToInt(ps.x),
            Mathf.RoundToInt(ps.y),
            Mathf.RoundToInt(ps.z)
        );
        Vector3 centre = CentreFromCoord(originalHittingCoord);
        float pointSpacing = boundSize / (numPointsPerAxis - 1);
        // The exact chunk the player is drawing at
        //print(hitCoord);

        // Get idVector from hitpoint
        Vector3 IdVector = new Vector3(
            ((hitPoint - centre).x + boundSize / 2) / pointSpacing,
            ((hitPoint - centre).y + boundSize / 2) / pointSpacing,
            ((hitPoint - centre).z + boundSize / 2) / pointSpacing
        );
        //print("originalHittingCoord: " + originalHittingCoord);
        // print("IdVector: " + IdVector);
        // Create a cube region of vectors
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                for (int z = -range; z <= range; z++)
                {
                    Vector3 currentVector = new Vector3(
                        ((IdVector.x + x) + (numPointsPerAxis - 1)) % (numPointsPerAxis - 1),
                        ((IdVector.y + y) + (numPointsPerAxis - 1)) % (numPointsPerAxis - 1),
                        ((IdVector.z + z) + (numPointsPerAxis - 1)) % (numPointsPerAxis - 1)
                    );

                    Vector3Int chunkOffsetVector = new Vector3Int(
                        (int)
                            Mathf.Floor(
                                ((IdVector.x + x) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)
                            ) - 1,
                        (int)
                            Mathf.Floor(
                                ((IdVector.y + y) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)
                            ) - 1,
                        (int)
                            Mathf.Floor(
                                ((IdVector.z + z) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)
                            ) - 1
                    );

                    float rangeFactor =
                        range
                        - (
                            (currentVector + chunkOffsetVector * (numPointsPerAxis - 1)) - IdVector
                        ).magnitude;
                    if (rangeFactor >= 0)
                    {
                        Vector3Int currentProcessingCoord =
                            originalHittingCoord + chunkOffsetVector;

                        // Add chunk in update list
                        if (!chunksNeedToBeUpdated.Contains(currentProcessingCoord))
                        {
                            chunksNeedToBeUpdated.Add(currentProcessingCoord);
                        }

                        Vector3Int currentVectorRoundToInt = new Vector3Int(
                            Mathf.RoundToInt(currentVector.x),
                            Mathf.RoundToInt(currentVector.y),
                            Mathf.RoundToInt(currentVector.z)
                        );

                        int currentId = PosToIndex(currentVectorRoundToInt);

                        // On 8 vertexs of a cube
                        if (
                            currentVectorRoundToInt.x == 0
                            && currentVectorRoundToInt.y == 0
                            && currentVectorRoundToInt.z == 0
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
                            currentVectorRoundToInt.x == numPointsPerAxis - 1
                            && currentVectorRoundToInt.y == 0
                            && currentVectorRoundToInt.z == 0
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
                            currentVectorRoundToInt.x == 0
                            && currentVectorRoundToInt.y == numPointsPerAxis - 1
                            && currentVectorRoundToInt.z == 0
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
                            currentVectorRoundToInt.x == 0
                            && currentVectorRoundToInt.y == 0
                            && currentVectorRoundToInt.z == numPointsPerAxis - 1
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
                            currentVectorRoundToInt.x == numPointsPerAxis - 1
                            && currentVectorRoundToInt.y == numPointsPerAxis - 1
                            && currentVectorRoundToInt.z == 0
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
                            currentVectorRoundToInt.x == numPointsPerAxis - 1
                            && currentVectorRoundToInt.y == 0
                            && currentVectorRoundToInt.z == numPointsPerAxis - 1
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
                            currentVectorRoundToInt.x == 0
                            && currentVectorRoundToInt.y == numPointsPerAxis - 1
                            && currentVectorRoundToInt.z == numPointsPerAxis - 1
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
                            currentVectorRoundToInt.x == numPointsPerAxis - 1
                            && currentVectorRoundToInt.y == numPointsPerAxis - 1
                            && currentVectorRoundToInt.z == numPointsPerAxis - 1
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
                        if (currentVectorRoundToInt.y == 0 && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    currentVectorRoundToInt.x,
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
                            currentVectorRoundToInt.y == 0
                            && currentVectorRoundToInt.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(currentVectorRoundToInt.x, numPointsPerAxis - 1, 0)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, -1, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            currentVectorRoundToInt.y == numPointsPerAxis - 1
                            && currentVectorRoundToInt.z == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(currentVectorRoundToInt.x, 0, numPointsPerAxis - 1)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 1, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            currentVectorRoundToInt.y == numPointsPerAxis - 1
                            && currentVectorRoundToInt.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, 0, 0));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 1, 1),
                                id,
                                rangeFactor
                            );
                        }

                        if (currentVectorRoundToInt.x == 0 && currentVectorRoundToInt.y == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    numPointsPerAxis - 1,
                                    numPointsPerAxis - 1,
                                    currentVectorRoundToInt.z
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, -1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            currentVectorRoundToInt.x == 0
                            && currentVectorRoundToInt.y == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(numPointsPerAxis - 1, 0, currentVectorRoundToInt.z)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            currentVectorRoundToInt.x == numPointsPerAxis - 1
                            && currentVectorRoundToInt.y == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(0, numPointsPerAxis - 1, currentVectorRoundToInt.z)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, -1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            currentVectorRoundToInt.x == numPointsPerAxis - 1
                            && currentVectorRoundToInt.y == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(0, 0, currentVectorRoundToInt.z));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 1, 0),
                                id,
                                rangeFactor
                            );
                        }

                        if (currentVectorRoundToInt.x == 0 && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    numPointsPerAxis - 1,
                                    currentVectorRoundToInt.y,
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
                            currentVectorRoundToInt.x == 0
                            && currentVectorRoundToInt.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(numPointsPerAxis - 1, currentVectorRoundToInt.y, 0)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 0, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            currentVectorRoundToInt.x == numPointsPerAxis - 1
                            && currentVectorRoundToInt.z == 0
                        )
                        {
                            int id = PosToIndex(
                                new Vector3(0, currentVectorRoundToInt.y, numPointsPerAxis - 1)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 0, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (
                            currentVectorRoundToInt.x == numPointsPerAxis - 1
                            && currentVectorRoundToInt.z == numPointsPerAxis - 1
                        )
                        {
                            int id = PosToIndex(new Vector3(0, currentVectorRoundToInt.y, 0));
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 0, 1),
                                id,
                                rangeFactor
                            );
                        }

                        // On 6 faces of a cube
                        if (currentVectorRoundToInt.x == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    numPointsPerAxis - 1,
                                    currentVectorRoundToInt.y,
                                    currentVectorRoundToInt.z
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(-1, 0, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(
                                new Vector3(0, currentVectorRoundToInt.y, currentVectorRoundToInt.z)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(1, 0, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    currentVectorRoundToInt.x,
                                    currentVectorRoundToInt.y,
                                    numPointsPerAxis - 1
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 0, -1),
                                id,
                                rangeFactor
                            );
                        }
                        if (currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(
                                new Vector3(currentVectorRoundToInt.x, currentVectorRoundToInt.y, 0)
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, 0, 1),
                                id,
                                rangeFactor
                            );
                        }
                        if (currentVectorRoundToInt.y == 0)
                        {
                            int id = PosToIndex(
                                new Vector3(
                                    currentVectorRoundToInt.x,
                                    numPointsPerAxis - 1,
                                    currentVectorRoundToInt.z
                                )
                            );
                            ChangeVolumeData(
                                currentProcessingCoord + new Vector3Int(0, -1, 0),
                                id,
                                rangeFactor
                            );
                        }
                        if (currentVectorRoundToInt.y == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(
                                new Vector3(currentVectorRoundToInt.x, 0, currentVectorRoundToInt.z)
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
            UpdateChunkMesh(existingChunks[chunksNeedToBeUpdated[i]], additionalPointsBuffer);
        }
        additionalPointsBuffer.Release();
    }

    /// <summary>
    /// Update chunk mesh by the chunk's position
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="additionalPointsBuffer"></param>
    /// <returns>0: Inactive chunk, 1: Active chunk</returns>
    public int UpdateChunkMesh(Chunk chunk, ComputeBuffer additionalPointsBuffer)
    {
        int numPointsPerAxis = lodSetup.numPointsPerAxis;
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        float isoLevel = 0;
        // A thread contains several mini threads
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        float pointSpacing = boundSize / (numPointsPerAxis - 1);

        Vector3Int coord = chunk.coord;
        Vector3 centre = CentreFromCoord(coord);

        Vector3 worldBounds = new Vector3(numChunks.x, numChunks.y, numChunks.z) * boundSize;

        // Indecator of the points are full or empty
        int[] pointStatusData = new int[2];
        pointStatusData[0] = 0;
        pointStatusData[1] = 0;
        ComputeBuffer pointsStatus = new ComputeBuffer(2, sizeof(int));
        pointsStatus.SetData(pointStatusData);

        // Chunk grass is not initialized yet (not in registery)
        modelGrass.InitializeGrassChunkIfNeeded(chunk, centre, numPointsPerAxis);

        // Gerenate individual noise value using compute shader， modifies pointsBuffer
        noiseDensity.Generate(
            chunk,
            pointsBuffer,
            additionalPointsBuffer,
            pointsStatus,
            numPointsPerAxis,
            boundSize,
            worldBounds,
            centre,
            offset,
            pointSpacing,
            isoLevel
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
        shader.SetInt("numPointsPerAxis", numPointsPerAxis);
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

        mesh.RecalculateNormals();
        chunk.UpdateColliders();

        // Dispatch grass chunk point shader
        modelGrass.CalculateGrassPos(chunk);
        return 1;
    }

    public void UpdateAllChunks()
    {
        additionalPointsBuffer = new ComputeBuffer(
            lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis,
            sizeof(float)
        );
        additionalPointsBuffer.SetData(
            new float[
                lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis
            ]
        );

        // Create mesh for each chunk
        foreach (Chunk chunk in chunks)
        {
            UpdateChunkMesh(chunk, additionalPointsBuffer);
        }
        additionalPointsBuffer.Release();
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ReleaseBuffers();
            modelGrass.ClearGrassBufferIfNeeded();
        }
    }

    void CreateBuffers()
    {
        int numPoints;
        int numVoxelsPerAxis;
        int numVoxels;
        int maxTriangleCount;

        if (!Application.isPlaying || triangleBuffer == null)
        {
            // print("Creating buffers");
            if (Application.isPlaying)
                ReleaseBuffers();

            numVoxelsPerAxis = Application.isPlaying
                ? lodSetup.numPointsPerAxis - 1
                : lodSetup.numPointsPerAxis - 1;

            numPoints = Application.isPlaying
                ? (int)Mathf.Pow(lodSetup.numPointsPerAxis, 3)
                : (int)Mathf.Pow(lodSetup.numPointsPerAxis, 3);
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
        if (fixedMapSize)
        {
            Vector3 totalBounds = (Vector3)numChunks * boundSize;
            return -totalBounds / 2 + (Vector3)coord * boundSize + Vector3.one * boundSize / 2;
        }

        return new Vector3(coord.x, coord.y, coord.z) * boundSize;
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

        List<Chunk> oldChunks = FindChunkInChildWithTag(chunkHolder);

        // Go through all coords and create a chunk there if one doesn't already exist
        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    bool chunkAlreadyExists = false;

                    // If chunk already exists, add it to the chunks list, and remove from the old list.
                    for (int i = 0; i < oldChunks.Count; i++)
                    {
                        if (oldChunks[i].coord == coord)
                        {
                            chunks.Add(oldChunks[i]);
                            oldChunks.RemoveAt(i);
                            chunkAlreadyExists = true;
                            break;
                        }
                    }

                    // Otherwise, create a new chunk
                    if (!chunkAlreadyExists)
                    {
                        var newChunk = CreateChunk(coord);
                        chunks.Add(newChunk);
                    }

                    // Setup this chunk's material and maybe setup a collider
                    chunks[chunks.Count - 1].SetUp(mat, generateColliders);
                }
            }
        }

        // Delete all unused old chunks
        for (int i = 0; i < oldChunks.Count; i++)
        {
            oldChunks[i].DestroyAndClearBuffer();
        }
    }

    Chunk CreateChunk(Vector3Int coord)
    {
        GameObject chunk = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk>();
        newChunk.coord = coord;
        return newChunk;
    }

    int PosToIndex(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        int z = Mathf.RoundToInt(pos.z);

        return z * lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis
            + y * lodSetup.numPointsPerAxis
            + x;
    }

    // Run every time settings in MeshGenerator changed
    void OnValidate()
    {
        if (!Application.isPlaying)
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
                Bounds bounds = new Bounds(CentreFromCoord(chunk.coord), Vector3.one * boundSize);
                Gizmos.color = boundsGizmoCol;
                Gizmos.DrawWireCube(CentreFromCoord(chunk.coord), Vector3.one * boundSize);
            }
        }
    }
}
