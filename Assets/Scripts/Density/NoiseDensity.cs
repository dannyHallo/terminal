using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : MonoBehaviour
{
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

    [Range(0, 0.04f)]
    public float noiseScale = 0.1f;

    [Range(0, 3f)]
    public float heightGredient = 1;

    [Range(0, 3f)]
    public float multiFractalWeight = 1;

    public float noiseWeight = 1;
    public bool closeEdges;
    public bool b1;
    public bool b2;
    public float f1;
    public float f2;
    public float f3;

    public float floorOffset = 1;

    public Vector4 shaderParams;

    ComputeBuffer debugBuffer;

    const int threadGroupSize = 8;
    public ComputeShader noiseDensityShader;

    protected List<ComputeBuffer> buffersToRelease;

    void OnValidate()
    {
        if (!Application.isPlaying && FindObjectOfType<TerrainMesh>())
        {
            FindObjectOfType<TerrainMesh>().RequestMeshUpdate();
        }
    }

    public void Generate(
        Chunk chunk,
        ComputeBuffer pointsBuffer,
        ComputeBuffer additionalPointsBuffer,
        ComputeBuffer pointsStatus,
        int numPointsPerAxis,
        float boundsSize,
        Vector3 worldBounds,
        Vector3 centre,
        Vector3 offset,
        float spacing,
        float isoLevel
    )
    {
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);
        buffersToRelease = new List<ComputeBuffer>();

        // Points buffer is populated inside shader with pos (xyz) + density (w).

        /// Noise parameters
        // Scale: (SCALE)
        // Octaves: level of details (LAYER NUMS)
        // Lacunarity: adjusts fraquency at each octave (FRAQUENCY multiplied per layer)
        // Persistance: adjust amplitude of each octave (AMPLITUDE multiplied per layer)

        var prng = new System.Random(seed);
        var offsets = new Vector3[numOctaves];
        float offsetRange = 1000;
        for (int i = 0; i < numOctaves; i++)
        {
            // What does it mean ( * 2 - 1 )?
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

        // Original: Vector4, found it useless
        noiseDensityShader.SetVector("centre", new Vector3(centre.x, centre.y, centre.z));
        noiseDensityShader.SetInt("octaves", Mathf.Max(1, numOctaves));
        noiseDensityShader.SetFloat("lacunarity", lacunarity);
        noiseDensityShader.SetFloat("persistence", persistence);
        noiseDensityShader.SetFloat("noiseScale", noiseScale);
        noiseDensityShader.SetFloat("noiseWeight", noiseWeight);
        noiseDensityShader.SetBool("closeEdges", closeEdges);
        noiseDensityShader.SetBool("b1", b1);
        noiseDensityShader.SetBool("b2", b2);
        noiseDensityShader.SetFloat("f1", f1);
        noiseDensityShader.SetFloat("f2", f2);
        noiseDensityShader.SetFloat("f3", f3);
        noiseDensityShader.SetBuffer(0, "offsets", offsetsBuffer);
        noiseDensityShader.SetFloat("floorOffset", floorOffset);
        noiseDensityShader.SetFloat("heightGredient", heightGredient);
        noiseDensityShader.SetFloat("multiFractalWeight", multiFractalWeight);
        noiseDensityShader.SetVector("params", shaderParams);

        noiseDensityShader.SetBuffer(0, "points", pointsBuffer);
        noiseDensityShader.SetBuffer(0, "manualData", additionalPointsBuffer);
        noiseDensityShader.SetBuffer(0, "GroundLevelDataBuffer", chunk.groundLevelDataBuffer);
        noiseDensityShader.SetBuffer(0, "pointsStatus", pointsStatus);
        noiseDensityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        noiseDensityShader.SetFloat("boundsSize", boundsSize);
        noiseDensityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        noiseDensityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        noiseDensityShader.SetFloat("spacing", spacing);
        noiseDensityShader.SetFloat("isoLevel", isoLevel);
        noiseDensityShader.SetVector("worldSize", worldBounds);

        noiseDensityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        foreach (ComputeBuffer buffer in buffersToRelease)
        {
            buffer.Release();
        }
    }
}
