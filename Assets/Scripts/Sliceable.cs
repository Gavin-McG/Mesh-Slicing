using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;

[System.Serializable] enum EdgeFillMode { None, Center, Triangulate }
[System.Serializable] enum EdgeUVMode { Zero, Proj }
[System.Serializable] enum MomentumMode { Reset, Simple, Advanced }

public class Sliceable : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshFilter emptyPrefab;
    [Space(10)]
    [SerializeField] EdgeFillMode edgeFillMode = EdgeFillMode.Center;
    [SerializeField] EdgeUVMode edgeUVMode = EdgeUVMode.Proj;
    [SerializeField] MomentumMode momentumMode = MomentumMode.Simple;
    [Space(10)]
    [SerializeField] Material sliceMaterial;
    [SerializeField] bool hasSliceSubMesh = false;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        Slicer.MakeSlice.AddListener(Slice);
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


    Mesh GetMeshSlice<T>(Mesh original, Plane plane) where T : struct, IVertex<T>
    {
        //values for first new mesh
        Dictionary<int, int> vectorDict = new();
        List<T> newVertices = new();
        List<Triangle> newTriangles = new();

        Dictionary<(int,int), int> cutPoints = new();
        List<(int,int)> cutEdges = new();

        //get mesh data
        NativeArray<T> vertices = new T().GetVertices(original);
        Triangle[] triangles = MeshUtility.GetTriangles(original);
        
        //add points to dict
        for (int i = 0; i < vertices.Length; i++)
        {
            if (VertexUtility.InSlice(vertices[i].position, plane))
            {
                vectorDict.Add(i, newVertices.Count);
                newVertices.Add(vertices[i]);
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

                        float t = VertexUtility.EdgePortion(vertices[tri.p1], vertices[tri.p2], plane);
                        newVertices.Add(vertices[tri.p1].Lerp(vertices[tri.p2], t));

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

                        float t = VertexUtility.EdgePortion(vertices[tri.p1], vertices[tri.p3], plane);
                        newVertices.Add(vertices[tri.p1].Lerp(vertices[tri.p3], t));

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

                        float t = VertexUtility.EdgePortion(vertices[tri.p1], vertices[tri.p2], plane);
                        newVertices.Add(vertices[tri.p1].Lerp(vertices[tri.p2], t));

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

                        float t = VertexUtility.EdgePortion(vertices[tri.p1], vertices[tri.p3], plane);
                        newVertices.Add(vertices[tri.p1].Lerp(vertices[tri.p3], t));

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
                    centerPoint += newVertices[cutEdges[i].Item1].position + newVertices[cutEdges[i].Item2].position;
                }
                centerPoint /= cutEdges.Count * 2;

                int centerPointIndex = newVertices.Count;
                T newVertex = new T().Initialize(centerPoint, -plane.normal, Vector2.zero);
                newVertices.Add(newVertex);

                //add new geometry into plane
                for (int i = 0; i < cutEdges.Count; ++i)
                {
                    //new traingle
                    newTriangles.Add(new Triangle(newVertices.Count + 1, newVertices.Count, centerPointIndex, sliceSubmesh));

                    //new points
                    T newVertex1 = newVertices[cutEdges[i].Item1];
                    newVertex1.normal = -plane.normal;
                    newVertex1.uv = Vector2.zero;
                    newVertices.Add(newVertex1);

                    T newVertex2 = newVertices[cutEdges[i].Item2];
                    newVertex1.normal = -plane.normal;
                    newVertex1.uv = Vector2.zero;
                    newVertices.Add(newVertex2);
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
                    if (VertexUtility.VectorsClose(newVertices[pointIndexes[i]].position, newVertices[pointIndexes[j]].position))
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
            List<Polygon> polygons = Polygon.MakePolygons(loops, newVertices, plane.normal, edgeUVMode==EdgeUVMode.Proj);

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
                    Vector2 newUV = edgeUVMode == EdgeUVMode.Zero ? Vector2.zero : node.pos;
                    T newVertex = new T().Initialize(newVertices[node.index].position, -plane.normal, newUV);
                    newVertices.Add(newVertex);
                }
            }
        }

        //create new mesh
        Mesh slice = new T().SetMeshVertices(newVertices);
        MeshUtility.AssignTrianglesToMesh(slice, newTriangles);
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
                newRigidBody.linearVelocity = oldRigidBody.linearVelocity;
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

        Plane plane = new Plane(planePoint, planeNormal);

        //check boudning box
        Mesh original = meshFilter.mesh;
        if (!MeshUtility.IntersectsBounds(original, plane)) return;

        //get new slices
        Mesh slice1 = GetMeshSlice<VertexFull>(original, plane);
        Mesh slice2 = slice1==null? null : GetMeshSlice<VertexFull>(original, -plane);

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