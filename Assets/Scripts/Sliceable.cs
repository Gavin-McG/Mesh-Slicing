using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

[System.Serializable] enum EdgeFillMode { None, Simple, Triangulate }
[System.Serializable] enum MomentumMode { Reset, Simple, Advanced }

public class Sliceable : MonoBehaviour
{
    public static HashSet<Sliceable> Sliceables = new HashSet<Sliceable>();

    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshFilter emptyPrefab;
    [SerializeField] EdgeFillMode edgeFillMode = EdgeFillMode.Simple;
    [SerializeField] MomentumMode momentumMode = MomentumMode.Simple;
    [SerializeField] bool SaveNoLoop = false;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void OnEnable()
    {
        Sliceables.Add(this);
    }

    private void OnDisable()
    {
        Sliceables.Remove(this);
    }

    public bool IntersectsBounds(Vector3 planePoint, Vector3 planeNormal)
    {
        Bounds bounds = meshFilter.mesh.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        // Define bounding box corners (fixed-size array to avoid GC)
        Vector3[] corners = new Vector3[8]
        {
        center + new Vector3(-extents.x, -extents.y, -extents.z),
        center + new Vector3(extents.x, -extents.y, -extents.z),
        center + new Vector3(-extents.x, -extents.y, extents.z),
        center + new Vector3(extents.x, -extents.y, extents.z),
        center + new Vector3(-extents.x, extents.y, -extents.z),
        center + new Vector3(extents.x, extents.y, -extents.z),
        center + new Vector3(-extents.x, extents.y, extents.z),
        center + new Vector3(extents.x, extents.y, extents.z)
        };

        // Precompute constant for plane equation
        float d = -Vector3.Dot(planeNormal, planePoint);
        bool hasPositive = false, hasNegative = false;

        // Check each corner against the plane
        for (int i = 0; i < 8; i++)
        {
            float side = Vector3.Dot(corners[i], planeNormal) + d;
            if (side > 0)
                hasPositive = true;
            else
                hasNegative = true;

            // If both positive and negative points exist, we have an intersection
            if (hasPositive && hasNegative)
                return true;
        }

        // All corners are on one side, so no intersection
        return false;
    }

    public Mesh GetMesh()
    {
        return meshFilter.mesh;
    }

    float epsilon = 0.0001f;
    public bool VectorsClose(Vector3 v1, Vector3 v2)
    {
        return Mathf.Abs(v1.x - v2.x) < epsilon &&
               Mathf.Abs(v1.y - v2.y) < epsilon &&
               Mathf.Abs(v1.z - v2.z) < epsilon;
    }

    List<List<int>> ConnectionsToLoops(Dictionary<int, int> direct)
    {
        List<List<int>> loops = new List<List<int>>();
        while (direct.Keys.Count > 0)
        {
            List<int> loop = new List<int>();
            int first = direct.Keys.First();
            int point = first;

            //traverse until end of loop found
            while (direct.ContainsKey(point))
            {
                loop.Add(point);

                int oldPoint = point;
                point = direct[point];
                direct.Remove(oldPoint);
            }

            //if loop closed
            if (point == first)
            {
                loops.Add(loop);
            }
        }
        return loops;
    }

    Mesh GetMeshSlice(Mesh original, Vector3 planePoint, Vector3 planeNormal)
    {
        //values for first new mesh
        Dictionary<int, int> vectorDict = new Dictionary<int, int>();
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        Dictionary<(int,int), int> cutPoints = new Dictionary<(int, int), int>();
        List<(int,int)> cutEdges = new List<(int, int)>();

        //get mesh data
        Vector3[] vertices = original.vertices;
        Vector3[] normals = original.normals;
        Vector2[] UVs = original.uv;
        int[] triangles = original.triangles;
        

        /*if (edgeFillMode == EdgeFillMode.Simple)
        {
            //find center point of new points on edge
            if (cutEdges.Count > 0)
            {
                Vector3 centerPoint = Vector3.zero;
                for (int i = 0; i < cutEdges.Count; ++i)
                {
                    centerPoint += newVertices[cutEdges[i].Item1] + newVertices[cutEdges[i].Item2];
                }
                centerPoint /= cutEdges.Count * 2;

                int centerPointIndex = newVertices.Count;
                newVertices.Add(centerPoint);
                newNormals.Add(-planeNormal);
                newUVs.Add(Vector2.zero);

                //add new geometry into plane
                for (int i = 0; i < cutEdges.Count; ++i)
                {
                    //new traingle
                    newTriangles.AddRange(new int[] { newVertices.Count + 1, newVertices.Count, centerPointIndex });

                    //new points
                    newVertices.Add(newVertices[cutEdges[i].Item1]);
                    newNormals.Add(-planeNormal);
                    newUVs.Add(Vector2.zero);

                    newVertices.Add(newVertices[cutEdges[i].Item2]);
                    newNormals.Add(-planeNormal);
                    newUVs.Add(Vector2.zero);
                }
            }
        }
        else if (edgeFillMode == EdgeFillMode.Triangulate)
        {
            //match vectors which are close enough
            List<int> pointIndexes = cutPoints.Values.ToList();
            Dictionary<int,int> closeIndexes = new();
            for (int i=0; i<pointIndexes.Count; ++i)
            {
                bool foundClose = false;
                for (int j=0; j<i; ++j)
                {
                    if (VectorsClose(newVertices[pointIndexes[i]], newVertices[pointIndexes[j]]))
                    {
                        closeIndexes[pointIndexes[i]] = pointIndexes[j];
                        foundClose = true;
                        break;
                    }
                }
                if (!foundClose)
                {
                    closeIndexes[pointIndexes[i]] = pointIndexes[i];
                }
            }

            //create relationship graph
            Dictionary<int, int> direct = new Dictionary<int, int>();
            for (int i = 0; i < cutEdges.Count; ++i)
            {
                int v1 = closeIndexes[cutEdges[i].Item1];
                int v2 = closeIndexes[cutEdges[i].Item2];
                if (v1 != v2)
                {
                    direct[v2] = v1;
                }
            }


            //attempt to get loops
            List<List<int>> loops = ConnectionsToLoops(direct);

            //get 2d polygons from loops
            List<Polygon> polygons = Polygon.MakePolygons(loops, newVertices, planeNormal);

            //divide polygons by direction 
            List<Polygon> polygonsCW = new();
            List<Polygon> polygonsCCW = new();
            foreach (Polygon polygon in polygons)
            {
                if (polygon.IsClockwise())
                {
                    polygonsCW.Add(polygon);
                }
                else
                {
                    polygonsCCW.Add(polygon);
                }
            }

            foreach (Polygon polygon in polygonsCW)
            {
                //get all the holes within the current polygon
                IList<IList<Vector2>> holes = new List<IList<Vector2>>();
                List<PolygonNode> nodes = new();
                nodes.AddRange(polygon.nodes);
                for (int i=0; i< polygonsCCW.Count; ++i)
                {
                    if (polygon.IsHoleInside(polygonsCCW[i]))
                    {
                        Debug.Log("Hole");
                        polygonsCCW[i].nodes.Reverse();
                        holes.Add(polygonsCCW[i].ToPoints());
                        nodes.AddRange(polygonsCCW[i].nodes);
                        polygonsCCW.RemoveAt(i);
                        --i;
                    }
                }

                //add triangles
                List<Vector2> points = polygon.ToPoints();
                List<int> indices = new();
                Triangulation.Triangulate(points, holes, out indices);

                for (int i = 0; i < indices.Count; i += 3)
                {
                    newTriangles.Add(newVertices.Count + indices[i]);
                    newTriangles.Add(newVertices.Count + indices[i+1]);
                    newTriangles.Add(newVertices.Count + indices[i+2]);
                }

                //add points
                foreach (PolygonNode node in nodes)
                {
                    newVertices.Add(newVertices[node.index]);
                    newUVs.Add(Vector2.zero);
                    newNormals.Add(-planeNormal);
                }
            }
        }*/

        //create new mesh
        Mesh slice = new Mesh();
        slice.SetVertices(newVertices);
        slice.SetNormals(newNormals);
        slice.SetUVs(0, newUVs);
        slice.SetTriangles(newTriangles, 0);
        slice.RecalculateBounds();

        return slice;
    }
}