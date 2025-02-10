using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using ReadMesh;
using Unity.Mathematics;
using System.Drawing;
using Unity.Burst;
using System;
using System.Diagnostics;

public class Slicer : MonoBehaviour
{
    [SerializeField] bool makeSlice = false;

    private void Update()
    {
        if (CheckSlice())
        {
            Stopwatch stopwatch = Stopwatch.StartNew(); // Start timer
            RunSlice();
            stopwatch.Stop(); // Stop timer
            UnityEngine.Debug.Log($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");

        }
        makeSlice = false;
    }
    
    bool CheckSlice()
    {
        return makeSlice;
    }

    void RunSlice()
    {
        //check for mesh included in slice
        List<Mesh> meshes = new List<Mesh>();
        List<Vector3> planePoints = new List<Vector3>();
        List<Vector3> planeNormals = new List<Vector3>();
        foreach (Sliceable sliceableObject in Sliceable.Sliceables)
        {
            Vector3 planePoint = sliceableObject.transform.InverseTransformPoint(transform.position);
            Vector3 planeNormal = sliceableObject.transform.InverseTransformDirection(transform.up);
            if (sliceableObject.IntersectsBounds(planePoint, planeNormal))
            {
                meshes.Add(sliceableObject.GetMesh());
                planePoints.Add(planePoint);
                planeNormals.Add(planeNormal);
            }

        }

        Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(meshes);

        for (int i = 0; i < meshDataArray.Length; i++)
        {
            //read data from mesh
            Mesh.MeshData data = meshDataArray[i];
            int vertexCount = data.vertexCount;
            int indexCount = data.GetSubMesh(0).indexCount;
            NativeArray<VertexData> vertices = new NativeArray<VertexData>(vertexCount, Allocator.Persistent);
            NativeArray<int> indices = new NativeArray<int>(indexCount, Allocator.Persistent);

            //read in mech data
            JobHandle readHandle = MeshInfo.ProcessMeshData(data, vertices, indices);

            //classify Vertices
            var vertexSides = new NativeArray<bool>(vertexCount, Allocator.Persistent);
            var classifyVerticesJob = new ClassifyVerticesJob
            {
                vertices = vertices,
                vertexSides = vertexSides,
                planePoint = planePoints[i],
                planeNormal = planeNormals[i],
            };
            var classifyVerticesHandle = classifyVerticesJob.Schedule(vertexCount, 64, readHandle);


            //classify triangles
            int trianglCount = indices.Length / 3;
            var TriangleType = new NativeArray<int>(trianglCount, Allocator.Persistent);
            var splitEdges = new NativeParallelHashMap<Edge, VertexData>(trianglCount,Allocator.Persistent);
            var classifyTrianglesJob = new ClassifyTrianglesJob
            {
                vertices = vertices,
                vertexSides = vertexSides,
                indices = indices,
                planePoint = planePoints[i],
                planeNormal = planeNormals[i],
                triangleType = TriangleType,
                splitEdges = splitEdges.AsParallelWriter()
            };
            var classifyTrianglesHandle = classifyTrianglesJob.Schedule(trianglCount, 64, classifyVerticesHandle);


            //split mesh
            var outputVertices1 = new NativeList<VertexData>(vertexCount, Allocator.Persistent);
            var outputVertices2 = new NativeList<VertexData>(vertexCount, Allocator.Persistent);
            var outputIndices1 = new NativeList<int>(indices.Length, Allocator.Persistent);
            var outputIndices2 = new NativeList<int>(indices.Length, Allocator.Persistent);
            var seperateMeshJob = new SeperateMeshJob
            {
                vertices = vertices,
                vertexSides = vertexSides,
                indices = indices,
                splitEdges = splitEdges,
                triangleType = TriangleType,
                outputVertices1 = outputVertices1,
                outputVertices2 = outputVertices2,
                outputIndices1 = outputIndices1,
                outputIndices2 = outputIndices2,
            };
            var seperateMeshHandle = seperateMeshJob.Schedule(classifyTrianglesHandle);

            seperateMeshHandle.Complete();


            foreach(var vertex in outputVertices1)
            {
                UnityEngine.Debug.DrawLine(vertex.position, vertex.position + vertex.normal);
            }


            splitEdges.Dispose();
            outputVertices1.Dispose();
            outputVertices2.Dispose();
            outputIndices1.Dispose();
            outputIndices2.Dispose();

        }

        
    }





    [BurstCompile]
    public struct ClassifyVerticesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<VertexData> vertices;
        [WriteOnly] public NativeArray<bool> vertexSides;

        public float3 planePoint;
        public float3 planeNormal;

        [BurstCompile]
        public void Execute(int index)
        {
            float3 offset = vertices[index].position - planePoint;
            float dotProduct = math.dot(planeNormal, offset);
            vertexSides[index] = dotProduct > 0.00001;
        }
    }



    public struct Edge : IEquatable<Edge>
    {
        public int index1;
        public int index2;

        public bool Equals(Edge other)
        {
            return index1 == other.index1 && index2 == other.index2;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Using a hash combining technique
                int hash = 17;
                hash = hash * 31 + index1;
                hash = hash * 31 + index2;
                return hash;
            }
        }
    }


    [BurstCompile]
    public struct ClassifyTrianglesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<VertexData> vertices;
        [ReadOnly] public NativeArray<bool> vertexSides;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> indices;
        public float3 planePoint;
        public float3 planeNormal;

        public NativeArray<int> triangleType;
        public NativeParallelHashMap<Edge, VertexData>.ParallelWriter splitEdges;

        [BurstCompile]
        float EdgePortion(float3 point1, float3 point2, float3 planePoint, float3 planeNormal)
        {
            float3 lineDir = point2 - point1;
            float dotProduct = math.dot(planeNormal, lineDir);
            float t = math.dot(planePoint - point1, planeNormal) / dotProduct;

            t = math.clamp(t, 0, 1);

            return t;
        }

        [BurstCompile]
        float3 EdgeIntercept(float3 point1, float3 point2, float t)
        {
            return point1 + t * (point2 - point1);
        }
        [BurstCompile]
        float2 EdgeUV(float2 uv1, float2 uv2, float t)
        {
            return uv1 + t * (uv2 - uv1);
        }
        [BurstCompile]
        float3 EdgeNormal(float3 normal1, float3 normal2, float t)
        {
            return normal1 + t * (normal2 - normal1);
        }

        [BurstCompile]
        public void Execute(int index)
        {
            int index1 = index * 3;
            int index2 = index * 3 + 1;
            int index3 = index * 3 + 2;

            int vertex1 = indices[index1];
            int vertex2 = indices[index2];
            int vertex3 = indices[index3];

            int side1 = vertexSides[vertex1] ? 1 : 0;
            int side2 = vertexSides[vertex2] ? 1 : 0;
            int side3 = vertexSides[vertex3] ? 1 : 0;

            int scenerio = (side3 << 2) + (side2 << 1) + side1;

            switch (scenerio)
            {
                case 0:
                    //No vertices in slice
                    triangleType[index] = 0;
                    break;
                case 1:
                    //only v1 in slice
                    triangleType[index] = 1;
                    break;
                case 2:
                    //only v2 in slice
                    triangleType[index] = 1;
                    indices[index1] = vertex2;
                    indices[index2] = vertex3;
                    indices[index3] = vertex1;
                    break;
                case 3:
                    //v1 and v2 in slice
                    triangleType[index] = 2;
                    indices[index1] = vertex3;
                    indices[index2] = vertex1;
                    indices[index3] = vertex2;
                    break;
                case 4:
                    //only v3 in slice
                    triangleType[index] = 1;
                    indices[index1] = vertex3;
                    indices[index2] = vertex1;
                    indices[index3] = vertex2;
                    break;
                case 5:
                    //v1 and v3 in slice
                    triangleType[index] = 2;
                    indices[index1] = vertex2;
                    indices[index2] = vertex3;
                    indices[index3] = vertex1;
                    break;
                case 6:
                    //v2 and v3 in slice
                    triangleType[index] = 2;
                    break;
                case 7:
                    //all vertices in slice
                    triangleType[index] = 3;
                    break;
            }

            vertex1 = indices[index1];
            vertex2 = indices[index2];
            vertex3 = indices[index3];

            if (triangleType[index] == 1 || triangleType[index] == 2)
            {
                Edge edge1to2 = new Edge { index1 = vertex1, index2 = vertex2 };
                float t = EdgePortion(vertices[vertex1].position, vertices[vertex2].position, planePoint, planeNormal);
                splitEdges.TryAdd(edge1to2, new VertexData
                {
                    position = EdgeIntercept(vertices[vertex1].position, vertices[vertex2].position, t),
                    normal = EdgeNormal(vertices[vertex1].normal, vertices[vertex2].normal, t),
                    uv0 = EdgeUV(vertices[vertex1].uv0, vertices[vertex2].uv0, t)
                });
                

                Edge edge1to3 = new Edge { index1 = vertex1, index2 = vertex3 };
                t = EdgePortion(vertices[vertex1].position, vertices[vertex2].position, planePoint, planeNormal);
                splitEdges.TryAdd(edge1to3, new VertexData
                {
                    position = EdgeIntercept(vertices[vertex1].position, vertices[vertex3].position, t),
                    normal = EdgeNormal(vertices[vertex1].normal, vertices[vertex3].normal, t),
                    uv0 = EdgeUV(vertices[vertex1].uv0, vertices[vertex3].uv0, t)
                });
            }
        }
    }



    [BurstCompile]
    public struct SeperateMeshJob : IJob
    {
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<VertexData> vertices;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<bool> vertexSides;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<int> indices;
        [ReadOnly] public NativeParallelHashMap<Edge, VertexData> splitEdges;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<int> triangleType;

        public NativeList<VertexData> outputVertices1;
        public NativeList<VertexData> outputVertices2;
        public NativeList<int> outputIndices1;
        public NativeList<int> outputIndices2;

        public void Execute()
        {
            // Create mapping of original indices to new output indices with extra capacity to reduce collisions
            NativeParallelHashMap<int, int> vertexMap1 = new NativeParallelHashMap<int, int>(vertices.Length * 2, Allocator.Temp);
            NativeParallelHashMap<int, int> vertexMap2 = new NativeParallelHashMap<int, int>(vertices.Length * 2, Allocator.Temp);
            NativeParallelHashMap<Edge, int> edgeMap1 = new NativeParallelHashMap<Edge, int>(splitEdges.Count() * 2, Allocator.Temp);
            NativeParallelHashMap<Edge, int> edgeMap2 = new NativeParallelHashMap<Edge, int>(splitEdges.Count() * 2, Allocator.Temp);

            // Process original vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertexSides[i])
                {
                    vertexMap2[i] = outputVertices2.Length;
                    outputVertices2.Add(vertices[i]);
                }
                else
                {
                    vertexMap1[i] = outputVertices1.Length;
                    outputVertices1.Add(vertices[i]);
                }
            }

            // Add split edge vertices to both outputs using GetKeyValueArrays
            NativeArray<Edge> splitEdgeKeys = splitEdges.GetKeyArray(Allocator.Temp);
            NativeArray<VertexData> splitEdgeValues = splitEdges.GetValueArray(Allocator.Temp);
            for (int i = 0; i < splitEdgeValues.Length; i++)
            {
                edgeMap1[splitEdgeKeys[i]] = outputVertices1.Length;
                edgeMap2[splitEdgeKeys[i]] = outputVertices2.Length;
                outputVertices1.Add(splitEdgeValues[i]);
                outputVertices2.Add(splitEdgeValues[i]);
            }
            splitEdgeKeys.Dispose();
            splitEdgeValues.Dispose();

            // --- TRIANGLE HANDLING LOGIC WOULD GO HERE ---

            // Dispose of hash maps
            vertexMap1.Dispose();
            vertexMap2.Dispose();
            edgeMap1.Dispose();
            edgeMap2.Dispose();
        }
    }
}
