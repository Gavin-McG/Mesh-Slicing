using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

public interface IVertex<T> : IEquatable<T>, IFormattable where T : struct, IVertex<T>
{
    public static float bucketSize = 0;

    Vector3 position { get; set; }
    Vector3 normal { get; set; }
    Vector2 uv { get; set; }

    T Lerp(T other, float t, bool clamp = true);

    T Initialize(Vector3 position, Vector3 normal, Vector2 uv)
    {
        T newVertex = new T();
        newVertex.position = position;
        newVertex.normal = normal;
        newVertex.uv = uv;
        return newVertex;
    }

    public bool Equals(object obj) => obj is T other && Equals(other);

    public NativeArray<T> GetVertices(Mesh mesh);
    public Mesh SetMeshVertices(List<T> vertices);
}

public interface ITangent<T> where T : struct, IVertex<T>
{
    Vector4 tangent { get; set; }
}

public interface IColor<T> where T : struct, IVertex<T>
{
    Vector4 color { get; set; }
}

public interface IBoneWeights<T> where T : struct, IVertex<T>
{
    BoneWeight boneWeights { get; set; }
}