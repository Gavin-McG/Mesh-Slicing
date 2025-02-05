using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;


public struct PolygonNode
{
    public Vector2 pos; //2d position
    public int index; //index from original vertice array (needed to reconstruction of triangulation

    public PolygonNode(Vector2 pos, int index)
    {
        this.pos = pos;
        this.index = index;
    }
}



public class Polygon
{
    public List<PolygonNode> nodes;

    public Polygon(List<Vector2> points, List<int> indexes)
    {
        if (points.Count != indexes.Count) return;

        nodes = new();
        for (int i=0; i<points.Count; i++)
        {
            nodes.Add(new PolygonNode(points[i], indexes[i]));
        }
    }

    //return a list of vector2 points
    public List<Vector2> ToPoints()
    {
        List<Vector2> points = new();
        foreach (PolygonNode node in nodes)
        {
            points.Add(node.pos);
        }
        return points;
    }





    public static Vector3 GetOrthogonalVector(Vector3 v)
    {
        Vector3 orthogonal = Mathf.Abs(v.x) > Mathf.Abs(v.z) ?
                new Vector3(-v.y, v.x, 0) :
                new Vector3(0, -v.z, v.y);
        orthogonal.Normalize();
        return orthogonal;
    }



    //convert a 3d path into a 2d polygon
    public static List<Polygon> MakePolygons(List<List<int>> loops, List<Vector3> vertices, Vector3 normal)
    {
        //Convert slice to 2D polygons and determine direction
        Vector3 dir1 = GetOrthogonalVector(normal);
        Vector3 dir2 = Vector3.Cross(normal, dir1);
        List<Polygon> polygons = new();
        foreach (List<int> loop in loops)
        {
            polygons.Add(MakePolygon(loop, vertices, dir1, dir2));
        }
        return polygons;
    }


    public static Polygon MakePolygon(List<int> loop,  List<Vector3> vertices, Vector3 dir1, Vector3 dir2)
    {
        //turn loops into 2d list
        List<Vector2> polygon = new List<Vector2>();
        foreach (int point in loop)
        {
            Vector3 pos = vertices[point];
            float comp1 = Vector3.Dot(dir1, pos);
            float comp2 = Vector3.Dot(dir2, pos);
            polygon.Add(new Vector2(comp2, comp1));
        }
        return new Polygon(polygon, loop);
    }



    //determine whether two lines intersect
    public static bool DoLinesIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        Vector2 r = p2 - p1;
        Vector2 s = q2 - q1;

        float rxs = Cross(r, s);
        Vector2 q1p1 = q1 - p1;

        if (Mathf.Approximately(rxs, 0f))
        {
            // Collinear case: check for overlap
            if (Mathf.Approximately(Cross(q1p1, r), 0f))
            {
                float t0 = Vector2.Dot(q1p1, r) / Vector2.Dot(r, r);
                float t1 = t0 + Vector2.Dot(s, r) / Vector2.Dot(r, r);
                return (t0 >= 0 && t0 <= 1) || (t1 >= 0 && t1 <= 1);
            }
            return false; // Parallel but not collinear
        }

        float t = Cross(q1p1, s) / rxs;
        float u = Cross(q1p1, r) / rxs;

        return t >= 0 && t <= 1 && u >= 0 && u <= 1;
    }



    //check if a line intersects with a polygon
    public bool DoesLineIntersect(Vector2 p1, Vector2 p2)
    {
        for (int i=0, j=nodes.Count-1; i<nodes.Count; j=i++)
        {
            Vector2 q1 = nodes[i].pos;
            Vector2 q2 = nodes[j].pos;
            if (DoLinesIntersect(p1, p2, q1, q2)) return true;
        }
        return false;
    }



    //determine whether a single point is encased within a polygon
    public bool IsPointInside(Vector2 point)
    {
        int crossingNumber = 0;
        int count = nodes.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 v1 = nodes[i].pos;
            Vector2 v2 = nodes[j].pos;

            if (((v1.y > point.y) != (v2.y > point.y)) &&
                (point.x < (v2.x - v1.x) * (point.y - v1.y) / (v2.y - v1.y) + v1.x))
            {
                crossingNumber++;
            }
        }
        return (crossingNumber % 2) == 1; // Odd means inside, even means outside
    }



    //determine whether a hole is completely encased within a polygon
    public bool IsHoleInside(Polygon hole)
    {
        foreach (PolygonNode node in hole.nodes)
        {
            if (!IsPointInside(node.pos))
            {
                return false;
            }
        }
        return true;
    }



    //Determine whether a Polygon is clockwise or counter-clockwise
    public bool IsClockwise()
    {
        float sum = 0;
        int count = nodes.Count;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 v1 = nodes[i].pos;
            Vector2 v2 = nodes[j].pos;

            sum += (v2.x - v1.x) * (v2.y + v1.y);
        }

        return sum > 0; // True if clockwise, False if counter-clockwise
    }
}
