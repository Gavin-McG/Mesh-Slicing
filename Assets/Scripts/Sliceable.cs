using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;

[System.Serializable] enum EdgeFillMode { None, Center, Triangulate }
[System.Serializable] enum MomentumMode { Reset, Simple, Advanced }

public struct Triangle
{
    public int p1, p2, p3;
    public int subMesh;

    public Triangle(int p1, int p2, int p3, int subMesh)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
        this.subMesh = subMesh;
    }

    public void RotateCW()
    {
        int t = p1;
        p1 = p3;
        p3 = p2;
        p2 = t;
    }

    public void RotateCCW()
    {
        int t = p1;
        p1 = p2;
        p2 = p3;
        p3 = t;
    }

    public override string ToString()
    {
        return "(" + p1 + ", " + p2 + ", " + p3 + ")";
    }
}

public class Sliceable : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshFilter emptyPrefab;
    [Space(10)]
    [SerializeField] EdgeFillMode edgeFillMode = EdgeFillMode.Center;
    [SerializeField] MomentumMode momentumMode = MomentumMode.Simple;
    [Space(10)]
    [SerializeField] Material sliceMaterial;
    [SerializeField] bool hasSliceSubMesh = false;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        Slicer.MakeSlice.AddListener(Slice);
    }

    bool InSlice(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
    {
        Vector3 offset = point - planePoint;
        float dotProduct = Vector3.Dot(offset, planeNormal);
        return dotProduct > 0.00001;
    }

    List<Vector3> GetCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        // Calculate corners relative to the center
        return new List<Vector3>
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z), // Bottom-back-left
            center + new Vector3(extents.x, -extents.y, -extents.z),  // Bottom-back-right
            center + new Vector3(-extents.x, -extents.y, extents.z),  // Bottom-front-left
            center + new Vector3(extents.x, -extents.y, extents.z),   // Bottom-front-right
            center + new Vector3(-extents.x, extents.y, -extents.z),  // Top-back-left
            center + new Vector3(extents.x, extents.y, -extents.z),   // Top-back-right
            center + new Vector3(-extents.x, extents.y, extents.z),   // Top-front-left
            center + new Vector3(extents.x, extents.y, extents.z)     // Top-front-right
        };
    }

    bool IntersectsBounds(Bounds bounds, Vector3 planePoint, Vector3 planeNormal)
    {
        //get corners of bounds
        List<Vector3> boundsCorners = GetCorners(bounds);

        //count points on positive side of bounds
        int s = 0;
        for (int i=0; i<boundsCorners.Count; i++)
        {
            s += Vector3.Dot(boundsCorners[i]-planePoint, planeNormal)>0?1:0;
        }

        return (s != 0 && s != boundsCorners.Count);
    }

    float EdgePortion(Vector3 point1, Vector3 point2, Vector3 planePoint, Vector3 planeNormal)
    {
        Vector3 lineDir = point2 - point1;
        float dotProduct = Vector3.Dot(planeNormal, lineDir);
        float t = Vector3.Dot(planePoint - point1, planeNormal) / dotProduct;

        t = Math.Clamp(t, 0.0f, 1.0f);

        return t;
    }

    Vector3 EdgeIntercept(Vector3 point1, Vector3 point2, float t)
    {
        return point1 + t * (point2 - point1);
    }
    Vector2 EdgeUV(Vector2 uv1,  Vector2 uv2, float t)
    {
        return uv1 + t * (uv2 - uv1);
    }
    Vector3 EdgeNormal(Vector3 normal1, Vector3 normal2, float t)
    {
        return normal1 + t * (normal2 - normal1);
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

    Triangle[] GetTriangles(Mesh mesh)
    {
        int[] indices = mesh.triangles;
        int totalTriangles = indices.Length/3;
        Triangle[] triangles = new Triangle[totalTriangles];

        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            SubMeshDescriptor subMeshDescriptor = mesh.GetSubMesh(submesh);
            int indexStart = subMeshDescriptor.indexStart / 3;
            int indexEnd = indexStart + (subMeshDescriptor.indexCount / 3);

            for (int i = indexStart; i < indexEnd; i++)
            {
                triangles[i] = new Triangle(indices[i*3+0], indices[i*3+1], indices[i*3+2], submesh);
            }
        }

        return triangles;
    }

    public void AssignTrianglesToMesh(Mesh mesh, List<Triangle> triangles)
    {
        if (triangles.Count == 0) return;

        // Determine the number of submeshes
        int subMeshCount = triangles[triangles.Count - 1].subMesh + 1;
        mesh.subMeshCount = subMeshCount;

        // Prepare triangle lists for each submesh
        List<List<int>> submeshTriangles = new List<List<int>>(subMeshCount);
        for (int i = 0; i < subMeshCount; i++)
            submeshTriangles.Add(new List<int>());

        // Populate submesh triangle lists
        foreach (var tri in triangles)
        {
            submeshTriangles[tri.subMesh].Add(tri.p1);
            submeshTriangles[tri.subMesh].Add(tri.p2);
            submeshTriangles[tri.subMesh].Add(tri.p3);
        }

        // Assign triangles to the mesh
        for (int i = 0; i < subMeshCount; i++)
        {
            mesh.SetTriangles(submeshTriangles[i], i);
        }
    }


    Mesh GetMeshSlice(Mesh original, Vector3 planePoint, Vector3 planeNormal)
    {
        //values for first new mesh
        Dictionary<int, int> vectorDict = new();
        List<Vector3> newVertices = new ();
        List<Vector3> newNormals = new();
        List<Vector2> newUVs = new();
        List<Triangle> newTriangles = new();

        Dictionary<(int,int), int> cutPoints = new();
        List<(int,int)> cutEdges = new();

        //get mesh data
        Vector3[] vertices = original.vertices;
        Vector3[] normals = original.normals;
        Vector2[] UVs = original.uv;
        Triangle[] triangles = GetTriangles(original);
        

        //add points to dict
        for (int i = 0; i < vertices.Length; i++)
        {
            if (InSlice(vertices[i], planePoint, planeNormal))
            {
                vectorDict.Add(i, newVertices.Count);
                newVertices.Add(vertices[i]);
                newNormals.Add(normals[i]);
                newUVs.Add(UVs[i]);
            }
        }

        //check for empty mesh
        if (newVertices.Count == 0 || newVertices.Count == vertices.Length)
        {
            return null;
        }

        //create new traingles from old triangles
        foreach (Triangle tri in triangles)
        {
            int hasV1 = vectorDict.ContainsKey(tri.p1) ? 1 : 0;
            int hasV2 = vectorDict.ContainsKey(tri.p2) ? 1 : 0;
            int hasV3 = vectorDict.ContainsKey(tri.p3) ? 1 : 0;

            int scenerio = (hasV3 << 2) + (hasV2 << 1) + (hasV1 << 0);

            int count = 0;
            switch (scenerio)
            {
                case 0:
                    //No vertices in slice
                    count = 0;
                    break;
                case 1:
                    //only v1 in slice
                    count = 1;
                    break;
                case 2:
                    //only v2 in slice
                    count = 1;
                    tri.RotateCCW();
                    break;
                case 3:
                    //v1 and v2 in slice
                    count = 2;
                    tri.RotateCW();
                    break;
                case 4:
                    //only v3 in slice
                    count = 1;
                    tri.RotateCW();
                    break;
                case 5:
                    //v1 and v3 in slice
                    count = 2;
                    tri.RotateCCW();
                    break;
                case 6:
                    //v2 and v3 in slice
                    count = 2;
                    break;
                case 7:
                    //all vertices in slice
                    count = 3;
                    break;
            }

            //add new geometry
            int newPoint1, newPoint2;
            switch (count)
            {
                case 0:
                    continue;
                case 1:
                    //get intercept point 1
                    if (cutPoints.ContainsKey((tri.p1, tri.p2)))
                    {
                        //get existing point
                        newPoint1 = cutPoints[(tri.p1, tri.p2)];
                    }
                    else
                    {
                        //create new point
                        newPoint1 = newVertices.Count;

                        float t = EdgePortion(vertices[tri.p1], vertices[tri.p2], planePoint, planeNormal);
                        newVertices.Add(EdgeIntercept(vertices[tri.p1], vertices[tri.p2], t));
                        newUVs.Add(EdgeUV(UVs[tri.p1], UVs[tri.p2], t));
                        newNormals.Add(EdgeNormal(normals[tri.p1], normals[tri.p2], t));

                        cutPoints.Add((tri.p1, tri.p2), newPoint1);
                    }

                    //get intercept point 2
                    if (cutPoints.ContainsKey((tri.p1, tri.p3)))
                    {
                        //get existing point
                        newPoint2 = cutPoints[(tri.p1, tri.p3)];
                    }
                    else
                    {
                        //create new point
                        newPoint2 = newVertices.Count;

                        float t = EdgePortion(vertices[tri.p1], vertices[tri.p3], planePoint, planeNormal);
                        newVertices.Add(EdgeIntercept(vertices[tri.p1], vertices[tri.p3], t));
                        newUVs.Add(EdgeUV(UVs[tri.p1], UVs[tri.p3], t));
                        newNormals.Add(EdgeNormal(normals[tri.p1], normals[tri.p3], t));

                        cutPoints.Add((tri.p1, tri.p3), newPoint2);
                    }

                    //add triangle to slice
                    newTriangles.Add(new Triangle(vectorDict[tri.p1], newPoint1, newPoint2, tri.subMesh));
                    //add new edge to list
                    cutEdges.Add((newPoint1, newPoint2));

                    break;
                case 2:
                    //get intercept point 1
                    if (cutPoints.ContainsKey((tri.p1, tri.p2)))
                    {
                        //get existing point
                        newPoint1 = cutPoints[(tri.p1, tri.p2)];
                    }
                    else
                    {
                        //create new point
                        newPoint1 = newVertices.Count;

                        float t = EdgePortion(vertices[tri.p1], vertices[tri.p2], planePoint, planeNormal);
                        newVertices.Add(EdgeIntercept(vertices[tri.p1], vertices[tri.p2], t));
                        newUVs.Add(EdgeUV(UVs[tri.p1], UVs[tri.p2], t));
                        newNormals.Add(EdgeNormal(normals[tri.p1], normals[tri.p2], t));

                        cutPoints.Add((tri.p1, tri.p2), newPoint1);
                    }

                    //get intercept point 2
                    if (cutPoints.ContainsKey((tri.p1, tri.p3)))
                    {
                        //get existing point
                        newPoint2 = cutPoints[(tri.p1, tri.p3)];
                    }
                    else
                    {
                        //create new point
                        newPoint2 = newVertices.Count;

                        float t = EdgePortion(vertices[tri.p1], vertices[tri.p3], planePoint, planeNormal);
                        newVertices.Add(EdgeIntercept(vertices[tri.p1], vertices[tri.p3], t));
                        newUVs.Add(EdgeUV(UVs[tri.p1], UVs[tri.p3], t));
                        newNormals.Add(EdgeNormal(normals[tri.p1], normals[tri.p3], t));

                        cutPoints.Add((tri.p1, tri.p3), newPoint2);
                    }

                    //add trapezoid to slice
                    newTriangles.Add(new Triangle(newPoint1, vectorDict[tri.p2], vectorDict[tri.p3], tri.subMesh));
                    newTriangles.Add(new Triangle(newPoint1, vectorDict[tri.p3], newPoint2, tri.subMesh));
                    //add new edge to list
                    cutEdges.Add((newPoint2, newPoint1));

                    break;
                case 3:
                    //add traingle to slice
                    newTriangles.Add(new Triangle(vectorDict[tri.p1], vectorDict[tri.p2], vectorDict[tri.p3], tri.subMesh));
                    break;
            }
        }


        //decide what submesh to put slice triangles in
        int subMeshCount = hasSliceSubMesh ? original.subMeshCount : original.subMeshCount+1;
        int sliceSubmesh = subMeshCount - 1;

        if (edgeFillMode == EdgeFillMode.Center)
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
                    newTriangles.Add(new Triangle(newVertices.Count + 1, newVertices.Count, centerPointIndex, sliceSubmesh));

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
                    newTriangles.Add(new Triangle(newVertices.Count + indices[i], newVertices.Count + indices[i + 1], newVertices.Count + indices[i + 2], sliceSubmesh));
                }

                //add points
                foreach (PolygonNode node in nodes)
                {
                    newVertices.Add(newVertices[node.index]);
                    newUVs.Add(Vector2.zero);
                    newNormals.Add(-planeNormal);
                }
            }
        }

        //create new mesh
        Mesh slice = new Mesh();
        slice.SetVertices(newVertices);
        slice.SetNormals(newNormals);
        slice.SetUVs(0, newUVs);
        AssignTrianglesToMesh(slice, newTriangles);
        slice.RecalculateBounds();

        return slice;
    }


    GameObject CreateNewPeice(MeshFilter prefabFilter, Mesh mesh)
    {
        //create new GameObject
        MeshFilter newFilter = Instantiate(prefabFilter);
        newFilter.name = gameObject.name;

        //set GameObject values
        newFilter.mesh = mesh;
        newFilter.transform.position = transform.position;
        newFilter.transform.rotation = transform.rotation;
        newFilter.transform.localScale = transform.localScale;

        //set mesh collider
        MeshCollider collider = newFilter.GetComponent<MeshCollider>();
        if (collider != null)
        {
            collider.sharedMesh = mesh;
        }

        //set mesh renderer materials
        MeshRenderer oldRenderer = gameObject.GetComponent<MeshRenderer>();
        MeshRenderer newRenderer = newFilter.GetComponent<MeshRenderer>();
        if (oldRenderer != null && newRenderer != null)
        {
            Material[] currentMaterials = oldRenderer.materials;
            Material[] newMaterials = new Material[currentMaterials.Length + (hasSliceSubMesh ? 0 : 1)];

            // Copy existing materials
            for (int i = 0; i < currentMaterials.Length; i++)
            {
                newMaterials[i] = currentMaterials[i];
            }

            if (!hasSliceSubMesh)
            {
                // Add the new material
                newMaterials[newMaterials.Length - 1] = sliceMaterial;
            }

            newRenderer.materials = newMaterials;
        }

        //set rigidbody
        Rigidbody oldRigidBody = gameObject.GetComponent<Rigidbody>();
        Rigidbody newRigidBody = newFilter.GetComponent<Rigidbody>();
        if (oldRigidBody != null && newRigidBody != null)
        {
            if (momentumMode == MomentumMode.Simple)
            {
                newRigidBody.velocity = oldRigidBody.velocity;
                newRigidBody.angularVelocity = oldRigidBody.angularVelocity;
            }
            else if (momentumMode == MomentumMode.Advanced)
            {
                Debug.LogWarning("MomentumMode 'Advanced' not yet implemented!");
            }
        }

        return newFilter.gameObject;
    }

    void Slice(Vector3 planePoint, Vector3 planeNormal)
    {
        //convert from world to local space
        planePoint = transform.InverseTransformPoint(planePoint);
        planeNormal = transform.InverseTransformVector(planeNormal);

        //normalize normal
        planeNormal.Normalize();

        //check boudning box
        Mesh original = meshFilter.mesh;
        if (!IntersectsBounds(original.bounds, planePoint, planeNormal)) return;

        //get new slices
        Mesh slice1 = GetMeshSlice(original, planePoint, planeNormal);
        Mesh slice2 = slice1==null? null : GetMeshSlice(original, planePoint, -planeNormal);

        //check that object is split
        if (slice2)
        {
            //assign new slices to GameObjects
            CreateNewPeice(emptyPrefab, slice1);
            CreateNewPeice(emptyPrefab, slice2);

            //destroy original object
            Destroy(gameObject);
            Slicer.MakeSlice.RemoveListener(Slice);
        }
    }
}