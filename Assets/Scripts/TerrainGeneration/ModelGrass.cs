using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;

public class ModelGrass : MonoBehaviour
{
    public int numGrassesPerAxis = 12;
    public Material grassMaterial;
    public Mesh grassMesh;
    public Mesh grassLODMesh;
    public bool noLOD;

    [Range(0, 1000.0f)]
    public float lodCutoff = 1000.0f;

    [Header("Wind")]
    public float windSpeed = 1.0f;
    public float frequency = 1.0f;
    public float windStrength = 1.0f;

    private bool inited = false;
    private ComputeShader GrassChunkPointShader,
        WindNoiseShader,
        cullGrassShader;
    private ComputeBuffer voteBuffer,
        scanBuffer,
        groupSumArrayBuffer,
        scannedGroupSumBuffer;

    private RenderTexture wind;

    private int grassesPerChunk,
        chunkDimension,
        numThreadGroups,
        numVoteThreadGroups,
        numGroupScanThreadGroups,
        numWindThreadGroups,
        numGrassInitThreadGroups;

    private struct GrassData
    {
        public Vector4 position;
        public Vector2 uv;
        public float displacement;
    }

    uint[] args;
    uint[] argsLOD;

    Bounds fieldBounds;

    public void Init(float chunkBoundSize)
    {
        if (inited)
            return;

        float spacing = chunkBoundSize / (numGrassesPerAxis - 1);

        grassesPerChunk = numGrassesPerAxis * numGrassesPerAxis;
        numThreadGroups = Mathf.CeilToInt(grassesPerChunk / 128.0f);

        // Make sure the num of threads are the power of two
        if (numThreadGroups > 128)
        {
            int powerOfTwo = 128;
            while (powerOfTwo < numThreadGroups)
                powerOfTwo *= 2;

            numThreadGroups = powerOfTwo;
        }
        else
        {
            while (128 % numThreadGroups != 0)
                numThreadGroups++;
        }

        numVoteThreadGroups = Mathf.CeilToInt(grassesPerChunk / 128.0f);
        numGroupScanThreadGroups = Mathf.CeilToInt(grassesPerChunk / 1024.0f);

        GrassChunkPointShader = Resources.Load<ComputeShader>("GrassChunkPoint");
        WindNoiseShader = Resources.Load<ComputeShader>("WindNoise");
        cullGrassShader = Resources.Load<ComputeShader>("CullGrass");

        voteBuffer = new ComputeBuffer(grassesPerChunk, 4);
        scanBuffer = new ComputeBuffer(grassesPerChunk, 4);
        groupSumArrayBuffer = new ComputeBuffer(numThreadGroups, 4);
        scannedGroupSumBuffer = new ComputeBuffer(numThreadGroups, 4);

        GrassChunkPointShader.SetFloat("chunkBoundSize", chunkBoundSize);
        GrassChunkPointShader.SetFloat("spacing", spacing);

        GrassChunkPointShader.SetInt("numGrassesPerAxis", numGrassesPerAxis);
        GrassChunkPointShader.SetInt("scale", numGrassesPerAxis);
        // GrassChunkPointShader.SetTexture(0, "_HeightMap", heightMap);

        wind = new RenderTexture(
            1024,
            1024,
            0,
            RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Linear
        );
        wind.enableRandomWrite = true;
        wind.Create();
        numWindThreadGroups = Mathf.CeilToInt(wind.height / 8.0f);

        args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)grassMesh.GetIndexCount(0);
        args[1] = (uint)0;
        args[2] = (uint)grassMesh.GetIndexStart(0);
        args[3] = (uint)grassMesh.GetBaseVertex(0);

        argsLOD = new uint[5] { 0, 0, 0, 0, 0 };
        argsLOD[0] = (uint)grassLODMesh.GetIndexCount(0);
        argsLOD[1] = (uint)0;
        argsLOD[2] = (uint)grassLODMesh.GetIndexStart(0);
        argsLOD[3] = (uint)grassLODMesh.GetBaseVertex(0);

        fieldBounds = new Bounds(
            Vector3.zero,
            new Vector3(-chunkBoundSize, chunkBoundSize * 2, chunkBoundSize)
        );
        inited = true;
    }

    public void InitializeGrassChunk(Chunk chunk, Vector3 centre)
    {
        if (!inited)
        {
            print("Please call GrassBufferInit before initialize grass chunk!");
            return;
        }

        // Create buffer to store grass mesh
        chunk.argsBuffer = new ComputeBuffer(
            1,
            5 * sizeof(uint),
            ComputeBufferType.IndirectArguments
        );
        chunk.argsBufferLOD = new ComputeBuffer(
            1,
            5 * sizeof(uint),
            ComputeBufferType.IndirectArguments
        );

        chunk.argsBuffer.SetData(args);
        chunk.argsBufferLOD.SetData(argsLOD);

        chunk.positionsBuffer = new ComputeBuffer(grassesPerChunk, SizeOf(typeof(GrassData)));
        chunk.culledPositionsBuffer = new ComputeBuffer(grassesPerChunk, SizeOf(typeof(GrassData)));

        GrassChunkPointShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        GrassChunkPointShader.SetBuffer(0, "_GrassDataBuffer", chunk.positionsBuffer);
        GrassChunkPointShader.Dispatch(0, numGrassesPerAxis, numGrassesPerAxis, 1);

        chunk.grassMaterial = new Material(grassMaterial);
        chunk.grassMaterial.SetBuffer("positionBuffer", chunk.culledPositionsBuffer);
        chunk.grassMaterial.SetTexture("_WindTex", wind);
        // chunk.grassMaterial.SetInt("_ChunkNum", xOffset + yOffset * numChunks);
    }

    void CullGrass(Chunk chunk, Matrix4x4 VP, bool noLOD)
    {
        //Reset Args
        if (noLOD)
            chunk.argsBuffer.SetData(args);
        else
            chunk.argsBufferLOD.SetData(argsLOD);

        float distanceCutoff = 10000f;

        // Vote
        cullGrassShader.SetMatrix("MATRIX_VP", VP);
        cullGrassShader.SetBuffer(0, "_GrassDataBuffer", chunk.positionsBuffer);
        cullGrassShader.SetBuffer(0, "_VoteBuffer", voteBuffer);
        cullGrassShader.SetVector("_CameraPosition", Camera.main.transform.position);
        cullGrassShader.SetFloat("_Distance", distanceCutoff);
        cullGrassShader.Dispatch(0, numVoteThreadGroups, 1, 1);

        // Scan Instances
        cullGrassShader.SetBuffer(1, "_VoteBuffer", voteBuffer);
        cullGrassShader.SetBuffer(1, "_ScanBuffer", scanBuffer);
        cullGrassShader.SetBuffer(1, "_GroupSumArray", groupSumArrayBuffer);
        cullGrassShader.Dispatch(1, numThreadGroups, 1, 1);

        // Scan Groups
        cullGrassShader.SetInt("_NumOfGroups", numThreadGroups);
        cullGrassShader.SetBuffer(2, "_GroupSumArrayIn", groupSumArrayBuffer);
        cullGrassShader.SetBuffer(2, "_GroupSumArrayOut", scannedGroupSumBuffer);
        cullGrassShader.Dispatch(2, numGroupScanThreadGroups, 1, 1);

        // Compact
        cullGrassShader.SetBuffer(3, "_GrassDataBuffer", chunk.positionsBuffer);
        cullGrassShader.SetBuffer(3, "_VoteBuffer", voteBuffer);
        cullGrassShader.SetBuffer(3, "_ScanBuffer", scanBuffer);
        cullGrassShader.SetBuffer(3, "_ArgsBuffer", noLOD ? chunk.argsBuffer : chunk.argsBufferLOD);
        cullGrassShader.SetBuffer(3, "_CulledGrassOutputBuffer", chunk.culledPositionsBuffer);
        cullGrassShader.SetBuffer(3, "_GroupSumArray", scannedGroupSumBuffer);
        cullGrassShader.Dispatch(3, numThreadGroups, 1, 1);
    }

    public void GenerateWind()
    {
        WindNoiseShader.SetTexture(0, "_WindMap", wind);
        WindNoiseShader.SetFloat("_Time", Time.time);
        WindNoiseShader.SetFloat("_Frequency", frequency);
        WindNoiseShader.SetFloat("_Amplitude", windStrength);
        WindNoiseShader.Dispatch(0, numWindThreadGroups, numWindThreadGroups, 1);
    }

    public void DrawAllGrass(List<Chunk> chunks)
    {
        Matrix4x4 P = Camera.main.projectionMatrix;
        Matrix4x4 V = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = P * V;

        foreach (Chunk chunk in chunks)
        {
            if (chunk.argsBuffer != null)
            {
                DrawGrassOnChunk(chunk, VP);
            }
        }
    }

    private void DrawGrassOnChunk(Chunk chunk, Matrix4x4 VP)
    {
        // float dist = Vector3.Distance(Camera.main.transform.position, chunks[i].bounds.center);
        // bool noLOD = dist < lodCutoff;

        bool noLOD = this.noLOD;
        CullGrass(chunk, VP, noLOD);

        if (noLOD)
            Graphics.DrawMeshInstancedIndirect(
                grassMesh,
                0,
                chunk.grassMaterial,
                fieldBounds,
                chunk.argsBuffer
            );
        else
            Graphics.DrawMeshInstancedIndirect(
                grassLODMesh,
                0,
                chunk.grassMaterial,
                fieldBounds,
                chunk.argsBufferLOD
            );
    }

    public void ClearGrassBuffer(List<Chunk> chunks)
    {
        if (!inited)
            return;

        foreach (Chunk chunk in chunks)
            FreeChunk(chunk);

        voteBuffer.Release();
        scanBuffer.Release();
        groupSumArrayBuffer.Release();
        scannedGroupSumBuffer.Release();
        wind.Release();
        wind = null;
        scannedGroupSumBuffer = null;
        voteBuffer = null;
        scanBuffer = null;
        groupSumArrayBuffer = null;
    }

    void FreeChunk(Chunk chunk)
    {
        if (chunk.positionsBuffer == null)
            return;

        chunk.positionsBuffer.Release();
        chunk.positionsBuffer = null;
        chunk.culledPositionsBuffer.Release();
        chunk.culledPositionsBuffer = null;
        chunk.argsBuffer.Release();
        chunk.argsBuffer = null;
        chunk.argsBufferLOD.Release();
        chunk.argsBufferLOD = null;
    }
}
