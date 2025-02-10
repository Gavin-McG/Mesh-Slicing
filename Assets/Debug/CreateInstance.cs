using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateInstance : MonoBehaviour
{
    [SerializeField] MeshDebugInfo meshDebugInfo;
    [SerializeField] MeshFilter prefab;

    [SerializeField] bool runInstance = false;

    private void Update()
    {
        if (runInstance)
        {
            runInstance = false;
            MeshFilter meshFilter = Instantiate(prefab, meshDebugInfo.objectPosition, meshDebugInfo.objectRotation);
            meshFilter.transform.localScale = meshDebugInfo.objectScale;
            meshFilter.mesh = meshDebugInfo.mesh;

            //Slicer.MakeSlice.Invoke(meshDebugInfo.slicePosition, meshDebugInfo.sliceNormal);
        }
    }
}
