using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Profiling;

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

    private ColourGenerator2D colourGenerator2D
    {
        get
        {
            return gameObject.GetComponent<ColourGenerator2D>();
        }
    }

    private ModelGrass modelGrass
    {
        get
        {
            return gameObject.GetComponent<ModelGrass>();
        }
    }

    public AudioProcessor audioProcessor;
    public float windWeight;


    [Header("General Settings")]
    public bool fixedMapSize;

    public int fixedHalfChunkNumHori;
    public int fixedHalfChunkNumVert;

    public Transform viewer;

    public ChunkMeshProperty chunkMeshProperty;
    public ComputeShader shader;

    // [Header("Gizmos")]
    private Color boundsGizmoCol = Color.white;

    private bool showBoundsGizmo = true;
    public bool generateColliders = true;
    private string chunkHolderName = "ChunkHolder";

    GameObject chunkHolder;
    List<Chunk> chunks;
    List<Chunk> activeChunks;
    List<Vector3Int> chunksBeingEditedThisFrame;
    List<Chunk> chunksNeedsToUpdateCollider;
    Dictionary<Vector3Int, Chunk> existingChunks;
    List<Vector3Int> chunkCoordsNeededToBeRendered;
    Queue<Chunk> recycleableChunks;

    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;

    [Range(0, 1.0f)] public float meshPaintingFac = 0.1f;
    private float meshPaintingWeight = 0.1f;

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
        chunksBeingEditedThisFrame = new List<Vector3Int>();
        chunksNeedsToUpdateCollider = new List<Chunk>();
        chunkCoordsNeededToBeRendered = new List<Vector3Int>();
        existingChunks = new Dictionary<Vector3Int, Chunk>();
    }



    private void PrecalculateChunkBounds()
    {
        maxChunksInViewHori = Mathf.CeilToInt(viewDistanceHori / chunkMeshProperty.boundSize);
        maxChunksInViewVert = Mathf.CeilToInt(viewDistanceVert / chunkMeshProperty.boundSize);

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
        chunk.BindMaterialAndCollider(colourGenerator2D.GetTerrainColourMaterial(), generateColliders);

        int chunkUseFlag = UpdateChunkMesh(chunk, true);

        existingChunks.Add(coord, chunk);
        chunks.Add(chunk);

        return chunkUseFlag;
    }

    private void LoadBoundedChunks()
    {
        CreateChunkHolderIfNeeded();

        for (int x = -fixedHalfChunkNumHori; x < fixedHalfChunkNumHori; x++)
        {
            for (int y = -fixedHalfChunkNumVert; y < fixedHalfChunkNumVert; y++)
            {
                for (int z = -fixedHalfChunkNumHori; z < fixedHalfChunkNumHori; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);

                    int numPoints =
                        chunkMeshProperty.numPointsPerAxis
                        * chunkMeshProperty.numPointsPerAxis
                        * chunkMeshProperty.numPointsPerAxis;

                    Chunk chunk = CreateChunk(coord);
                    chunk.BindMaterialAndCollider(colourGenerator2D.GetTerrainColourMaterial(), generateColliders);

                    existingChunks.Add(coord, chunk);
                    chunks.Add(chunk);

                    UpdateChunkMesh(chunk, true);
                }
            }
        }
    }

    private Vector3Int WorldPosToChunkId(Vector3 worldPos)
    {
        Vector3Int returnVal = new Vector3Int();
        returnVal.x = Mathf.FloorToInt(worldPos.x / chunkMeshProperty.boundSize);
        returnVal.y = Mathf.FloorToInt(worldPos.y / chunkMeshProperty.boundSize);
        returnVal.z = Mathf.FloorToInt(worldPos.z / chunkMeshProperty.boundSize);

        return returnVal;
    }

    private Vector3 CentreFromCoord(Vector3Int coord)
    {
        return (new Vector3(coord.x, coord.y, coord.z) * chunkMeshProperty.boundSize) +
                new Vector3(chunkMeshProperty.boundSize / 2, chunkMeshProperty.boundSize / 2, chunkMeshProperty.boundSize / 2);
    }

    private Vector3Int GetViewerCoord()
    {
        return WorldPosToChunkId(viewer.position);
    }

    public void DrawOnChunk(
        Vector3 hitPoint,   // The directional hit piont
        float radius,       // Affact range
        float strength,
        int drawType
    ) // 0: dig, 1: add
    {
        if (drawType == 0)
            meshPaintingWeight = -Mathf.Abs(meshPaintingFac * strength);
        else
            meshPaintingWeight = Mathf.Abs(meshPaintingFac * strength);

        Vector3Int hittingCoordMin = WorldPosToChunkId(hitPoint - radius * Vector3.one);
        Vector3Int hittingCoordMax = WorldPosToChunkId(hitPoint + radius * Vector3.one);

        for (int x = hittingCoordMin.x; x <= hittingCoordMax.x; x++)
        {
            for (int y = hittingCoordMin.y; y <= hittingCoordMax.y; y++)
            {
                for (int z = hittingCoordMin.z; z <= hittingCoordMax.z; z++)
                {
                    Vector3Int coordNeedToUpdate = new Vector3Int(x, y, z);

                    if (!existingChunks.ContainsKey(coordNeedToUpdate)) continue;
                    chunksBeingEditedThisFrame.Add(coordNeedToUpdate);
                }
            }
        }

        // Update these chunks
        if (chunksBeingEditedThisFrame.Count > 0)
        {
            noiseDensity.EnableChunkDrawing();
            for (int i = 0; i < chunksBeingEditedThisFrame.Count; i++)
            {
                noiseDensity.RegisterChunkDrawingDataToComputeShader(
                    hitWorldPos: hitPoint,
                    radius,
                    meshPaintingWeight
                );
                UpdateChunkMesh(
                    existingChunks[chunksBeingEditedThisFrame[i]],
                    generateColliderNow: false);
            }
            noiseDensity.DisableChunkDrawing();
            chunksBeingEditedThisFrame.Clear();
        }
    }

    public int UpdateChunkMesh(Chunk chunk, bool generateColliderNow)
    {
        int numVoxelsPerAxis = chunkMeshProperty.numPointsPerAxis - 1;
        // A thread contains several mini threads
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        float meshPointSpacing = chunkMeshProperty.boundSize / (chunkMeshProperty.numPointsPerAxis - 1);

        // Indecator of the points are full or empty
        int[] pointStatusData = new int[2];
        pointStatusData[0] = 0;
        pointStatusData[1] = 0;
        ComputeBuffer pointsStatus = new ComputeBuffer(2, sizeof(int));
        pointsStatus.SetData(pointStatusData);

        Vector3 worldSize =
                new Vector3(fixedHalfChunkNumHori * 2, fixedHalfChunkNumVert * 2, fixedHalfChunkNumHori * 2) *
                chunkMeshProperty.boundSize;

        // Gerenate individual noise value using compute shader, modifies pointsBuffer
        noiseDensity.CalculateChunkVolumeData(
            chunk,
            pointsBuffer,
            pointsStatus,
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
        shader.SetFloat("isoLevel", 0);

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

        if (generateColliderNow)
            chunk.UpdateColliderSharedMesh();
        else
            chunksNeedsToUpdateCollider.Add(chunk);

        if (Application.isPlaying)
        {
            Vector3Int chunkBelowCoord = chunk.coord + new Vector3Int(0, -1, 0);
            if (existingChunks.ContainsKey(chunkBelowCoord))
            {
                modelGrass.CalculateChunkGrassPosition(chunk, existingChunks[chunkBelowCoord].groundLevelDataBuffer);
            }else{
                modelGrass.CalculateChunkGrassPosition(chunk);
            }
        }

        if (!activeChunks.Contains(chunk)) activeChunks.Add(chunk);

        return 1;
    }

    // Editor Preview
    public void UpdateAllChunks()
    {
        foreach (Chunk chunk in chunks)
        {
            UpdateChunkMesh(chunk, true);
        }
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

        for (int x = -fixedHalfChunkNumHori; x < fixedHalfChunkNumHori; x++)
        {
            for (int y = -fixedHalfChunkNumVert; y < fixedHalfChunkNumVert; y++)
            {
                for (int z = -fixedHalfChunkNumHori; z < fixedHalfChunkNumHori; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    var newChunk = CreateChunk(coord);
                    chunks.Add(newChunk);
                    chunks[chunks.Count - 1].BindMaterialAndCollider(
                        colourGenerator2D.GetTerrainColourMaterial(), generateColliders);
                }
            }
        }
    }

    Chunk CreateChunk(Vector3Int coord)
    {
        GameObject chunk = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk>();

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
