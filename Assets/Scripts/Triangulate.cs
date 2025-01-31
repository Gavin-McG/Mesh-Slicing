using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Triangulate
{
    public static List<(int, int, int)> TriangulatePolygon(List<Vector2> polygon)
    {
        List<(int, int, int)> triangles = new();
        if (polygon.Count < 3) return triangles;

        List<int> indices = new();
        for (int i = 0; i < polygon.Count; i++)
            indices.Add(i);

        while (indices.Count > 2)
        {
            bool earFound = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                if (IsEar(polygon, prev, curr, next, indices))
                {
                    triangles.Add((prev, curr, next));
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            if (!earFound) break; // Prevent infinite loop
        }
        return triangles;
    }

    private static bool IsEar(List<Vector2> vertices, int prev, int curr, int next, List<int> indices)
    {
        Vector2 a = vertices[prev], b = vertices[curr], c = vertices[next];
        if (GetTriangleArea(a, b, c) >= 0) return false; // Check if CCW

        foreach (int i in indices)
        {
            if (i == prev || i == curr || i == next) continue;
            if (PointInTriangle(vertices[i], a, b, c)) return false;
        }
        return true;
    }

    private static float GetTriangleArea(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float areaOrig = Mathf.Abs(GetTriangleArea(a, b, c));
        float area1 = Mathf.Abs(GetTriangleArea(p, b, c));
        float area2 = Mathf.Abs(GetTriangleArea(a, p, c));
        float area3 = Mathf.Abs(GetTriangleArea(a, b, p));
        return Mathf.Abs(areaOrig - (area1 + area2 + area3)) < 1e-5;
    }
}
