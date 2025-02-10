using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct VertexData
{
    public float3 position;
    public float3 normal;
    public float2 uv0;
}

namespace ReadMesh
{
    [BurstCompile]
    struct GetVerticesJob : IJob
    {
        public Mesh.MeshData meshData;
        public NativeArray<float3> positions;

        [BurstCompile]
        public void Execute()
        {
            NativeArray<Vector3> tempPositions = new NativeArray<Vector3>(positions.Length, Allocator.Temp);
            meshData.GetVertices(tempPositions);
            positions.CopyFrom(tempPositions.Reinterpret<float3>());
            tempPositions.Dispose();
        }
    }

    [BurstCompile]
    struct GetNormalsJob : IJob
    {
        public Mesh.MeshData meshData;
        public NativeArray<float3> normals;

        [BurstCompile]
        public void Execute()
        {
            NativeArray<Vector3> tempNormals = new NativeArray<Vector3>(normals.Length, Allocator.Temp);
            meshData.GetNormals(tempNormals);
            normals.CopyFrom(tempNormals.Reinterpret<float3>());
            tempNormals.Dispose();
        }
    }

    [BurstCompile]
    struct GetUVsJob : IJob
    {
        public Mesh.MeshData meshData;
        public NativeArray<float2> uvs;
        public int channel;

        [BurstCompile]
        public void Execute()
        {
            NativeArray<Vector2> tempUVs = new NativeArray<Vector2>(uvs.Length, Allocator.Temp);
            meshData.GetUVs(channel, tempUVs); ;
            uvs.CopyFrom(tempUVs.Reinterpret<float2>());
            tempUVs.Dispose();
        }
    }

    [BurstCompile]
    struct PopulateVertexDataJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float3> positions;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float3> normals;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float2> uv0;
        public NativeArray<VertexData> vertices;

        [BurstCompile]
        public void Execute(int index)
        {
            vertices[index] = new VertexData
            {
                position = index < positions.Length ? positions[index] : float3.zero,
                normal = index < normals.Length ? normals[index] : float3.zero,
                uv0 = index < uv0.Length ? uv0[index] : float2.zero
            };
        }
    }

    [BurstCompile]
    struct GetIndicesJob : IJob
    {
        public Mesh.MeshData meshData;
        public NativeArray<int> indices;
        public int submesh;

        [BurstCompile]
        public void Execute()
        {
            meshData.GetIndices(indices, 0);
        }
    }

    public static class MeshInfo
    {
        public static JobHandle ProcessMeshData(Mesh.MeshData data, NativeArray<VertexData> vertices, NativeArray<int> indices)
        {
            int vertexCount = data.vertexCount;
            int indexCount = data.GetSubMesh(0).indexCount;

            // Allocate arrays
            NativeArray<float3> positions = new NativeArray<float3>(vertexCount, Allocator.TempJob);
            NativeArray<float3> normals = new NativeArray<float3>(vertexCount, Allocator.TempJob);
            NativeArray<float2> uv0 = new NativeArray<float2>(vertexCount, Allocator.TempJob);

            // Schedule data extraction jobs
            GetVerticesJob verticesJob = new GetVerticesJob { meshData = data, positions = positions };
            GetNormalsJob normalsJob = new GetNormalsJob { meshData = data, normals = normals };
            GetUVsJob uvsJob = new GetUVsJob { meshData = data, uvs = uv0 };
            GetIndicesJob indicesJob = new GetIndicesJob { meshData = data, indices = indices };

            NativeList<JobHandle> handles = new NativeList<JobHandle>(Allocator.Persistent)
            {
                verticesJob.Schedule(),
                normalsJob.Schedule(),
                uvsJob.Schedule(),
                indicesJob.Schedule()
            };

            // Ensure all attribute jobs complete before processing vertices
            JobHandle allAttributesHandle = JobHandle.CombineDependencies(handles.AsArray());
            handles.Dispose();

            // Schedule PopulateVertexDataJob
            PopulateVertexDataJob populateJob = new PopulateVertexDataJob
            {
                positions = positions,
                normals = normals,
                uv0 = uv0,
                vertices = vertices
            };
            JobHandle populateHandle = populateJob.Schedule(vertexCount, 64, allAttributesHandle);

            return populateHandle;
        }
    }
}


