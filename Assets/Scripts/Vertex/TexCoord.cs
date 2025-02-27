using System.Runtime.InteropServices;
using System;
using UnityEngine;

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct TexCoord1 : ITexCoord<TexCoord1>
{
    public Vector2 uv0;

    public Vector2 UV0 { get => uv0; set => uv0 = value; }

    TexCoord1(Vector2 uv0)
    {
        this.uv0 = uv0;
    }

    public TexCoord1 Lerp(TexCoord1 other, float t, bool clamp = true)
    {
        if (clamp) t = Mathf.Clamp01(t);
        return new TexCoord1(
            Vector2.Lerp(uv0, other.uv0, t)
        );
    }
}

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct TexCoord2 : ITexCoord<TexCoord2>
{
    public Vector2 uv0, uv1;

    public Vector2 UV0 { get => uv0; set => uv0 = value; }

    public TexCoord2(Vector2 uv0, Vector2 uv1)
    {
        this.uv0 = uv0;
        this.uv1 = uv1;
    }

    public TexCoord2 Lerp(TexCoord2 other, float t, bool clamp = true)
    {
        if (clamp) t = Mathf.Clamp01(t);
        return new TexCoord2(
            Vector2.Lerp(uv0, other.uv0, t),
            Vector2.Lerp(uv1, other.uv1, t)
        );
    }
}

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct TexCoord3 : ITexCoord<TexCoord3>
{
    public Vector2 uv0, uv1, uv2;

    public Vector2 UV0 { get => uv0; set => uv0 = value; }

    public TexCoord3(Vector2 uv0, Vector2 uv1, Vector2 uv2)
    {
        this.uv0 = uv0;
        this.uv1 = uv1;
        this.uv2 = uv2;
    }

    public TexCoord3 Lerp(TexCoord3 other, float t, bool clamp = true)
    {
        if (clamp) t = Mathf.Clamp01(t);
        return new TexCoord3(
            Vector2.Lerp(uv0, other.uv0, t),
            Vector2.Lerp(uv1, other.uv1, t),
            Vector2.Lerp(uv2, other.uv2, t)
        );
    }
}

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct TexCoord4 : ITexCoord<TexCoord4>
{
    public Vector2 uv0, uv1, uv2, uv3;

    public Vector2 UV0 { get => uv0; set => uv0 = value; }

    public TexCoord4(Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        this.uv0 = uv0;
        this.uv1 = uv1;
        this.uv2 = uv2;
        this.uv3 = uv3;
    }

    public TexCoord4 Lerp(TexCoord4 other, float t, bool clamp = true)
    {
        if (clamp) t = Mathf.Clamp01(t);
        return new TexCoord4(
            Vector2.Lerp(uv0, other.uv0, t),
            Vector2.Lerp(uv1, other.uv1, t),
            Vector2.Lerp(uv2, other.uv2, t),
            Vector2.Lerp(uv3, other.uv3, t)
        );
    }
}

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct TexCoord5 : ITexCoord<TexCoord5>
{
    public Vector2 uv0, uv1, uv2, uv3, uv4;

    public Vector2 UV0 { get => uv0; set => uv0 = value; }

    public TexCoord5(Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        this.uv0 = uv0;
        this.uv1 = uv1;
        this.uv2 = uv2;
        this.uv3 = uv3;
        this.uv4 = uv4;
    }

    public TexCoord5 Lerp(TexCoord5 other, float t, bool clamp = true)
    {
        if (clamp) t = Mathf.Clamp01(t);
        return new TexCoord5(
            Vector2.Lerp(uv0, other.uv0, t),
            Vector2.Lerp(uv1, other.uv1, t),
            Vector2.Lerp(uv2, other.uv2, t),
            Vector2.Lerp(uv3, other.uv3, t),
            Vector2.Lerp(uv4, other.uv4, t)
        );
    }
}

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct TexCoord6 : ITexCoord<TexCoord6>
{
    public Vector2 uv0, uv1, uv2, uv3, uv4, uv5;

    public Vector2 UV0 { get => uv0; set => uv0 = value; }

    public TexCoord6(Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, Vector2 uv5)
    {
        this.uv0 = uv0;
        this.uv1 = uv1;
        this.uv2 = uv2;
        this.uv3 = uv3;
        this.uv4 = uv4;
        this.uv5 = uv5;
    }

    public TexCoord6 Lerp(TexCoord6 other, float t, bool clamp = true)
    {
        if (clamp) t = Mathf.Clamp01(t);
        return new TexCoord6(
            Vector2.Lerp(uv0, other.uv0, t),
            Vector2.Lerp(uv1, other.uv1, t),
            Vector2.Lerp(uv2, other.uv2, t),
            Vector2.Lerp(uv3, other.uv3, t),
            Vector2.Lerp(uv4, other.uv4, t),
            Vector2.Lerp(uv5, other.uv5, t)
        );
    }
}

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct TexCoord7 : ITexCoord<TexCoord7>
{
    public Vector2 uv0, uv1, uv2, uv3, uv4, uv5, uv6;

    public Vector2 UV0 { get => uv0; set => uv0 = value; }

    public TexCoord7(Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, Vector2 uv5, Vector2 uv6)
    {
        this.uv0 = uv0;
        this.uv1 = uv1;
        this.uv2 = uv2;
        this.uv3 = uv3;
        this.uv4 = uv4;
        this.uv5 = uv5;
        this.uv6 = uv6;
    }

    public TexCoord7 Lerp(TexCoord7 other, float t, bool clamp = true)
    {
        if (clamp) t = Mathf.Clamp01(t);
        return new TexCoord7(
            Vector2.Lerp(uv0, other.uv0, t),
            Vector2.Lerp(uv1, other.uv1, t),
            Vector2.Lerp(uv2, other.uv2, t),
            Vector2.Lerp(uv3, other.uv3, t),
            Vector2.Lerp(uv4, other.uv4, t),
            Vector2.Lerp(uv5, other.uv5, t),
            Vector2.Lerp(uv6, other.uv6, t)
        );
    }
}

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct TexCoord8 : ITexCoord<TexCoord8>
{
    public Vector2 uv0, uv1, uv2, uv3, uv4, uv5, uv6, uv7;

    public Vector2 UV0 { get => uv0; set => uv0 = value; }

    public TexCoord8(Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, Vector2 uv5, Vector2 uv6, Vector2 uv7)
    {
        this.uv0 = uv0;
        this.uv1 = uv1;
        this.uv2 = uv2;
        this.uv3 = uv3;
        this.uv4 = uv4;
        this.uv5 = uv5;
        this.uv6 = uv6;
        this.uv7 = uv7;
    }

    public TexCoord8 Lerp(TexCoord8 other, float t, bool clamp = true)
    {
        if (clamp) t = Mathf.Clamp01(t);
        return new TexCoord8(
            Vector2.Lerp(uv0, other.uv0, t),
            Vector2.Lerp(uv1, other.uv1, t),
            Vector2.Lerp(uv2, other.uv2, t),
            Vector2.Lerp(uv3, other.uv3, t),
            Vector2.Lerp(uv4, other.uv4, t),
            Vector2.Lerp(uv5, other.uv5, t),
            Vector2.Lerp(uv6, other.uv6, t),
            Vector2.Lerp(uv7, other.uv7, t)
        );
    }
}