using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

[ExecuteInEditMode]
public class TerrainMesh : MonoBehaviour
{

    const int threadGroupSize = 8;

    [Header("General Settings")]
    public NoiseDensity noiseDensity;

    public bool fixedMapSize;

    // Nums of chunks to generate
    // Show this variable when fixedMapSize is true
    public Vector3Int numChunks = Vector3Int.one;
    public Transform viewer;

    public LodSetup lodSetup;
    public ComputeShader shader;
    public Material mat;


    [Header("Voxel Settings")]
    public float isoLevel;
    public float boundsSize = 20;
    Vector3 offset = Vector3.zero;

    // [Header("Gizmos")]
    Color boundsGizmoCol = Color.white;

    bool showBoundsGizmo = true;
    bool generateColliders = true;

    GameObject chunkHolder;
    public string chunkHolderName = "ChunkHolder";
    List<Chunk> chunks;
    Dictionary<Vector3Int, Chunk> existingChunks;
    Dictionary<Vector3Int, float[]> existingChunkVolumeData;
    Queue<Chunk> recycleableChunks;

    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer additionalPointsBuffer;
    ComputeBuffer triCountBuffer;

    float changeFactor = -0.6f;
    bool settingsUpdated;
    bool boundedMapGenerated;
    float loadTime;
    [System.Serializable]
    public struct LodSetup
    {
        public int numPointsPerAxisPreview;
        public int numPointsPerAxis;
        public int viewDistanceHori;
        public int viewDistanceVert;
        public int fixedDistanceHori;
        public int fixedDistanceVert;

    }

    void Awake()
    {
        if (Application.isPlaying)
        {
            boundedMapGenerated = false;
            loadTime = 0;
            ReleaseBuffers();
            // if (fixedMapSize)
            //     fixedMapSize = false;
            InitVariableChunkStructures();
            // Destroy all chunks by searching all objects contains Chunk script
            var oldChunks = FindObjectsOfType<Chunk>();
            for (int i = 0; i < oldChunks.Length; i++)
            {
                Destroy(oldChunks[i].gameObject);
            }
        }
    }

    void Update()
    {
        // Playing update
        if (Application.isPlaying)
            Run();

        // Editor update
        else if (settingsUpdated)
        {
            RequestMeshUpdate();
            settingsUpdated = false;
        }
        loadTime += Time.deltaTime;

    }

    void Run()
    {
        CreateBuffers();

        if (!Application.isPlaying)
        {
            InitChunks();
            UpdateAllChunks();
        }
        else
        {
            if (fixedMapSize)
            {
                if (!boundedMapGenerated)
                {
                    LoadBoundedChunks();

                }
            }
            else
            {
                LoadVisibleChunks();
            }
        }

        // Release buffers immediately in editor
        if (!Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    public void RequestMeshUpdate()
    {
        Run();
    }

    void InitVariableChunkStructures()
    {
        recycleableChunks = new Queue<Chunk>();
        chunks = new List<Chunk>();
        existingChunks = new Dictionary<Vector3Int, Chunk>();
        existingChunkVolumeData = new Dictionary<Vector3Int, float[]>();
    }

    void LoadVisibleChunks()
    {
        if (chunks == null)
            return;

        CreateChunkHolder();

        Vector3 p = viewer.position;
        Vector3 ps = p / boundsSize;

        // Indicates which chunk the viewer in in
        Vector3Int viewerCoord = new Vector3Int(Mathf.RoundToInt(ps.x), Mathf.RoundToInt(ps.y), Mathf.RoundToInt(ps.z));
        // All chunks, no matter lod
        int maxChunksInViewHori = Mathf.CeilToInt(lodSetup.viewDistanceHori / boundsSize);
        int maxChunksInViewVert = Mathf.CeilToInt(lodSetup.viewDistanceVert / boundsSize);

        // Kick chunks outside the range
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            Chunk chunk = chunks[i];
            Vector3Int chunkCoord = chunk.coord;

            if (((Mathf.Pow(chunkCoord.x - viewerCoord.x, 2) +
                Mathf.Pow(chunkCoord.z - viewerCoord.z, 2)) >
                Mathf.Pow(maxChunksInViewHori, 2)))
            {
                existingChunks.Remove(chunk.coord);
                chunk.DestroyOrDisable();
                chunks.RemoveAt(i);
            }
        }
        for (int x = -maxChunksInViewHori; x <= maxChunksInViewHori; x++)
        {
            for (int y = -maxChunksInViewVert; y <= maxChunksInViewVert; y++)
            {
                for (int z = -maxChunksInViewHori; z <= maxChunksInViewHori; z++)
                {
                    if (((Mathf.Pow(x, 2) + Mathf.Pow(z, 2)) > Mathf.Pow(maxChunksInViewHori, 2)))
                        continue;

                    Vector3Int coord = new Vector3Int(x + viewerCoord.x, y, z + viewerCoord.z);

                    // Keep the chunk unchanged
                    if (existingChunks.ContainsKey(coord))
                        continue;

                    int numPoints = lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis;
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
                }
            }
        }
    }

    void LoadBoundedChunks()
    {
        if (chunks == null)
            return;

        CreateChunkHolder();

        // All chunks, no matter lod
        int fixedChunksHori = Mathf.CeilToInt(lodSetup.fixedDistanceHori / boundsSize);
        int fixedChunksVert = Mathf.CeilToInt(lodSetup.fixedDistanceVert / boundsSize);

        int updatedChunks = 0;
        for (int x = -fixedChunksHori; x <= fixedChunksHori; x++)
        {
            for (int y = -fixedChunksVert; y <= fixedChunksVert; y++)
            {
                for (int z = -fixedChunksHori; z <= fixedChunksHori; z++)
                {
                    // TODO:
                    Vector3Int coord = new Vector3Int(x, y, z);

                    // Keep the chunk unchanged
                    if (existingChunks.ContainsKey(coord))
                        continue;
                    updatedChunks++;
                    int numPoints = lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis;
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
            print(loadTime);
        }


    }

    void ChangeVolumeData(Vector3Int chunkCoord, int id, float rangeFactor)
    {
        if (!existingChunkVolumeData.ContainsKey(chunkCoord))
        {
            int numPointsPerAxis = lodSetup.numPointsPerAxis;
            int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            float[] chunkVolumeData = new float[numPoints];
            existingChunkVolumeData.Add(chunkCoord, chunkVolumeData);
        }
        existingChunkVolumeData[chunkCoord][id] += changeFactor * rangeFactor;
    }

    public void DrawOnChunk(
        Vector3 hitPoint,   // The directional hit piont
        int range,          // Affact range
        int drawType)       // 0: dig, 1: add
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
        Vector3 ps = hitPoint / boundsSize;
        Vector3Int originalHittingCoord = new Vector3Int(Mathf.RoundToInt(ps.x), Mathf.RoundToInt(ps.y), Mathf.RoundToInt(ps.z));
        Vector3 centre = CentreFromCoord(originalHittingCoord);
        float pointSpacing = boundsSize / (numPointsPerAxis - 1);
        // The exact chunk the player is drawing at
        //print(hitCoord);

        // Get idVector from hitpoint
        Vector3 IdVector = new Vector3(
            ((hitPoint - centre).x + boundsSize / 2) / pointSpacing,
            ((hitPoint - centre).y + boundsSize / 2) / pointSpacing,
            ((hitPoint - centre).z + boundsSize / 2) / pointSpacing);
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
                        ((IdVector.z + z) + (numPointsPerAxis - 1)) % (numPointsPerAxis - 1));

                    Vector3Int chunkOffsetVector = new Vector3Int(
                        (int)Mathf.Floor(((IdVector.x + x) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)) - 1,
                        (int)Mathf.Floor(((IdVector.y + y) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)) - 1,
                        (int)Mathf.Floor(((IdVector.z + z) + (numPointsPerAxis - 1)) / (numPointsPerAxis - 1)) - 1);

                    float rangeFactor = range - ((currentVector + chunkOffsetVector * (numPointsPerAxis - 1)) - IdVector).magnitude;
                    if (rangeFactor >= 0)
                    {
                        Vector3Int currentProcessingCoord = originalHittingCoord + chunkOffsetVector;

                        // Add chunk in update list
                        if (!chunksNeedToBeUpdated.Contains(currentProcessingCoord))
                        {
                            chunksNeedToBeUpdated.Add(currentProcessingCoord);
                        }

                        Vector3Int currentVectorRoundToInt = new Vector3Int(
                            Mathf.RoundToInt(currentVector.x),
                            Mathf.RoundToInt(currentVector.y),
                            Mathf.RoundToInt(currentVector.z));

                        int currentId = PosToIndex(currentVectorRoundToInt);

                        // On 8 vertexs of a cube
                        if (currentVectorRoundToInt.x == 0
                        && currentVectorRoundToInt.y == 0
                        && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, numPointsPerAxis - 1, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, -1, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1
                        && currentVectorRoundToInt.y == 0
                        && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(0, numPointsPerAxis - 1, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, -1, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == 0
                        && currentVectorRoundToInt.y == numPointsPerAxis - 1
                        && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, 0, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, 1, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == 0
                        && currentVectorRoundToInt.y == 0
                        && currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, numPointsPerAxis - 1, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, -1, 1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1
                        && currentVectorRoundToInt.y == numPointsPerAxis - 1
                        && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(0, 0, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, 1, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1
                        && currentVectorRoundToInt.y == 0
                        && currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(0, numPointsPerAxis - 1, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, -1, 1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == 0
                        && currentVectorRoundToInt.y == numPointsPerAxis - 1
                        && currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, 0, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, 1, 1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1
                        && currentVectorRoundToInt.y == numPointsPerAxis - 1
                        && currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(0, 0, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, 1, 1), id, rangeFactor);
                        }

                        // On 12 edges of a cube
                        if (currentVectorRoundToInt.y == 0
                        && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, numPointsPerAxis - 1, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(0, -1, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.y == 0
                        && currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, numPointsPerAxis - 1, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(0, -1, 1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.y == numPointsPerAxis - 1
                        && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, 0, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(0, 1, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.y == numPointsPerAxis - 1
                        && currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, 0, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(0, 1, 1), id, rangeFactor);
                        }

                        if (currentVectorRoundToInt.x == 0
                        && currentVectorRoundToInt.y == 0)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, numPointsPerAxis - 1, currentVectorRoundToInt.z));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, -1, 0), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == 0
                        && currentVectorRoundToInt.y == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, 0, currentVectorRoundToInt.z));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, 1, 0), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1
                        && currentVectorRoundToInt.y == 0)
                        {
                            int id = PosToIndex(new Vector3(0, numPointsPerAxis - 1, currentVectorRoundToInt.z));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, -1, 0), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1
                        && currentVectorRoundToInt.y == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(0, 0, currentVectorRoundToInt.z));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, 1, 0), id, rangeFactor);
                        }

                        if (currentVectorRoundToInt.x == 0
                        && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, currentVectorRoundToInt.y, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, 0, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == 0
                        && currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, currentVectorRoundToInt.y, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, 0, 1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1
                        && currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(0, currentVectorRoundToInt.y, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, 0, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1
                        && currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(0, currentVectorRoundToInt.y, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, 0, 1), id, rangeFactor);
                        }

                        // On 6 faces of a cube
                        if (currentVectorRoundToInt.x == 0)
                        {
                            int id = PosToIndex(new Vector3(numPointsPerAxis - 1, currentVectorRoundToInt.y, currentVectorRoundToInt.z));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(-1, 0, 0), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.x == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(0, currentVectorRoundToInt.y, currentVectorRoundToInt.z));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(1, 0, 0), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.z == 0)
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, currentVectorRoundToInt.y, numPointsPerAxis - 1));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(0, 0, -1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.z == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, currentVectorRoundToInt.y, 0));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(0, 0, 1), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.y == 0)
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, numPointsPerAxis - 1, currentVectorRoundToInt.z));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(0, -1, 0), id, rangeFactor);
                        }
                        if (currentVectorRoundToInt.y == numPointsPerAxis - 1)
                        {
                            int id = PosToIndex(new Vector3(currentVectorRoundToInt.x, 0, currentVectorRoundToInt.z));
                            ChangeVolumeData(currentProcessingCoord + new Vector3Int(0, 1, 0), id, rangeFactor);
                        }
                        ChangeVolumeData(currentProcessingCoord, currentId, rangeFactor);
                    }
                }
            }
        }
        additionalPointsBuffer = new ComputeBuffer(numPointsPerAxis * numPointsPerAxis * numPointsPerAxis, sizeof(float));

        // Mesh will be updated more than once if chunk edge is met
        for (int i = 0; i < chunksNeedToBeUpdated.Count; i++)
        {
            additionalPointsBuffer.SetData(existingChunkVolumeData[chunksNeedToBeUpdated[i]]);
            UpdateChunkMesh(existingChunks[chunksNeedToBeUpdated[i]], additionalPointsBuffer);
        }
        additionalPointsBuffer.Release();


    }


    public bool IsVisibleFrom(Bounds bounds, Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        bool visiableFromCam = GeometryUtility.TestPlanesAABB(planes, bounds);
        return visiableFromCam;
    }

    public void UpdateChunkMesh(Chunk chunk, ComputeBuffer additionalPointsBuffer)
    {
        int numPointsPerAxis = 0;
        if (Application.isPlaying)
            numPointsPerAxis = lodSetup.numPointsPerAxis;
        else
            numPointsPerAxis = lodSetup.numPointsPerAxisPreview;

        int numVoxelsPerAxis = numPointsPerAxis - 1;
        // A thread contains several mini threads
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        float pointSpacing = boundsSize / (numPointsPerAxis - 1);

        Vector3Int coord = chunk.coord;
        Vector3 centre = CentreFromCoord(coord);

        Vector3 worldBounds = new Vector3(numChunks.x, numChunks.y, numChunks.z) * boundsSize;

        // Indecator of the points are full or empty
        int[] pointStatusData = new int[2];
        pointStatusData[0] = 0;
        pointStatusData[1] = 0;
        ComputeBuffer pointsStatus = new ComputeBuffer(2, sizeof(int));
        pointsStatus.SetData(pointStatusData);

        // Gerenate individual noise value using compute shader， modifies pointsBuffer
        noiseDensity.Generate(pointsBuffer, additionalPointsBuffer, pointsStatus, numPointsPerAxis, boundsSize, worldBounds, centre, offset, pointSpacing, isoLevel);
        pointsStatus.GetData(pointStatusData);
        pointsStatus.Release();

        // If all empty/ all full
        if (pointStatusData[0] == 0 || pointStatusData[1] == 0)
        {
            chunk.gameObject.SetActive(false);
            return;
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
    }

    public void UpdateAllChunks()
    {
        additionalPointsBuffer = new ComputeBuffer(
            lodSetup.numPointsPerAxisPreview *
            lodSetup.numPointsPerAxisPreview *
            lodSetup.numPointsPerAxisPreview, sizeof(float));
        additionalPointsBuffer.SetData(new float[
            lodSetup.numPointsPerAxisPreview *
            lodSetup.numPointsPerAxisPreview *
            lodSetup.numPointsPerAxisPreview]);
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

            numVoxelsPerAxis = Application.isPlaying ?
            lodSetup.numPointsPerAxis - 1 : lodSetup.numPointsPerAxisPreview - 1;

            numPoints = Application.isPlaying ?
            (int)Mathf.Pow(lodSetup.numPointsPerAxis, 3) : (int)Mathf.Pow(lodSetup.numPointsPerAxis, 3);
            // Voxels(mini cubes) in a volume
            numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            // Max triangles to be create per voxel is 5
            maxTriangleCount = numVoxels * 5;

            // ComputeBuffer(Num of elements, size per element, type of the buffer)
            // 3 points, x, y, z, stored in float
            triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
            // stores x, y, z, volumeValue
            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            // A int to store total count of triangles
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }
    }

    void ReleaseBuffers()
    {
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
            Vector3 totalBounds = (Vector3)numChunks * boundsSize;
            return -totalBounds / 2 + (Vector3)coord * boundsSize + Vector3.one * boundsSize / 2;
        }

        return new Vector3(coord.x, coord.y, coord.z) * boundsSize;
    }

    void CreateChunkHolder()
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
        // TODO:
        // Create a folder
        CreateChunkHolder();
        chunks = new List<Chunk>();
        List<Chunk> oldChunks = FindChunkInChildWithTag(chunkHolder);
        //List<Chunk> oldChunks = new List<Chunk>(FindObjectsOfType<Chunk>());
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

        // Delete all unused chunks
        for (int i = 0; i < oldChunks.Count; i++)
        {
            oldChunks[i].DestroyOrDisable();
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

        return z * lodSetup.numPointsPerAxis * lodSetup.numPointsPerAxis + y * lodSetup.numPointsPerAxis + x;

    }

    // Run every time settings in MeshGenerator changed
    void OnValidate()
    {
        settingsUpdated = true;
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

            List<Chunk> chunks = (this.chunks == null) ? new List<Chunk>(FindObjectsOfType<Chunk>()) : this.chunks;
            foreach (var chunk in chunks)
            {
                Bounds bounds = new Bounds(CentreFromCoord(chunk.coord), Vector3.one * boundsSize);
                Gizmos.color = boundsGizmoCol;
                Gizmos.DrawWireCube(CentreFromCoord(chunk.coord), Vector3.one * boundsSize);
            }
        }
    }

}