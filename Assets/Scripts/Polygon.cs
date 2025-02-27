using System;
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

    public Polygon(List<PolygonNode> nodes)
    {
        this.nodes = nodes;
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
    public static List<Polygon> MakePolygons<T, U>(List<List<int>> loops, List<T> vertices, Vector3 normal, bool clamp=false) 
        where T : struct, IVertex<T, U>
        where U : struct, ITexCoord<U>
    {
        //Convert slice to 2D polygons and determine direction
        Vector3 dir1 = GetOrthogonalVector(normal);
        Vector3 dir2 = Vector3.Cross(normal, dir1);
        List<Polygon> polygons = new();
        foreach (List<int> loop in loops)
        {
            polygons.Add(MakePolygon<T, U>(loop, vertices, dir1, dir2));
        }

        //clamp values between 0 and 1
        if (clamp && polygons.Count>0)
        {
            //find bounding box
            float minX = polygons[0].nodes[0].pos.x, maxX = polygons[0].nodes[0].pos.x;
            float minY = polygons[0].nodes[0].pos.y, maxY = polygons[0].nodes[0].pos.y;
            foreach (Polygon polygon in polygons)
            {
                foreach(PolygonNode node in polygon.nodes)
                {
                    minX = Mathf.Min(minX, node.pos.x);
                    maxX = Mathf.Max(maxX, node.pos.x);
                    minY = Mathf.Min(minY, node.pos.y);
                    maxY = Mathf.Max(maxY, node.pos.y);
                }
            }

            //modify loops
            for (int p = 0; p < polygons.Count; p++)
            {
                for (int i = 0; i < polygons[p].nodes.Count; i++)
                {
                    PolygonNode node = polygons[p].nodes[i];
                    node.pos.x -= minX;
                    node.pos.y -= minY;
                    node.pos.x /= (maxX - minX);
                    node.pos.y /= (maxY - minY);
                    polygons[p].nodes[i] = node;
                }
            }
        }

        return polygons;
    }


    public static Polygon MakePolygon<T, U>(List<int> loop,  List<T> vertices, Vector3 dir1, Vector3 dir2)
        where T : struct, IVertex<T, U>
        where U : struct, ITexCoord<U>
    {
        //turn loops into 2d list
        List<Vector2> polygon = new List<Vector2>();
        foreach (int point in loop)
        {
            Vector3 pos = vertices[point].Position;
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




    //attepts to combine a hole into a polygon by checking bridge compatibility
    public bool CombineHole(Polygon hole, List<Polygon> allHoles)
    {
        //create list of potential bridges
        List<(float sqDist, int polyIndex, int holeIndex)> bridges = new();
        for (int i=0; i<nodes.Count; ++i)
        {
            for (int j=0; j<hole.nodes.Count; ++j)
            {
                bridges.Add((Vector2.SqrMagnitude(nodes[i].pos - hole.nodes[j].pos), i, j));
            }
        }

        //sort potential bridges
        bridges.Sort((x, y) => x.sqDist.CompareTo(y.sqDist));

        //look through bridges
        foreach (var bridge in bridges)
        {
            Vector2 p1 = nodes[bridge.polyIndex].pos;
            Vector2 p2 = hole.nodes[bridge.holeIndex].pos;

            bool validBridge = true;

            //check against this polygon
            for (int i=0,j=nodes.Count-1; i<nodes.Count; j=i++)
            {
                if (bridge.polyIndex == i || bridge.polyIndex == j) continue;

                Vector2 q1 = nodes[i].pos;
                Vector2 q2 = nodes[j].pos;

                if (DoLinesIntersect(p1,p2,q1,q2))
                {
                    validBridge = false; 
                    break;
                }
            }
            if (!validBridge) continue;

            //check against this hole
            for (int i = 0, j = hole.nodes.Count-1; i < hole.nodes.Count; j = i++)
            {
                if (bridge.holeIndex == i || bridge.holeIndex == j) continue;

                Vector2 q1 = hole.nodes[i].pos;
                Vector2 q2 = hole.nodes[j].pos;

                if (DoLinesIntersect(p1, p2, q1, q2))
                {
                    validBridge = false;
                    break;
                }
            }
            if (!validBridge) continue;

            //check against other holes
            foreach (Polygon otherHole in allHoles)
            {
                if (otherHole == hole) continue;

                for (int i = 0, j = otherHole.nodes.Count - 1; i < otherHole.nodes.Count; j = i++)
                {
                    Vector2 q1 = otherHole.nodes[i].pos;
                    Vector2 q2 = otherHole.nodes[j].pos;

                    if (DoLinesIntersect(p1, p2, q1, q2))
                    {
                        validBridge = false;
                        break;
                    }
                }
                if (!validBridge) break;
            }

            //check for bridge validity
            if (validBridge)
            {
                MergeHole(hole, bridge.polyIndex, bridge.holeIndex);
                return true;
            }
        }

        return false;
    }




    void MergeHole(Polygon Hole, int polyIndex, int holeIndex)
    {
        List<PolygonNode> newPolygon = new();

        // Add elements from list1 up to index1 (inclusive)
        newPolygon.AddRange(nodes.GetRange(0, polyIndex + 1));

        // Add elements from list2 starting from index2 and wrapping around
        for (int i = holeIndex; i < Hole.nodes.Count; i++)
        {
            newPolygon.Add(Hole.nodes[i]);
        }
        for (int i = 0; i <= holeIndex; i++)
        {
            newPolygon.Add(Hole.nodes[i]);
        }

        // Add elements from list1 starting from index1 (inclusive) to the end
        newPolygon.AddRange(nodes.GetRange(polyIndex, nodes.Count - polyIndex));

        nodes = newPolygon;
    }
}
