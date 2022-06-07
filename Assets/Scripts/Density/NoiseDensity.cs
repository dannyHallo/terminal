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
    public ComputeShader densityShader;

    protected List<ComputeBuffer> buffersToRelease;

    void OnValidate()
    {
        if (!Application.isPlaying && FindObjectOfType<TerrainMesh>())
        {
            FindObjectOfType<TerrainMesh>().RequestMeshUpdate();
        }
    }

    public void Generate(
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
        densityShader.SetVector("centre", new Vector3(centre.x, centre.y, centre.z));
        densityShader.SetInt("octaves", Mathf.Max(1, numOctaves));
        densityShader.SetFloat("lacunarity", lacunarity);
        densityShader.SetFloat("persistence", persistence);
        densityShader.SetFloat("noiseScale", noiseScale);
        densityShader.SetFloat("noiseWeight", noiseWeight);
        densityShader.SetBool("closeEdges", closeEdges);
        densityShader.SetBool("b1", b1);
        densityShader.SetBool("b2", b2);
        densityShader.SetFloat("f1", f1);
        densityShader.SetFloat("f2", f2);
        densityShader.SetFloat("f3", f3);
        densityShader.SetBuffer(0, "offsets", offsetsBuffer);
        densityShader.SetFloat("floorOffset", floorOffset);
        densityShader.SetFloat("heightGredient", heightGredient);
        densityShader.SetFloat("multiFractalWeight", multiFractalWeight);
        densityShader.SetVector("params", shaderParams);

        densityShader.SetBuffer(0, "points", pointsBuffer);
        densityShader.SetBuffer(0, "manualData", additionalPointsBuffer);
        densityShader.SetBuffer(0, "pointsStatus", pointsStatus);
        densityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        densityShader.SetFloat("boundsSize", boundsSize);
        densityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        densityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        densityShader.SetFloat("spacing", spacing);
        densityShader.SetFloat("isoLevel", isoLevel);
        densityShader.SetVector("worldSize", worldBounds);

        densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        foreach (ComputeBuffer buffer in buffersToRelease)
        {
            buffer.Release();
        }
    }
}
