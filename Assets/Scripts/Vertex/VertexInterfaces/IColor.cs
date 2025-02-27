using UnityEngine;

public interface IColor<T, U> 
    where T : struct, IVertex<T, U> 
    where U : struct, ITexCoord<U>
{
    Vector4 Color { get; set; }
}
