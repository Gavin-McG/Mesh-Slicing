using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine;

namespace MeshSlicing.Vertex
{

    using TexCoord = TexCoord1;
    using Vertex = VertexBasic;


    [Serializable, StructLayout(LayoutKind.Sequential), VertexVariant]
    public struct VertexBasic :
        IVertex<Vertex, TexCoord>
    {
        public Vector3 position;
        public Vector3 normal;
        public TexCoord uvs;

        // Explicit Interface Implementations
        Vector3 IVertex<Vertex, TexCoord>.Position { get => position; set => position = value; }
        Vector3 IVertex<Vertex, TexCoord>.Normal { get => normal; set => normal = value; }
        TexCoord IVertex<Vertex, TexCoord>.UVs { get => uvs; set => uvs = value; }

        //constructors
        public VertexBasic(Vector3 position, Vector3 normal, TexCoord uvs)
        {
            this.position = position;
            this.normal = normal;
            this.uvs = uvs;
        }

        VertexBasic(Stream0 s0)
        {
            this.position = s0.position;
            this.normal = s0.normal;
            this.uvs = s0.uvs;
        }

        //IVertex
        public Vertex Lerp(Vertex other, float t, bool clamp = true)
        {
            t = clamp ? Mathf.Clamp01(t) : t;

            Vertex newVertex = new Vertex(
                Vector3.Lerp(position, other.position, t),
                Vector3.Lerp(normal, other.normal, t),
                uvs.Lerp(other.uvs, t)
            );

            return newVertex;
        }

        public bool Equals(Vertex other)
        {
            return position == other.position;
        }

        //streams
        struct Stream0
        {
            public Vector3 position;
            public Vector3 normal;
            public TexCoord uvs;
        }

        // Readonly properties to access associated streams
        Stream0 stream0 => new Stream0
        {
            position = this.position,
            normal = this.normal,
            uvs = this.uvs
        };


        // Static property to get accurate VertexAttributeDescriptor[]
        static VertexAttributeDescriptor[] VertexBufferDescriptors => new VertexAttributeDescriptor[]
            {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2, stream:0),
            };

        public NativeArray<Vertex> GetVertices(Mesh mesh)
        {
            int vertexCount = mesh.vertexCount;

            //get readOnly access to mesh buffer data
            Mesh.MeshDataArray meshArray = Mesh.AcquireReadOnlyMeshData(mesh);
            Mesh.MeshData data = meshArray[0];
            NativeArray<Stream0> stream0Data = data.GetVertexData<Stream0>(0);

            //create VertexFull values from streams
            NativeArray<Vertex> vertexData = new NativeArray<Vertex>(vertexCount, Allocator.Temp);
            for (int i = 0; i < vertexCount; i++)
            {
                vertexData[i] = new Vertex(stream0Data[i]);
            }

            meshArray.Dispose();
            return vertexData;
        }

        public Mesh SetMeshVertices(List<Vertex> vertices)
        {
            int vertexCount = vertices.Count;

            // Allocate writable mesh data for the mesh
            Mesh.MeshDataArray meshArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData data = meshArray[0];
            data.SetVertexBufferParams(vertexCount, VertexBufferDescriptors);

            // Get the vertex streams (assuming there are 4 streams as seen in GetVertices)
            NativeArray<Stream0> stream0Data = data.GetVertexData<Stream0>(0);

            // Set the data for each stream from the input vertices
            for (int i = 0; i < vertexCount; i++)
            {
                // Decompose the VertexFull object back into its individual streams
                stream0Data[i] = vertices[i].stream0;
            }

            // Apply changes back to the mesh data
            Mesh newMesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(meshArray, newMesh, MeshUpdateFlags.Default);

            // Return the updated mesh (Note: this method doesn't modify the mesh itself directly)
            return newMesh;
        }
    }
}