using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable, StructLayout(LayoutKind.Sequential), VertexVariant]
public struct VertexBasic : IVertex<VertexBasic>
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;

    // Explicit Interface Implementations
    Vector3 IVertex<VertexBasic>.position { get => position; set => position = value; }
    Vector3 IVertex<VertexBasic>.normal { get => normal; set => normal = value; }
    Vector2 IVertex<VertexBasic>.uv { get => uv; set => uv = value; }

    //constructors
    public VertexBasic(Vector3 position, Vector3 normal, Vector2 uv)
    {
        this.position = position;
        this.normal = normal;
        this.uv = uv;
    }

    VertexBasic(Stream0 s0)
    {
        this.position = s0.position;
        this.normal = s0.normal;
        this.uv = s0.uv;
    }

    //IVertex
    public VertexBasic Lerp(VertexBasic other, float t, bool clamp = true)
    {
        t = clamp ? Mathf.Clamp01(t) : t;

        VertexBasic newVertex = new VertexBasic(
            Vector3.Lerp(position, other.position, t),
            Vector3.Lerp(normal, other.normal, t),
            Vector2.Lerp(uv, other.uv, t)
        );

        newVertex.normal.Normalize();

        return newVertex;
    }

    public bool Equals(VertexBasic other)
    {
        return position == other.position;
    }

    public int GetHash()
    {
        int x = Mathf.FloorToInt(position.x / IVertex<VertexBasic>.bucketSize);
        int y = Mathf.FloorToInt(position.y / IVertex<VertexBasic>.bucketSize);
        int z = Mathf.FloorToInt(position.z / IVertex<VertexBasic>.bucketSize);

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

    //streams
    struct Stream0
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }

    // Readonly properties to access associated streams
    Stream0 stream0 => new Stream0
    {
        position = this.position,
        normal = this.normal,
        uv = this.uv
    };


    // Static property to get accurate VertexAttributeDescriptor[]
    static VertexAttributeDescriptor[] VertexBufferDescriptors => new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2, stream:0),
        };

    public NativeArray<VertexBasic> GetVertices(Mesh mesh)
    {
        int vertexCount = mesh.vertexCount;

        //get readOnly access to mesh buffer data
        Mesh.MeshDataArray meshArray = Mesh.AcquireReadOnlyMeshData(mesh);
        Mesh.MeshData data = meshArray[0];
        NativeArray<Stream0> stream0Data = data.GetVertexData<Stream0>(0);

        //create VertexFull values from streams
        NativeArray<VertexBasic> vertexData = new NativeArray<VertexBasic>(vertexCount, Allocator.Temp);
        for (int i = 0; i < vertexCount; i++)
        {
            vertexData[i] = new VertexBasic(stream0Data[i]);
        }

        meshArray.Dispose();
        return vertexData;
    }

    public Mesh SetMeshVertices(List<VertexBasic> vertices)
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







[Serializable, StructLayout(LayoutKind.Sequential), VertexVariant]
public struct VertexStandardRig : IVertex<VertexStandardRig>, ITangent<VertexStandardRig>, IBoneWeights<VertexStandardRig>
{
    public Vector3 position;
    public Vector3 normal;
    public Vector4 tangent;
    public Vector2 uv;
    public BoneWeight boneWeights;

    // Explicit Interface Implementations
    Vector3 IVertex<VertexStandardRig>.position { get => position; set => position = value; }
    Vector3 IVertex<VertexStandardRig>.normal { get => normal; set => normal = value; }
    Vector2 IVertex<VertexStandardRig>.uv { get => uv; set => uv = value; }
    Vector4 ITangent<VertexStandardRig>.tangent { get => tangent; set => tangent = value; }
    BoneWeight IBoneWeights<VertexStandardRig>.boneWeights { get => boneWeights; set => boneWeights = value; }

    //constructors
    public VertexStandardRig(Vector3 position, Vector3 normal, Vector4 tangent, Vector2 uv, BoneWeight boneWeights)
    {
        this.position = position;
        this.normal = normal;
        this.tangent = tangent;
        this.uv = uv;
        this.boneWeights = boneWeights;
    }

    VertexStandardRig(Stream0 s0, Stream1 s1, Stream2 s2)
    {
        this.position = s0.position;
        this.normal = s0.normal;
        this.tangent = s0.tangent;
        this.uv = s1.uv;
        this.boneWeights = s2.weight;
    }

    //IVertex
    public VertexStandardRig Lerp(VertexStandardRig other, float t, bool clamp = true)
    {
        t = clamp ? Mathf.Clamp01(t) : t;

        VertexStandardRig newVertex = new VertexStandardRig(
            Vector3.Lerp(position, other.position, t),
            Vector3.Lerp(normal, other.normal, t),
            Vector4.Lerp(tangent, other.tangent, t),
            Vector2.Lerp(uv, other.uv, t),
            VertexUtility.LerpBoneWeight(boneWeights, other.boneWeights, t)
        );

        newVertex.normal.Normalize();
        newVertex.tangent.Normalize();

        return newVertex;
    }

    public bool Equals(VertexStandardRig other)
    {
        return position == other.position;
    }

    public int GetHash()
    {
        int x = Mathf.FloorToInt(position.x / IVertex<VertexStandardRig>.bucketSize);
        int y = Mathf.FloorToInt(position.y / IVertex<VertexStandardRig>.bucketSize);
        int z = Mathf.FloorToInt(position.z / IVertex<VertexStandardRig>.bucketSize);

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

    //streams
    struct Stream0
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
    }

    struct Stream1
    {
        public Vector2 uv;
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
        uv = this.uv
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

            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2, stream:1),

            new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, dimension:4, stream:2),
            new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, dimension:4, stream:2),
        };

    public NativeArray<VertexStandardRig> GetVertices(Mesh mesh)
    {
        int vertexCount = mesh.vertexCount;

        //get readOnly access to mesh buffer data
        Mesh.MeshDataArray meshArray = Mesh.AcquireReadOnlyMeshData(mesh);
        Mesh.MeshData data = meshArray[0];
        NativeArray<Stream0> stream0Data = data.GetVertexData<Stream0>(0);
        NativeArray<Stream1> stream1Data = data.GetVertexData<Stream1>(1);
        NativeArray<Stream2> stream2Data = data.GetVertexData<Stream2>(2);

        //create VertexFull values from streams
        NativeArray<VertexStandardRig> vertexData = new NativeArray<VertexStandardRig>(vertexCount, Allocator.Temp);
        for (int i = 0; i < vertexCount; i++)
        {
            vertexData[i] = new VertexStandardRig(stream0Data[i], stream1Data[i], stream2Data[i]);
        }

        meshArray.Dispose();
        return vertexData;
    }

    public Mesh SetMeshVertices(List<VertexStandardRig> vertices)
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
