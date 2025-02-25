using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public struct Vertex : IEquatable<Vertex>, IComparable<Vertex>, IFormattable, ISerializable
{
    static float bucketSize = 0.1f;

    public Vector3 position;
    public Vector3 normal;
    public Vector4 tangent;
    public Color color;
    public Vector2 uv0;

    //constructors
    public Vertex(Vector3 position)
        : this(position, Vector3.zero, Vector4.zero, Vector4.zero, Vector2.zero) { }

    public Vertex(Vector3 position, Vector3 normal) 
        : this(position, normal, Vector4.zero, Vector4.zero, Vector2.zero) { }

    public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        : this(position, normal, Vector4.zero, Vector4.zero, uv) { }

    public Vertex(Vector3 position, Vector3 normal, Vector4 tangent) 
        : this(position, normal, tangent, Vector4.zero, Vector2.zero) { }

    public Vertex(Vector3 position, Vector3 normal, Vector4 tangent, Color color) 
        : this(position, normal, tangent, color, Vector2.zero) { }

    public Vertex(Vector3 position, Vector3 normal, Vector4 tangent, Color color, Vector2 uv)
    {
        this.position = position;
        this.normal = normal;
        this.tangent = tangent;
        this.color = color;
        this.uv0 = uv;
    }


    public static Vertex Lerp(Vertex p1, Vertex p2, float t, bool clamp = true)
    {
        t = clamp ? Mathf.Clamp01(t) : t;

        Vertex newVertex = new Vertex(
            Vector3.Lerp(p1.position, p2.position, t),
            Vector3.Lerp(p1.normal, p2.normal, t),
            Vector4.Lerp(p1.tangent, p2.tangent, t),
            Color.Lerp(p1.color, p2.color, t),
            Vector2.Lerp(p1.uv0, p2.uv0, t)
        );

        newVertex.normal.Normalize();
        newVertex.tangent.Normalize();

        return newVertex;
    }

    public override bool Equals(object obj) => obj is Vertex other && Equals(other);

    public bool Equals(Vertex other)
    {
        return position == other.position;
    }

    public int CompareTo(Vertex other) {
        if (Equals(other)) return 0;
        return other.position.y > position.y || (other.position.y == position.y && other.position.x > position.x)
            ? 1 : -1;
    }

    public override int GetHashCode()
    {
        int x = Mathf.FloorToInt(position.x / bucketSize);
        int y = Mathf.FloorToInt(position.y / bucketSize);
        int z = Mathf.FloorToInt(position.z / bucketSize);

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

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("position", position);
        info.AddValue("normal", normal);
        info.AddValue("tangent", tangent);
        info.AddValue("color", color);
        info.AddValue("uv0", uv0);
    }
}
