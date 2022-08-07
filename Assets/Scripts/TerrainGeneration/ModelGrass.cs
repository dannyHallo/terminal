using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;

public class ModelGrass : MonoBehaviour
{
    [Header("8n")]
    public int numGrassesPerAxis = 12;
    public Material grassMaterial;
    public Mesh grassMesh;
    public Mesh grassLODMesh;
    public bool noLOD;

    [Range(0, 1000.0f)]
    public float lodCutoff = 1000.0f;

    private ComputeShader grassChunkPointShader, cullGrassShader;
    private ComputeBuffer voteBuffer,
        scanBuffer,
        groupSumArrayBuffer,
        scannedGroupSumBuffer;

    private int numGrassesPerChunk,
        chunkDimension,
        numThreadGroups,
        numVoteThreadGroups,
        numGroupScanThreadGroups,
        numWindThreadGroups,
        numGrassInitThreadGroups;

    private struct GrassData
    {
        Vector4 position;
        bool enable;
    };

    public struct GroundLevelData
    {
        public float twodeeHeight;
        public bool isDirt;
    };

    uint[] args;
    uint[] argsLOD;
    private RenderTexture universalRenderTex;

    private ColourGenerator2D colourGenerator2D
    {
        get
        {
            return gameObject.GetComponent<ColourGenerator2D>();
        }
    }


    Bounds fieldBounds;

    float chunkBoundSize
    {
        get
        {
            return gameObject.GetComponent<TerrainMesh>().boundSize;
        }
    }

    int numPointsPerAxis
    {
        get
        {
            return gameObject.GetComponent<TerrainMesh>().lodSetup.numPointsPerAxis;
        }
    }

    public void InitIfNeeded()
    {
        if (voteBuffer != null)
            return;

        float grassSpacing = chunkBoundSize / numGrassesPerAxis;
        numGrassesPerChunk = numGrassesPerAxis * numGrassesPerAxis;
        numThreadGroups = Mathf.CeilToInt(numGrassesPerChunk / 128.0f);

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

        numVoteThreadGroups = Mathf.CeilToInt(numGrassesPerChunk / 128.0f);
        numGroupScanThreadGroups = Mathf.CeilToInt(numGrassesPerChunk / 1024.0f);

        grassChunkPointShader = Resources.Load<ComputeShader>("GrassChunkPoint");
        cullGrassShader = Resources.Load<ComputeShader>("CullGrass");

        voteBuffer = new ComputeBuffer(numGrassesPerChunk, 4);
        scanBuffer = new ComputeBuffer(numGrassesPerChunk, 4);
        groupSumArrayBuffer = new ComputeBuffer(numThreadGroups, 4);
        scannedGroupSumBuffer = new ComputeBuffer(numThreadGroups, 4);

        grassChunkPointShader.SetFloat("chunkBoundSize", chunkBoundSize);

        grassChunkPointShader.SetFloat("grassSpacing", grassSpacing);

        grassChunkPointShader.SetInt("numGrassesPerAxis", numGrassesPerAxis);
        grassChunkPointShader.SetInt("numPointsPerAxis", numPointsPerAxis);

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

    }

    public void InitializeGrassChunkIfNeeded(Chunk chunk, Vector3 centre, int numPointsPerAxis, Vector3 offset, float meshSpacing)
    {
        if (voteBuffer == null)
        {
            InitIfNeeded();
        }

        chunk.FreeBuffers();

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
        chunk.groundLevelDataBuffer = new ComputeBuffer(numPointsPerAxis * numPointsPerAxis * numPointsPerAxis,
                    SizeOf(typeof(GroundLevelData)));

        chunk.argsBuffer.SetData(args);
        chunk.argsBufferLOD.SetData(argsLOD);

        chunk.positionsBuffer = new ComputeBuffer(numGrassesPerChunk, SizeOf(typeof(GrassData)));
        chunk.culledPositionsBuffer = new ComputeBuffer(numGrassesPerChunk, SizeOf(typeof(GrassData)));

        grassChunkPointShader.SetVector("centre", centre);
        grassChunkPointShader.SetVector("offset", offset);
        grassChunkPointShader.SetFloat("meshSpacing", meshSpacing);

        grassChunkPointShader.SetBuffer(0, "GroundLevelDataBuffer", chunk.groundLevelDataBuffer);
        grassChunkPointShader.SetBuffer(1, "GroundLevelDataBuffer", chunk.groundLevelDataBuffer);
        grassChunkPointShader.SetBuffer(1, "_GrassDataBuffer", chunk.positionsBuffer);

        chunk.grassMaterial = new Material(grassMaterial);
        chunk.grassMaterial.SetBuffer("positionBuffer", chunk.culledPositionsBuffer);
    }

    public void CalculateChunkGrassPos(Chunk chunk)
    {
        Texture2D userTex = colourGenerator2D.userTex;

        if (!universalRenderTex)
        {
            universalRenderTex = new RenderTexture(userTex.width, userTex.height, 0);
            universalRenderTex.enableRandomWrite = true;
            universalRenderTex.Create();
        }
        Graphics.Blit(userTex, universalRenderTex);

        float ratio = 1 / (2.0f * colourGenerator2D.worldPosOffset);
        float worldPosOffset = colourGenerator2D.worldPosOffset;
        float textureSize = userTex.width;
        grassChunkPointShader.SetFloat("ratio", ratio);
        grassChunkPointShader.SetFloat("worldPosOffset", worldPosOffset);
        grassChunkPointShader.SetFloat("textureSize", textureSize);
        grassChunkPointShader.SetTexture(1, "image", universalRenderTex);

        int meshThreadGroupNum = Mathf.CeilToInt(numPointsPerAxis / 8.0f);
        int grassThreadGroupNum = Mathf.CeilToInt(numGrassesPerAxis / 8.0f);

        grassChunkPointShader.SetFloats("requiredColor", ColourGenerator2D.natureColor);
        grassChunkPointShader.Dispatch(0, meshThreadGroupNum, meshThreadGroupNum, meshThreadGroupNum);
        grassChunkPointShader.Dispatch(1, grassThreadGroupNum, grassThreadGroupNum, 1);
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
        float dist = Vector3.Distance(Camera.main.transform.position, CentreFromCoord(chunk.coord));
        bool noLOD = dist < lodCutoff;
        // bool noLOD = this.noLOD;

        CullGrass(chunk, VP, noLOD);

        fieldBounds = new Bounds(
            Vector3.zero,
            new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)
        );

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

    Vector3 CentreFromCoord(Vector3Int coord)
    {
        return new Vector3(coord.x, coord.y, coord.z) * chunkBoundSize;
    }

    public void ClearGrassBufferIfNeeded()
    {
        if (voteBuffer == null)
            return;

        voteBuffer.Release();
        voteBuffer = null;

        scanBuffer.Release();
        scanBuffer = null;

        groupSumArrayBuffer.Release();
        groupSumArrayBuffer = null;

        scannedGroupSumBuffer.Release();
        scannedGroupSumBuffer = null;
    }


}
