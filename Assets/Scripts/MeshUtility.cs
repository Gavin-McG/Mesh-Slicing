using MeshSlicing.Vertex;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class MeshUtility
{


    public static List<Vector3> GetCorners(Mesh mesh)
    {
        Bounds bounds = mesh.bounds;
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


    public static bool InSlice(Vector3 point, Plane plane)
    {
        Vector3 offset = point - plane.point;
        float dotProduct = Vector3.Dot(offset, plane.normal);
        return dotProduct > 0.00001;
    }


    public static float EdgePortion<T, U>(T vertex1, T vertex2, Plane plane)
        where T : struct, IVertex<T, U>
        where U : struct, ITexCoord<U>
    {
        return EdgePortion(vertex1.Position, vertex2.Position, plane);
    }

    public static float EdgePortion(Vector3 point1, Vector3 point2, Plane plane)
    {
        Vector3 lineDir = point2 - point1;
        float dotProduct = Vector3.Dot(plane.normal, lineDir);
        float t = Vector3.Dot(plane.point - point1, plane.normal) / dotProduct;

        t = Mathf.Clamp(t, 0.0f, 1.0f);

        return t;
    }


    public static bool VectorsClose(Vector3 v1, Vector3 v2, float epsilon = 0.0001f)
    {
        return Mathf.Abs(v1.x - v2.x) < epsilon &&
               Mathf.Abs(v1.y - v2.y) < epsilon &&
               Mathf.Abs(v1.z - v2.z) < epsilon;
    }



    public static bool IntersectsBounds(Mesh mesh, Plane plane)
    {
        //get corners of bounds
        List<Vector3> boundsCorners = GetCorners(mesh);

        //count points on positive side of bounds
        int s = 0;
        for (int i = 0; i < boundsCorners.Count; i++)
        {
            s += Vector3.Dot(boundsCorners[i] - plane.point, plane.normal) > 0 ? 1 : 0;
        }

        return (s != 0 && s != boundsCorners.Count);
    }

    public static Triangle[] GetTriangles(Mesh mesh)
    {
        int[] indices = mesh.triangles;
        int totalTriangles = indices.Length / 3;
        Triangle[] triangles = new Triangle[totalTriangles];

        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            SubMeshDescriptor subMeshDescriptor = mesh.GetSubMesh(submesh);
            int indexStart = subMeshDescriptor.indexStart / 3;
            int indexEnd = indexStart + (subMeshDescriptor.indexCount / 3);

            for (int i = indexStart; i < indexEnd; i++)
            {
                triangles[i] = new Triangle(indices[i * 3 + 0], indices[i * 3 + 1], indices[i * 3 + 2], submesh);
            }
        }

        return triangles;
    }



    public static void AssignTrianglesToMesh(Mesh mesh, List<Triangle> triangles)
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
}
