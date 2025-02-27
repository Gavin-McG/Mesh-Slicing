using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


[Serializable, StructLayout(LayoutKind.Sequential)]
public struct VertexFull : IVertex<VertexFull>, ITangent<VertexFull>, IColor<VertexFull>, IBoneWeights<VertexFull>
{
    public Vector3 position;
    public Vector3 normal;
    public Vector4 tangent;
    public Vector4 color;
    public Vector2 uv;
    public BoneWeight boneWeights;

    //streams
    public struct Stream0
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
    }

    public struct Stream1
    {
        public Vector4 color;
        public Vector2 uv;
    }

    public struct Stream2
    {
        public BoneWeight weight;
    }

    public struct Stream3 { }

    // Explicit Interface Implementations
    Vector3 IVertex<VertexFull>.position { get => position; set => position = value; }
    Vector3 IVertex<VertexFull>.normal { get => normal; set => normal = value; }
    Vector2 IVertex<VertexFull>.uv { get => uv; set => uv = value; }
    Vector4 ITangent<VertexFull>.tangent { get => tangent; set => tangent = value; }
    Vector4 IColor<VertexFull>.color { get => color; set => color = value; }
    BoneWeight IBoneWeights<VertexFull>.boneWeights { get => boneWeights; set => boneWeights = value; }

    //constructors
    public VertexFull(Vector3 position, Vector3 normal, Vector4 tangent, Color color, Vector2 uv, BoneWeight boneWeights)
    {
        this.position = position;
        this.normal = normal;
        this.tangent = tangent;
        this.color = color;
        this.uv = uv;
        this.boneWeights = boneWeights;
    }

    public VertexFull(Stream0 s0, Stream1 s1, Stream2 s2, Stream3 s3)
    {
        this.position = s0.position;
        this.normal = s0.normal;
        this.tangent = s0.tangent;
        this.color = s1.color;
        this.uv = s1.uv;
        this.boneWeights = s2.weight;
    }

    //IVertex
    public VertexFull Lerp(VertexFull other, float t, bool clamp = true)
    {
        t = clamp ? Mathf.Clamp01(t) : t;

        VertexFull newVertex = new VertexFull(
            Vector3.Lerp(position, other.position, t),
            Vector3.Lerp(normal, other.normal, t),
            Vector4.Lerp(tangent, other.tangent, t),
            Color.Lerp(color, other.color, t),
            Vector2.Lerp(uv, other.uv, t),
            VertexUtility.LerpBoneWeight(boneWeights, other.boneWeights, t)
        );

        newVertex.normal.Normalize();
        newVertex.tangent.Normalize();

        return newVertex;
    }

    public bool Equals(VertexFull other)
    {
        return position == other.position;
    }

    public int GetHash()
    {
        int x = Mathf.FloorToInt(position.x / IVertex<VertexFull>.bucketSize);
        int y = Mathf.FloorToInt(position.y / IVertex<VertexFull>.bucketSize);
        int z = Mathf.FloorToInt(position.z / IVertex<VertexFull>.bucketSize);

        return HashCode.Combine(x, y, z);
    }

    public override string ToString()
    {
        return position.ToString();
    }

    public string ToString(string s, IFormatProvider format)
    {
        return position.ToString(s, format);
    }

    // Readonly properties to access associated streams
    public Stream0 stream0 => new Stream0
    {
        position = this.position,
        normal = this.normal,
        tangent = this.tangent
    };

    public Stream1 stream1 => new Stream1
    {
        color = this.color,
        uv = this.uv
    };

    public Stream2 stream2 => new Stream2
    {
        weight = this.boneWeights
    };

    public Stream3 stream3 => new Stream3();

    // Static property to get accurate VertexAttributeDescriptor[]
    public static VertexAttributeDescriptor[] VertexBufferDescriptors => new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, dimension:4, stream:0),

            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension:4, stream:1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2, stream:1),

            new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, dimension:3, stream:2),
            new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.Float32, dimension:3, stream:0),
        };
}




public static partial class MeshUtility
{
    public static void GetVertices(Mesh mesh, out NativeArray<VertexFull> vertexData)
    {
        int vertexCount = mesh.vertexCount;

        //get readOnly access to mesh buffer data
        Mesh.MeshDataArray meshArray = Mesh.AcquireReadOnlyMeshData(mesh);
        Mesh.MeshData data = meshArray[0];
        NativeArray<VertexFull.Stream0> stream0Data = data.GetVertexData<VertexFull.Stream0>(0);
        NativeArray<VertexFull.Stream1> stream1Data = data.GetVertexData<VertexFull.Stream1>(1);
        NativeArray<VertexFull.Stream2> stream2Data = data.GetVertexData<VertexFull.Stream2>(2);
        NativeArray<VertexFull.Stream3> stream3Data = data.GetVertexData<VertexFull.Stream3>(3);

        //create VertexFull values from streams
        vertexData = new NativeArray<VertexFull>(vertexCount, Allocator.Temp);
        for(int i=0; i<vertexCount; i++)
        {
            vertexData[i] = new VertexFull(stream0Data[i], stream1Data[i], stream2Data[i], stream3Data[i]);
        }

        meshArray.Dispose();
    }

    public static Mesh SetMeshVertices(List<VertexFull> vertices)
    {
        int vertexCount = vertices.Count;

        // Allocate writable mesh data for the mesh
        Mesh.MeshDataArray meshArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData data = meshArray[0];
        data.SetVertexBufferParams(vertexCount, VertexFull.VertexBufferDescriptors);

        // Get the vertex streams (assuming there are 4 streams as seen in GetVertices)
        NativeArray<VertexFull.Stream0> stream0Data = data.GetVertexData<VertexFull.Stream0>(0);
        NativeArray<VertexFull.Stream1> stream1Data = data.GetVertexData<VertexFull.Stream1>(1);
        NativeArray<VertexFull.Stream2> stream2Data = data.GetVertexData<VertexFull.Stream2>(2);
        NativeArray<VertexFull.Stream3> stream3Data = data.GetVertexData<VertexFull.Stream3>(3);

        // Set the data for each stream from the input vertices
        for (int i = 0; i < vertexCount; i++)
        {
            // Decompose the VertexFull object back into its individual streams
            stream0Data[i] = vertices[i].stream0;
            stream1Data[i] = vertices[i].stream1;
            stream2Data[i] = vertices[i].stream2;
            stream3Data[i] = vertices[i].stream3;
        }

        // Apply changes back to the mesh data
        Mesh newMesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshArray, newMesh, MeshUpdateFlags.Default);

        // Return the updated mesh (Note: this method doesn't modify the mesh itself directly)
        return newMesh;
    }
}
