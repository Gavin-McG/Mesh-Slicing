using UnityEngine;

public interface ITexCoord<T> 
    where T : struct, ITexCoord<T>
{
    public Vector2 UV0 { get; set; }

    public T Lerp(T other, float t, bool clamp = true); 
}


