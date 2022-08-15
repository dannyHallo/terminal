using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Runtime.InteropServices.Marshal;

public class Chunk : MonoBehaviour
{
    public Vector3Int coord;
    public Vector3 centre;
    public float boundSize;
    public float pointSpacing;

    [HideInInspector]
    public Mesh mesh;
    public int lodLevel;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    bool generateCollider;
    bool setuped = false;

    public ComputeBuffer argsBuffer;
    public ComputeBuffer argsLodBuffer;
    public ComputeBuffer grassPositionsBuffer;
    public ComputeBuffer editingBuffer;
    public ComputeBuffer groundLevelDataBuffer;
    public ComputeBuffer culledPositionsBuffer;
    public Material grassMaterial;

    public int numPointsPerAxis, numGrassesPerAxis;

    private struct GrassData
    {
        Vector4 position;
        bool enable;
    };

    public struct GroundLevelData
    {
        public float weight;
        public float twoDimentionalHeight;
        public bool hasMeshAtThisPlace;
    };

    public void SetupConfig(
        int numPointsPerAxis,
        int numGrassesPerAxis,
        Vector3Int coord,
        Vector3 centre,
        float boundSize,
        float pointSpacing)
    {
        this.numPointsPerAxis = numPointsPerAxis;
        this.numGrassesPerAxis = numGrassesPerAxis;
        this.coord = coord;
        this.centre = centre;
        this.boundSize = boundSize;
        this.pointSpacing = pointSpacing;
    }

    public void DestroyAndClearBuffer()
    {
        if (Application.isPlaying)
        {
            Destroy(mesh);
            Destroy(meshFilter);
            mesh.Clear();
            Destroy(this.gameObject);
        }
        else
        {
            DestroyImmediate(gameObject, false);
        }
        FreeBuffers();
    }

    public void CreateBuffers()
    {
        argsBuffer = new ComputeBuffer(
            1,
            5 * sizeof(uint),
            ComputeBufferType.IndirectArguments
        );
        argsLodBuffer = new ComputeBuffer(
            1,
            5 * sizeof(uint),
            ComputeBufferType.IndirectArguments
        );
        groundLevelDataBuffer = new ComputeBuffer(numPointsPerAxis * numPointsPerAxis * numPointsPerAxis,
                    SizeOf(typeof(GroundLevelData)));

        grassPositionsBuffer = new ComputeBuffer(numGrassesPerAxis * numGrassesPerAxis, SizeOf(typeof(GrassData)));
        culledPositionsBuffer = new ComputeBuffer(numGrassesPerAxis * numGrassesPerAxis, SizeOf(typeof(GrassData)));
        editingBuffer = new ComputeBuffer(numPointsPerAxis * numPointsPerAxis * numPointsPerAxis, sizeof(float));
    }

    public void FreeBuffers()
    {
        if (grassPositionsBuffer == null)
            return;

        grassPositionsBuffer.Release();
        grassPositionsBuffer = null;

        editingBuffer.Release();
        editingBuffer = null;

        culledPositionsBuffer.Release();
        culledPositionsBuffer = null;

        argsBuffer.Release();
        argsBuffer = null;

        argsLodBuffer.Release();
        argsLodBuffer = null;

        groundLevelDataBuffer.Release();
        groundLevelDataBuffer = null;
    }

    // Add components/get references in case lost (references can be lost when working in the editor)
    public void BindMaterialAndCollider(Material mat, bool generateCollider)
    {
        if (setuped)
            return;

        this.generateCollider = generateCollider;

        if (gameObject.tag != "Chunk")
            gameObject.tag = "Chunk";

        // Get / Create meshFilter
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        else
            meshFilter = GetComponent<MeshFilter>();

        // Get / Create meshRenderer
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        else
            meshRenderer = GetComponent<MeshRenderer>();

        // Get / Create mesh and bind
        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh;
        }

        // Generate and bind collider with mesh
        if (generateCollider)
        {
            if (meshCollider == null)
                meshCollider = gameObject.AddComponent<MeshCollider>();
            else
                meshCollider = GetComponent<MeshCollider>();

            meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation |
                    MeshColliderCookingOptions.EnableMeshCleaning |
                    MeshColliderCookingOptions.UseFastMidphase |
                    MeshColliderCookingOptions.WeldColocatedVertices;

            UpdateColliderSharedMesh();
        }
        // Destroy existing mesh collider if collision is disabled
        else if (meshCollider != null)
            DestroyImmediate(meshCollider); // DestroyImm.. works at editor, whereas Destroy does not

        // Set materials
        meshRenderer.material = mat;
        setuped = true;
    }

    public int GetMeshInstanceId()
    {
        return mesh.GetInstanceID();
    }

    public void UpdateColliderSharedMesh()
    {
        if (!generateCollider)
            return;

        meshCollider.sharedMesh = mesh;
    }
}
