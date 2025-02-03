using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject to store mesh metadata for debugging.
/// </summary>
public class MeshDebugInfo : ScriptableObject
{
    public Mesh mesh;
    public Vector3 objectPosition;
    public Quaternion objectRotation;
    public Vector3 objectScale;

    public Vector3 slicePosition;
    public Vector3 sliceNormal;
}
