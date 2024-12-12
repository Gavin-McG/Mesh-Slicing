using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Slicer : MonoBehaviour
{
    [SerializeField] bool makeSlice = false;

    public static UnityEvent<Vector3, Vector3> MakeSlice = new UnityEvent<Vector3, Vector3>();

    private void Update()
    {
        if (makeSlice)
        {
            MakeSlice.Invoke(transform.position, transform.up);
            makeSlice = false;
        }
    }
}
