using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine;


using TexCoord = TexCoord1;
using Vertex = VertexFull;


[Serializable, StructLayout(LayoutKind.Sequential), VertexVariant]
public struct VertexFull : 
    IVertex<Vertex, TexCoord>, 
    ITangent<Vertex, TexCoord>, 
    IColor<Vertex, TexCoord>, 
    IBoneWeights<Vertex, TexCoord>
{
    public Vector3 position;
    public Vector3 normal;
    public Vector4 tangent;
    public Vector4 color;
    public TexCoord uvs;
    public BoneWeight boneWeights;

    // Explicit Interface Implementations
    Vector3 IVertex<Vertex, TexCoord>.Position { get => position; set => position = value; }
    Vector3 IVertex<Vertex, TexCoord>.Normal { get => normal; set => normal = value; }
    Vector4 ITangent<Vertex, TexCoord>.Tangent { get => tangent; set => tangent = value; }
    Vector4 IColor<Vertex, TexCoord>.Color { get => color; set => color = value; }
    TexCoord IVertex<Vertex, TexCoord>.UVs { get => uvs; set => uvs = value; }
    BoneWeight IBoneWeights<Vertex, TexCoord>.BoneWeight { get => boneWeights; set => boneWeights = value; }

    //constructors
    public VertexFull(Vector3 position, Vector3 normal, Vector4 tangent, Vector4 color, TexCoord uvs, BoneWeight boneWeights)
    {
        this.position = position;
        this.normal = normal;
        this.tangent = tangent;
        this.color = color;
        this.uvs = uvs;
        this.boneWeights = boneWeights;
    }

    VertexFull(Stream0 s0, Stream1 s1, Stream2 s2)
    {
        this.position = s0.position;
        this.normal = s0.normal;
        this.tangent = s0.tangent;
        this.color = s1.color;
        this.uvs = s1.uvs;
        this.boneWeights = s2.weight;
    }

    //IVertex
    public Vertex Lerp(Vertex other, float t, bool clamp = true)
    {
        t = clamp ? Mathf.Clamp01(t) : t;

        Vertex newVertex = new Vertex(
            Vector3.Lerp(position, other.position, t),
            Vector3.Lerp(normal, other.normal, t),
            Vector4.Lerp(tangent, other.tangent, t),
            Vector4.Lerp(color, other.color, t),
            uvs.Lerp(other.uvs, t),
            VertexUtility.LerpBoneWeight(boneWeights, other.boneWeights, t)
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
        public Vector4 tangent;
    }

    struct Stream1
    {
        public Vector4 color;
        public TexCoord uvs;
    }

    struct Stream2
    {
        public BoneWeight weight;
    }

    // Readonly properties to access associated streams
    Stream0 stream0 => new Stream0
    {
        position = this.position,
        normal = this.normal,
        tangent = this.tangent
    };

    Stream1 stream1 => new Stream1
    {
        color = this.color,
        uvs = this.uvs
    };

    Stream2 stream2 => new Stream2
    {
        weight = this.boneWeights
    };


    // Static property to get accurate VertexAttributeDescriptor[]
    static VertexAttributeDescriptor[] VertexBufferDescriptors => new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, dimension:4, stream:0),

            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension:4, stream:1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2, stream:1),

            new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, dimension:4, stream:2),
            new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, dimension:4, stream:2),
        };

    public NativeArray<Vertex> GetVertices(Mesh mesh)
    {
        int vertexCount = mesh.vertexCount;

        //get readOnly access to mesh buffer data
        Mesh.MeshDataArray meshArray = Mesh.AcquireReadOnlyMeshData(mesh);
        Mesh.MeshData data = meshArray[0];
        NativeArray<Stream0> stream0Data = data.GetVertexData<Stream0>(0);
        NativeArray<Stream1> stream1Data = data.GetVertexData<Stream1>(1);
        NativeArray<Stream2> stream2Data = data.GetVertexData<Stream2>(2);

        //create VertexFull values from streams
        NativeArray<Vertex> vertexData = new NativeArray<Vertex>(vertexCount, Allocator.Temp);
        for (int i = 0; i < vertexCount; i++)
        {
            vertexData[i] = new Vertex(stream0Data[i], stream1Data[i], stream2Data[i]);
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
        NativeArray<Stream1> stream1Data = data.GetVertexData<Stream1>(1);
        NativeArray<Stream2> stream2Data = data.GetVertexData<Stream2>(2);

        // Set the data for each stream from the input vertices
        for (int i = 0; i < vertexCount; i++)
        {
            // Decompose the VertexFull object back into its individual streams
            stream0Data[i] = vertices[i].stream0;
            stream1Data[i] = vertices[i].stream1;
            stream2Data[i] = vertices[i].stream2;
        }

        // Apply changes back to the mesh data
        Mesh newMesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshArray, newMesh, MeshUpdateFlags.Default);

        // Return the updated mesh (Note: this method doesn't modify the mesh itself directly)
        return newMesh;
    }
}