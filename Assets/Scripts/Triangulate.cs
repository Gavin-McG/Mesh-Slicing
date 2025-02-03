using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;

public static class Triangulate
{
    private class AngleComparer : IComparer<PointInfo>
    {
        public int Compare(PointInfo a, PointInfo b)
        {
            return a.angle.CompareTo(b.angle);
        }
    }

    private class PointInfo
    {
        public int index;
        public float angle;
        public Vector2 pos;

        public PointInfo next;
        public PointInfo prev;

        public PointInfo(int index, float angle, Vector2 pos)
        {
            this.index = index;
            this.angle = angle;
            this.pos = pos;
        }

        public void CalculateAngle()
        {
            Vector2 v1 = next.pos - pos;
            Vector2 v2 = prev.pos - pos;
            angle = Mathf.Atan2(v1.y * v2.x - v1.x * v2.y, v1.x * v2.x + v1.y * v2.y);
        }

        public bool IsEar(List<PointInfo> pointInfo)
        {
            if (angle < 0) return false; // Check if CCW

            Vector2 a = prev.pos, b = pos, c = next.pos;
            foreach (PointInfo i in pointInfo)
            {
                if (i.index == prev.index || i.index == index || i.index == next.index) continue;
                if (IsPointInsideTriangle(i.pos, a, b, c)) return false;
            }
            return true;
        }
    }

    public static List<(int, int, int)> TriangulatePolygon(List<Vector2> polygon)
    {
        List<(int, int, int)> triangles = new();
        if (polygon.Count < 3) return triangles;

        //create point data
        List<PointInfo> pointInfo = new();
        for (int i = 0; i < polygon.Count; i++) {
            pointInfo.Add(new PointInfo(i, 0, polygon[i]));
        }
        for (int i = 0; i < pointInfo.Count; ++i)
        {
            //create relation
            int prevIndex = (i - 1 + pointInfo.Count) % pointInfo.Count;
            int nextIndex = (i + 1) % pointInfo.Count;
            pointInfo[i].prev = pointInfo[prevIndex];
            pointInfo[i].next = pointInfo[nextIndex];
            pointInfo[i].CalculateAngle();
        }

        while (pointInfo.Count > 2)
        {
            bool earFound = false;

            pointInfo.Sort(new AngleComparer());
            for (int i = 0; i < pointInfo.Count; i++)
            {
                if (pointInfo[i].IsEar(pointInfo))
                {
                    triangles.Add((pointInfo[i].prev.index, pointInfo[i].index, pointInfo[i].next.index));
                    pointInfo[i].next.prev = pointInfo[i].prev;
                    pointInfo[i].prev.next = pointInfo[i].next;
                    pointInfo[i].prev.CalculateAngle();
                    pointInfo[i].next.CalculateAngle();
                    pointInfo.RemoveAt(i);
                    earFound = true;

                    break;
                }
            }
            if (!earFound) break; // Prevent infinite loop
        }

        return triangles;
    }

    private static bool IsPointInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // Compute vectors
        Vector2 v0 = c - a;
        Vector2 v1 = b - a;
        Vector2 v2 = p - a;

        // Compute dot products
        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        // Compute barycentric coordinates
        float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // Check if point is inside the triangle
        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }
}
