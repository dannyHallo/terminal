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

        print("FREEED!");

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
        setuped = true;
        this.generateCollider = generateCollider;
        if (gameObject.tag != "Chunk")
            gameObject.tag = "Chunk";
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (generateCollider)
            meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (meshCollider == null && generateCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        if (meshCollider != null && !generateCollider)
        {
            // DestroyImm.. works at editor, whereas Destroy not
            DestroyImmediate(meshCollider);
        }

        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh;
        }

        if (generateCollider)
        {
            if (meshCollider.sharedMesh == null)
            {
                meshCollider.sharedMesh = mesh;
            }
            UpdateColliders();
        }
        meshRenderer.material = mat;
    }

    public void UpdateColliders()
    {
        if (generateCollider)
        {
            // force update
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
            meshCollider.enabled = false;
            meshCollider.enabled = true;
        }
    }
}
