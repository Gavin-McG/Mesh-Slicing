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
using UnityEngine.Rendering;

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

        var meshDataArray = Mesh.AcquireReadOnlyMeshData(meshes);
        var newMeshDataArray = Mesh.AllocateWritableMeshData(2*meshDataArray.Length);

        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(meshDataArray.Length,Allocator.TempJob);
        for (int i = 0; i < meshDataArray.Length; i++)
        {
            //read data from mesh
            Mesh.MeshData data = meshDataArray[i];
            int vertexCount = data.vertexCount;
            int indexCount = data.GetSubMesh(0).indexCount;

            //read in mech data
            NativeArray<VertexData> vertices = new NativeArray<VertexData>(vertexCount, Allocator.TempJob);
            NativeArray<int> indices = new NativeArray<int>(indexCount, Allocator.TempJob);
            JobHandle parseMeshHandle = MeshInfo.ProcessMeshData(data, vertices, indices);

            //classify Vertices
            var vertexSides = new NativeArray<bool>(vertexCount, Allocator.TempJob);
            var classifyVerticesJob = new ClassifyVerticesJob
            {
                vertices = vertices,
                vertexSides = vertexSides,
                planePoint = planePoints[i],
                planeNormal = planeNormals[i],
            };
            var classifyVerticesHandle = classifyVerticesJob.Schedule(vertexCount, 64, parseMeshHandle);


            //classify triangles
            int trianglCount = indices.Length / 3;
            var TriangleType = new NativeArray<int>(trianglCount, Allocator.TempJob);
            var splitEdges = new NativeParallelHashMap<Edge, VertexData>(trianglCount*3,Allocator.TempJob);
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
            var outputVertices1 = new NativeList<VertexData>(vertexCount + 2 * trianglCount, Allocator.TempJob);
            var outputVertices2 = new NativeList<VertexData>(vertexCount + 2 * trianglCount, Allocator.TempJob);
            var outputIndices1 = new NativeList<int>(3 * indices.Length, Allocator.TempJob);
            var outputIndices2 = new NativeList<int>(3 * indices.Length, Allocator.TempJob);
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
            splitEdges.Dispose(seperateMeshHandle);



            //create new meshes
            Mesh.MeshData data1 = newMeshDataArray[i * 2];
            Mesh.MeshData data2 = newMeshDataArray[i * 2 + 1];
            MeshAssignJob meshAssignJob1 = new MeshAssignJob
            {
                data = data1,
                vertices = outputVertices1,
                indices = outputIndices1
            };
            MeshAssignJob meshAssignJob2 = new MeshAssignJob
            {
                data = data2,
                vertices = outputVertices2,
                indices = outputIndices2
            };
            JobHandle meshAssignHandle1 = meshAssignJob1.Schedule(seperateMeshHandle);
            outputVertices1.Dispose(meshAssignHandle1);
            outputIndices1.Dispose(meshAssignHandle1);
            JobHandle meshAssignHandle2 = meshAssignJob2.Schedule(seperateMeshHandle);
            outputVertices2.Dispose(meshAssignHandle2);
            outputIndices2.Dispose(meshAssignHandle2);
            JobHandle meshAssignHndle = JobHandle.CombineDependencies(meshAssignHandle1, meshAssignHandle2);

            handles[i] = meshAssignHndle;
        }

        JobHandle.CompleteAll(handles);

        List<Mesh> newMeshes = new List<Mesh>();
        for (int i = 0; i < newMeshDataArray.Length; i++) newMeshes.Add(new Mesh());
        Mesh.ApplyAndDisposeWritableMeshData(newMeshDataArray, newMeshes);

        newMeshes[0].RecalculateBounds();
        GameObject.Find("Empty").GetComponent<MeshFilter>().mesh = newMeshes[0];
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
            vertexSides[index] = dotProduct > 0;
        }
    }



    public struct Edge : IEquatable<Edge>
    {
        public int index1;
        public int index2;

        public bool Equals(Edge other)
        {
            return (index1 == other.index1 && index2 == other.index2) ||
                (index2 == other.index1 && index1 == other.index2);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Using a hash combining technique
                int hash = 17;
                hash = hash * 31 + (index1 * index2);
                hash = hash * 31 + (index1 + index2);
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
                t = EdgePortion(vertices[vertex1].position, vertices[vertex3].position, planePoint, planeNormal);
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
            NativeHashMap<int, int> vertexMap = new NativeHashMap<int, int>(vertices.Length * 2, Allocator.Temp);
            NativeHashMap<Edge, int> edgeMap1 = new NativeHashMap<Edge, int>(splitEdges.Count() * 2, Allocator.Temp);
            NativeHashMap<Edge, int> edgeMap2 = new NativeHashMap<Edge, int>(splitEdges.Count() * 2, Allocator.Temp);

            // Process original vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertexSides[i])
                {
                    vertexMap.TryAdd(i, outputVertices2.Length);
                    outputVertices2.AddNoResize(vertices[i]);
                }
                else
                {
                    vertexMap.TryAdd(i, outputVertices1.Length);
                    outputVertices1.AddNoResize(vertices[i]);
                }
            }

            // Add split edge vertices to both outputs using GetKeyValueArrays
            NativeArray<Edge> splitEdgeKeys = splitEdges.GetKeyArray(Allocator.Temp);
            NativeArray<VertexData> splitEdgeValues = splitEdges.GetValueArray(Allocator.Temp);
            for (int i = 0; i < splitEdgeValues.Length; i++)
            {
                edgeMap1.TryAdd(splitEdgeKeys[i], outputVertices1.Length);
                edgeMap2.TryAdd(splitEdgeKeys[i], outputVertices2.Length);
                outputVertices1.AddNoResize(splitEdgeValues[i]);
                outputVertices2.AddNoResize(splitEdgeValues[i]);
            }
            splitEdgeKeys.Dispose();
            splitEdgeValues.Dispose();

            //add all triangles to each mesh
            for (int i=0; i<triangleType.Length; i++)
            {
                int vertex1 = indices[i * 3];
                int vertex2 = indices[i * 3 + 1];
                int vertex3 = indices[i * 3 + 2];

                int outputVertex1, outputVertex2, outputVertex3;
                vertexMap.TryGetValue(vertex1, out outputVertex1);
                vertexMap.TryGetValue(vertex2, out outputVertex2);
                vertexMap.TryGetValue(vertex3, out outputVertex3);

                int edgeVertex1to2_1, edgeVertex1to3_1;
                edgeMap1.TryGetValue(new Edge { index1 = vertex1, index2 = vertex2 }, out edgeVertex1to2_1);
                edgeMap1.TryGetValue(new Edge { index1 = vertex1, index2 = vertex3 }, out edgeVertex1to3_1);

                int edgeVertex1to2_2, edgeVertex1to3_2;
                edgeMap2.TryGetValue(new Edge { index1 = vertex1, index2 = vertex2 }, out edgeVertex1to2_2);
                edgeMap2.TryGetValue(new Edge { index1 = vertex1, index2 = vertex3 }, out edgeVertex1to3_2);

                switch (triangleType[i])
                {
                    case 0:
                        outputIndices1.AddNoResize(outputVertex1);
                        outputIndices1.AddNoResize(outputVertex2);
                        outputIndices1.AddNoResize(outputVertex3);
                        break;
                    case 1:
                        outputIndices2.AddNoResize(outputVertex1);
                        outputIndices2.AddNoResize(edgeVertex1to2_2);
                        outputIndices2.AddNoResize(edgeVertex1to3_2);

                        outputIndices1.AddNoResize(edgeVertex1to2_1);
                        outputIndices1.AddNoResize(outputVertex2);
                        outputIndices1.AddNoResize(outputVertex3);

                        outputIndices1.AddNoResize(edgeVertex1to2_1);
                        outputIndices1.AddNoResize(outputVertex3);
                        outputIndices1.AddNoResize(edgeVertex1to3_1);
                        break;
                    case 2:
                        outputIndices1.AddNoResize(outputVertex1);
                        outputIndices1.AddNoResize(edgeVertex1to2_1);
                        outputIndices1.AddNoResize(edgeVertex1to3_1);

                        outputIndices2.AddNoResize(edgeVertex1to2_2);
                        outputIndices2.AddNoResize(outputVertex2);
                        outputIndices2.AddNoResize(outputVertex3);

                        outputIndices2.AddNoResize(edgeVertex1to2_2);
                        outputIndices2.AddNoResize(outputVertex3);
                        outputIndices2.AddNoResize(edgeVertex1to3_2);
                        break;
                    case 3:
                        outputIndices2.AddNoResize(outputVertex1);
                        outputIndices2.AddNoResize(outputVertex2);
                        outputIndices2.AddNoResize(outputVertex3);
                        break;
                }
            }

            // Dispose of hash maps
            vertexMap.Dispose();
            edgeMap1.Dispose();
            edgeMap2.Dispose();
        }
    }
    



    struct MeshAssignJob : IJob
    {
        public Mesh.MeshData data;
        [ReadOnly] public NativeList<VertexData> vertices;
        [ReadOnly] public NativeList<int> indices;

        public void Execute()
        {
            data.SetVertexBufferParams(vertices.Length,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2, 0)
            );
            data.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);

            data.GetVertexData<VertexData>(0).CopyFrom(vertices.AsArray());
            data.GetIndexData<int>().CopyFrom(indices.AsArray());
            data.subMeshCount = 1;
            data.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length));
        }
    }
}
