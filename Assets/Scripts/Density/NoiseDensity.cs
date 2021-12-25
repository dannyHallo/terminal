using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : DensityGenerator
{

    [Header("Noise")]
    public int seed;

    [Range(1, 20)]
    public int numOctaves = 4;
    public float lacunarity = 2;
    public float persistence = .5f;
    public float noiseScale = 1;
    public float noiseWeight = 1;
    public bool closeEdges;
    public float floorOffset = 1;
    public float weightMultiplier = 1;

    public float hardFloorHeight;
    public float hardFloorWeight;

    public Vector4 shaderParams;

    ComputeBuffer debugBuffer;

    public override ComputeBuffer Generate(ComputeBuffer pointsBuffer, ComputeBuffer additionalPointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 worldBounds, Vector3 centre, Vector3 offset, float spacing, float planetRadius, float isoLevel)
    {
        buffersToRelease = new List<ComputeBuffer>();

        ///     Noise parameters
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
            offsets[i] = new Vector3((float)prng.NextDouble() * 2 - 1, (float)prng.NextDouble() * 2 - 1, (float)prng.NextDouble() * 2 - 1) * offsetRange;
        }

        // Sets offset buffer
        var offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
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
        densityShader.SetBuffer(0, "offsets", offsetsBuffer);
        densityShader.SetFloat("floorOffset", floorOffset);
        densityShader.SetFloat("weightMultiplier", weightMultiplier);
        densityShader.SetFloat("hardFloor", hardFloorHeight);
        densityShader.SetFloat("hardFloorWeight", hardFloorWeight);
        densityShader.SetBuffer(0, "manualData", additionalPointsBuffer);
        densityShader.SetVector("params", shaderParams);

        return base.Generate(pointsBuffer, additionalPointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, spacing, planetRadius, isoLevel);
    }
}