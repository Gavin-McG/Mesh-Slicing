using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct Plane
{
    public Vector3 point;
    public Vector3 normal;

    public Plane(Vector3 point, Vector3 normal)
    {
        this.point = point;
        this.normal = normal;
    }

    public static Plane operator -(Plane plane) {
        return new Plane(plane.point, -plane.normal);
    }
}
