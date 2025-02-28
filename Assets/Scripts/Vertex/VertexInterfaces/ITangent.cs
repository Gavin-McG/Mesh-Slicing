using UnityEngine;

namespace MeshSlicing.Vertex
{
    public interface ITangent<T, U>
        where T : struct, IVertex<T, U>
        where U : struct, ITexCoord<U>
    {
        Vector4 Tangent { get; set; }
    }
}