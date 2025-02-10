using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder;
/*
[BurstCompile]
public struct PolygonNode
{
    public float2 pos; // 2D position
    public int index;  // Index from the original vertex array (needed for reconstruction)

    public PolygonNode(float2 pos, int index)
    {
        this.pos = pos;
        this.index = index;
    }
}

[BurstCompile]
public static class Polygon
{
    [BurstCompile]
    public static float3 GetOrthogonalVector(float3 v)
    {
        float3 orthogonal = math.abs(v.x) > math.abs(v.z)
            ? new float3(-v.y, v.x, 0)
            : new float3(0, -v.z, v.y);
        return math.normalize(orthogonal);
    }



    [BurstCompile]
    public static void MakePolygon(
    NativeList<int> loop,
    NativeArray<float3> vertices,
    float3 normal,
    ref NativeList<PolygonNode> polygon)
    {
        //get components of 3d plane
        float3 dir1 = GetOrthogonalVector(normal);
        float3 dir2 = math.cross(normal, dir1);

        polygon.Clear(); // Ensure it's empty before filling
        for (int i = 0; i < loop.Length; i++)
        {
            int point = loop[i];
            float3 pos = vertices[point];
            float comp1 = math.dot(dir1, pos);
            float comp2 = math.dot(dir2, pos);
            polygon.Add(new PolygonNode(new float2(comp2, comp1), point));
        }
    }

    [BurstCompile]
    static float Cross(float2 a, float2 b) => a.x * b.y - a.y * b.x;

    [BurstCompile]
    public static bool DoLinesIntersect(float2 p1, float2 p2, float2 q1, float2 q2)
    {
        float2 r = p2 - p1;
        float2 s = q2 - q1;

        float rxs = Cross(r, s);
        float2 q1p1 = q1 - p1;

        if (math.abs(rxs) < 1e-5f) // Almost zero, consider it parallel
        {
            if (math.abs(Cross(q1p1, r)) < 1e-5f) // Collinear case
            {
                float t0 = math.dot(q1p1, r) / math.dot(r, r);
                float t1 = t0 + math.dot(s, r) / math.dot(r, r);
                return (t0 >= 0 && t0 <= 1) || (t1 >= 0 && t1 <= 1);
            }
            return false;
        }

        float t = Cross(q1p1, s) / rxs;
        float u = Cross(q1p1, r) / rxs;

        return (t >= 0 && t <= 1) && (u >= 0 && u <= 1);
    }

    [BurstCompile]
    public static bool DoesLineIntersect(NativeList<PolygonNode> polygon, float2 p1, float2 p2)
    {
        int count = polygon.Length;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            if (DoLinesIntersect(p1, p2, polygon[i].pos, polygon[j].pos))
                return true;
        }
        return false;
    }

    [BurstCompile]
    public static bool IsPointInside(NativeList<PolygonNode> polygon, float2 point)
    {
        int crossingNumber = 0;
        int count = polygon.Length;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            float2 v1 = polygon[i].pos;
            float2 v2 = polygon[j].pos;

            if (((v1.y > point.y) != (v2.y > point.y)) &&
                (point.x < (v2.x - v1.x) * (point.y - v1.y) / (v2.y - v1.y) + v1.x))
            {
                crossingNumber++;
            }
        }
        return (crossingNumber & 1) == 1; // Odd means inside, even means outside
    }

    [BurstCompile]
    public static bool IsHoleInside(NativeList<PolygonNode> polygon, NativeList<PolygonNode> hole)
    {
        for (int i = 0; i < hole.Length; i++)
        {
            if (!IsPointInside(polygon, hole[i].pos))
                return false;
        }
        return true;
    }

    [BurstCompile]
    public static bool IsClockwise(NativeList<PolygonNode> polygon)
    {
        float sum = 0;
        int count = polygon.Length;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            float2 v1 = polygon[i].pos;
            float2 v2 = polygon[j].pos;

            sum += (v2.x - v1.x) * (v2.y + v1.y);
        }
        return sum > 0;
    }
}*/