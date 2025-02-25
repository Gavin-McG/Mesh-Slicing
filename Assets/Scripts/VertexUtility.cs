using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

//class for using information from vertices
public static class VertexUtility
{

    public static bool InSlice(Vector3 point, Plane plane)
    {
        Vector3 offset = point - plane.point;
        float dotProduct = Vector3.Dot(offset, plane.normal);
        return dotProduct > 0.00001;
    }


    public static float EdgePortion(Vertex vertex1, Vertex vertex2, Plane plane)
    {
        return EdgePortion(vertex1.position, vertex2.position, plane);
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

}
