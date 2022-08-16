using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : MonoBehaviour
{
    private TerrainMesh terrainMesh
    {
        get
        {
            return gameObject.GetComponent<TerrainMesh>();
        }
    }

    [Header("Noise")]
    public int seed;

    [Range(0, 1)]
    public float DragMeToUpdate;

    [Range(1, 20)]
    public int numOctaves = 4;

    [Range(1, 2)]
    public float lacunarity = 2;

    [Range(0, 1)]
    public float persistence = .5f;

    public float noiseScale = 0.1f;

    [Range(0, 3f)]
    public float heightGredient = 1;

    [Range(0, 3f)]
    public float multiFractalWeight = 1;
    public float planetRadius = 50;
    public float noiseWeight = 1;
    public bool closeEdges;

    public float floorOffset = 1;

    public Vector4 shaderParams;

    ComputeBuffer debugBuffer;

    public ComputeShader noiseDensityShader;

    protected List<ComputeBuffer> buffersToRelease;

    [Header("Debug")]
    public bool editorUpdate = false;

    void OnValidate()
    {
        if (editorUpdate && !Application.isPlaying && terrainMesh)
        {
            terrainMesh.RequestMeshUpdate();
        }
    }

    public void EnableChunkDrawing()
    {
        noiseDensityShader.SetBool("processDrawing", true);
    }

    public void DisableChunkDrawing()
    {
        noiseDensityShader.SetBool("processDrawing", false);
    }

    public void RegisterChunkDrawingDataToComputeShader(
        Vector3 hitWorldPos,
        float affactRadius,
        float affactWeight
    )
    {
        noiseDensityShader.SetVector("hitWorldPos", hitWorldPos);
        noiseDensityShader.SetFloat("affactRadius", affactRadius);
        noiseDensityShader.SetFloat("affactWeight", affactWeight);
    }

    public void CalculateChunkVolumeData(
        Chunk chunk,
        ComputeBuffer pointsBuffer,
        ComputeBuffer pointsStatus,
        Vector3 worldSize
    )
    {
        int numPoints = chunk.numPointsPerAxis * chunk.numPointsPerAxis * chunk.numPointsPerAxis;
        int numThreadsPerAxis = Mathf.CeilToInt(chunk.numPointsPerAxis / 8.0f);
        buffersToRelease = new List<ComputeBuffer>();

        // Points buffer is populated inside shader with pos (xyz) + density (w).

        var prng = new System.Random(seed);
        var offsets = new Vector3[numOctaves];
        float offsetRange = 1000;
        for (int i = 0; i < numOctaves; i++)
        {
            // What does it mean by ( * 2 - 1 )?
            offsets[i] =
                new Vector3(
                    (float)prng.NextDouble() * 2 - 1,
                    (float)prng.NextDouble() * 2 - 1,
                    (float)prng.NextDouble() * 2 - 1
                ) * offsetRange;
        }

        // Sets offset buffer
        ComputeBuffer offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);

        offsetsBuffer.SetData(offsets);
        buffersToRelease.Add(offsetsBuffer);

        noiseDensityShader.SetVector("centre", chunk.centre);
        noiseDensityShader.SetVector("worldSize", worldSize);
        noiseDensityShader.SetVector("params", shaderParams);

        noiseDensityShader.SetBool("closeEdges", closeEdges);

        noiseDensityShader.SetInt("octaves", Mathf.Max(1, numOctaves));
        noiseDensityShader.SetInt("numPointsPerAxis", chunk.numPointsPerAxis);

        noiseDensityShader.SetFloat("boundsSize", chunk.boundSize);
        noiseDensityShader.SetFloat("spacing", chunk.pointSpacing);
        noiseDensityShader.SetFloat("lacunarity", lacunarity);
        noiseDensityShader.SetFloat("persistence", persistence);
        noiseDensityShader.SetFloat("noiseScale", noiseScale);
        noiseDensityShader.SetFloat("noiseWeight", noiseWeight);
        noiseDensityShader.SetFloat("floorOffset", floorOffset);
        noiseDensityShader.SetFloat("radius", planetRadius);
        noiseDensityShader.SetFloat("heightGredient", heightGredient);
        noiseDensityShader.SetFloat("multiFractalWeight", multiFractalWeight);

        noiseDensityShader.SetBuffer(0, "offsets", offsetsBuffer);
        noiseDensityShader.SetBuffer(0, "points", pointsBuffer);
        noiseDensityShader.SetBuffer(0, "manualData", chunk.editingBuffer);
        noiseDensityShader.SetBuffer(0, "GroundLevelDataBuffer", chunk.groundLevelDataBuffer);
        noiseDensityShader.SetBuffer(0, "pointsStatus", pointsStatus);

        noiseDensityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        foreach (ComputeBuffer buffer in buffersToRelease) buffer.Release();
    }
}
