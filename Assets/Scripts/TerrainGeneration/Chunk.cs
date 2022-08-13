using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Vector3Int coord;

    [HideInInspector]
    public Mesh mesh;
    public int lodLevel;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    bool generateCollider;
    bool setuped = false;

    public ComputeBuffer argsBuffer;
    public ComputeBuffer argsBufferLOD;
    public ComputeBuffer positionsBuffer;
    public ComputeBuffer groundLevelDataBuffer;
    public ComputeBuffer culledPositionsBuffer;
    public Material grassMaterial;

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

    public void FreeBuffers()
    {
        if (positionsBuffer == null)
            return;

        positionsBuffer.Release();
        positionsBuffer = null;

        culledPositionsBuffer.Release();
        culledPositionsBuffer = null;

        argsBuffer.Release();
        argsBuffer = null;

        argsBufferLOD.Release();
        argsBufferLOD = null;

        groundLevelDataBuffer.Release();
        groundLevelDataBuffer = null;
    }

    // Add components/get references in case lost (references can be lost when working in the editor)
    public void SetUp(Material mat, bool generateCollider)
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
