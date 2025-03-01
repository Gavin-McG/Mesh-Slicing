using UnityEngine;

namespace MeshSlicing.Vertex
{
    public interface IBoneWeights<T, U>
        where T : struct, IVertex<T, U>
        where U : struct, ITexCoord<U>
    {
        BoneWeight BoneWeight { get; set; }
    }
}
