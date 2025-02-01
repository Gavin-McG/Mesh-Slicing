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
                if (PointInTriangle(i.pos, a, b, c)) return false;
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
            Debug.DrawLine(pointInfo[i].pos, pointInfo[i].next.pos, Color.black);
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

        for (int i=0; i<triangles.Count; ++i)
        {
            Debug.DrawLine(polygon[triangles[i].Item1], polygon[triangles[i].Item2], Color.red);
            Debug.DrawLine(polygon[triangles[i].Item2], polygon[triangles[i].Item3], Color.blue);
            Debug.DrawLine(polygon[triangles[i].Item3], polygon[triangles[i].Item1], Color.green);
        }
        return triangles;
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
