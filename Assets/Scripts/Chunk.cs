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

    public void DestroyOrDisable()
    {
        if (Application.isPlaying)
        {
            Destroy(mesh);
            Destroy(meshFilter);
            mesh.Clear();
            // gameObject.SetActive(false);
            Destroy(this.gameObject);
        }
        else
        {
            DestroyImmediate(gameObject, false);
        }
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
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
            // force update
            meshCollider.enabled = false;
            meshCollider.enabled = true;
        }
    }
}