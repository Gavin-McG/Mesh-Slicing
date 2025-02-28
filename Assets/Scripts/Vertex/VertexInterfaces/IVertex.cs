using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MeshSlicing.Vertex
{
    public interface IVertex<T, U> : IEquatable<T>
        where T : struct, IVertex<T, U>
        where U : struct, ITexCoord<U>
    {
        public static float bucketSize = 0;

        Vector3 Position { get; set; }
        Vector3 Normal { get; set; }
        U UVs { get; set; }

        T Lerp(T other, float t, bool clamp = true);

        public bool Equals(object obj) => obj is T other && Equals(other);

        public NativeArray<T> GetVertices(Mesh mesh);
        public Mesh SetMeshVertices(List<T> vertices);
    }
}