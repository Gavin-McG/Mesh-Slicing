using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sliceable : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshFilter emptyPrefab;

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
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int hasV1 = vectorDict.ContainsKey(triangles[i + 0]) ? 1 : 0;
            int hasV2 = vectorDict.ContainsKey(triangles[i + 1]) ? 1 : 0;
            int hasV3 = vectorDict.ContainsKey(triangles[i + 2]) ? 1 : 0;

            int scenerio = (hasV3 << 2) + (hasV2 << 1) + (hasV1 << 0);

            int count = 0;
            int v1 = 0, v2 = 0, v3 = 0;
            switch (scenerio)
            {
                case 0:
                    //No vertices in slice
                    count = 0;
                    break;
                case 1:
                    //only v1 in slice
                    count = 1;
                    v1 = triangles[i + 0];
                    v2 = triangles[i + 1];
                    v3 = triangles[i + 2];
                    break;
                case 2:
                    //only v2 in slice
                    count = 1;
                    v1 = triangles[i + 1];
                    v2 = triangles[i + 2];
                    v3 = triangles[i + 0];
                    break;
                case 3:
                    //v1 and v2 in slice
                    count = 2;
                    v1 = triangles[i + 2];
                    v2 = triangles[i + 0];
                    v3 = triangles[i + 1];
                    break;
                case 4:
                    //only v3 in slice
                    count = 1;
                    v1 = triangles[i + 2];
                    v2 = triangles[i + 0];
                    v3 = triangles[i + 1];
                    break;
                case 5:
                    //v1 and v3 in slice
                    count = 2;
                    v1 = triangles[i + 1];
                    v2 = triangles[i + 2];
                    v3 = triangles[i + 0];
                    break;
                case 6:
                    //v2 and v3 in slice
                    count = 2;
                    v1 = triangles[i + 0];
                    v2 = triangles[i + 1];
                    v3 = triangles[i + 2];
                    break;
                case 7:
                    //all vertices in slice
                    count = 3;
                    v1 = triangles[i + 0];
                    v2 = triangles[i + 1];
                    v3 = triangles[i + 2];
                    break;
            }

            //add new geometry
            int newPoint1=0, newPoint2=0;
            switch (count)
            {
                case 0:
                    continue;
                case 1:
                    //get intercept point 1
                    if (cutPoints.ContainsKey((v1, v2)))
                    {
                        //get existing point
                        newPoint1 = cutPoints[(v1, v2)];
                    }
                    else
                    {
                        //create new point
                        newPoint1 = newVertices.Count;

                        float t = EdgePortion(vertices[v1], vertices[v2], planePoint, planeNormal);
                        newVertices.Add(EdgeIntercept(vertices[v1], vertices[v2], t));
                        newUVs.Add(EdgeUV(UVs[v1], UVs[v2], t));
                        newNormals.Add(EdgeNormal(normals[v1], normals[v2], t));

                        cutPoints.Add((v1, v2), newPoint1);
                    }

                    //get intercept point 2
                    if (cutPoints.ContainsKey((v1, v3)))
                    {
                        //get existing point
                        newPoint2 = cutPoints[(v1, v3)];
                    }
                    else
                    {
                        //create new point
                        newPoint2 = newVertices.Count;

                        float t = EdgePortion(vertices[v1], vertices[v3], planePoint, planeNormal);
                        newVertices.Add(EdgeIntercept(vertices[v1], vertices[v3], t));
                        newUVs.Add(EdgeUV(UVs[v1], UVs[v3], t));
                        newNormals.Add(EdgeNormal(normals[v1], normals[v3], t));

                        cutPoints.Add((v1, v3), newPoint2);
                    }

                    //add triangle to slice
                    newTriangles.AddRange(new int[] { vectorDict[v1], newPoint1, newPoint2});
                    //add new edge to list
                    cutEdges.Add((newPoint1, newPoint2));

                    break;
                case 2:
                    //get intercept point 1
                    if (cutPoints.ContainsKey((v1, v2)))
                    {
                        //get existing point
                        newPoint1 = cutPoints[(v1, v2)];
                    }
                    else
                    {
                        //create new point
                        newPoint1 = newVertices.Count;

                        float t = EdgePortion(vertices[v1], vertices[v2], planePoint, planeNormal);
                        newVertices.Add(EdgeIntercept(vertices[v1], vertices[v2], t));
                        newUVs.Add(EdgeUV(UVs[v1], UVs[v2], t));
                        newNormals.Add(EdgeNormal(normals[v1], normals[v2], t));

                        cutPoints.Add((v1, v2), newPoint1);
                    }

                    //get intercept point 2
                    if (cutPoints.ContainsKey((v1, v3)))
                    {
                        //get existing point
                        newPoint2 = cutPoints[(v1, v3)];
                    }
                    else
                    {
                        //create new point
                        newPoint2 = newVertices.Count;

                        float t = EdgePortion(vertices[v1], vertices[v3], planePoint, planeNormal);
                        newVertices.Add(EdgeIntercept(vertices[v1], vertices[v3], t));
                        newUVs.Add(EdgeUV(UVs[v1], UVs[v3], t));
                        newNormals.Add(EdgeNormal(normals[v1], normals[v3], t));

                        cutPoints.Add((v1, v3), newPoint2);
                    }

                    //add trapezoid to slice
                    newTriangles.AddRange(new int[] { newPoint1, vectorDict[v2], vectorDict[v3] });
                    newTriangles.AddRange(new int[] { newPoint1, vectorDict[v3], newPoint2 });
                    //add new edge to list
                    cutEdges.Add((newPoint2, newPoint1));

                    break;
                case 3:
                    //add traingle to slice
                    newTriangles.AddRange(new int[] { vectorDict[v1], vectorDict[v2], vectorDict[v3] });
                    break;
            }
        }



        //TODO - make better algorithm for complex shapes
        //find center point of new points on edge
        if (cutEdges.Count > 0)
        {
            Vector3 centerPoint = Vector3.zero;
            for (int i = 0; i < cutEdges.Count; ++i)
            {
                centerPoint += newVertices[cutEdges[i].Item1] + newVertices[cutEdges[i].Item2];
            }
            centerPoint /= cutEdges.Count * 2;
            if (centerPoint.magnitude > 0.05)
            {
                Debug.LogWarning("Big centerpoint");
            }

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
        

        //create new mesh
        Mesh slice = new Mesh();
        slice.SetVertices(newVertices);
        slice.SetNormals(newNormals);
        slice.SetUVs(0, newUVs);
        slice.SetTriangles(newTriangles, 0);
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

        //set mesh renderer
        MeshRenderer oldRenderer = gameObject.GetComponent<MeshRenderer>();
        MeshRenderer newRenderer = newFilter.GetComponent<MeshRenderer>();
        if (oldRenderer != null && newRenderer != null)
        {
            newRenderer.material = oldRenderer.material;
        }

        //set rigidbody
        Rigidbody oldRigidBody = gameObject.GetComponent<Rigidbody>();
        Rigidbody newRigidBody = newFilter.GetComponent<Rigidbody>();
        if (oldRigidBody != null && newRigidBody != null)
        {
            newRigidBody.velocity = oldRigidBody.velocity;
            newRigidBody.angularVelocity = oldRigidBody.angularVelocity;
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
        if (slice2 != null)
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