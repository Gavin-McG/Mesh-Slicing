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
            //MakeSlice.Invoke(transform.position, transform.up);
            for (int i = 0; i < 20; ++i)
            {
                Vector3 pos = transform.position + new Vector3(Random.Range(-2, 2), Random.Range(-2, 2), Random.Range(-2, 2));
                Vector3 dir = new Vector3(Random.Range(-2, 2), Random.Range(-2, 2), Random.Range(-2, 2));
                MakeSlice.Invoke(pos, dir);
            }
            makeSlice = false;
        }
    }
}
