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
