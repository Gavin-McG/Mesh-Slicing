using System.Collections;
using System.Collections.Generic;
using System.Linq;
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


    public static float EdgePortion<T>(T vertex1, T vertex2, Plane plane) where T : struct, IVertex<T>
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


    public static BoneWeight LerpBoneWeight(BoneWeight a, BoneWeight b, float t)
    {
        Dictionary<int, float> weightMap = new Dictionary<int, float>(8);

        // Add weights from BoneWeight A
        void AddWeight(int index, float weight, float factor)
        {
            if (index < 0 || weight <= 0f) return; // Ignore invalid or zero weights
            float adjustedWeight = weight * factor;
            if (weightMap.TryGetValue(index, out float existingWeight))
                weightMap[index] = existingWeight + adjustedWeight;
            else
                weightMap[index] = adjustedWeight;
        }

        // Blend weights
        float tInv = 1f - t;
        AddWeight(a.boneIndex0, a.weight0, tInv);
        AddWeight(a.boneIndex1, a.weight1, tInv);
        AddWeight(a.boneIndex2, a.weight2, tInv);
        AddWeight(a.boneIndex3, a.weight3, tInv);
        AddWeight(b.boneIndex0, b.weight0, t);
        AddWeight(b.boneIndex1, b.weight1, t);
        AddWeight(b.boneIndex2, b.weight2, t);
        AddWeight(b.boneIndex3, b.weight3, t);

        // Sort by highest weight
        var sortedWeights = weightMap.OrderByDescending(kv => kv.Value).ToArray();

        // Ensure at least 4 influences
        int[] boneIndices = new int[4] { 0, 0, 0, 0 };
        float[] weights = new float[4] { 0f, 0f, 0f, 0f };

        float totalWeight = 0f;
        for (int i = 0; i < sortedWeights.Length && i < 4; i++)
        {
            boneIndices[i] = sortedWeights[i].Key;
            weights[i] = sortedWeights[i].Value;
            totalWeight += sortedWeights[i].Value;
        }

        // Normalize weights to sum to 1
        if (totalWeight > 0f)
        {
            float invTotalWeight = 1f / totalWeight;
            for (int i = 0; i < 4; i++) weights[i] *= invTotalWeight;
        }

        // Assign final values to BoneWeight
        BoneWeight result = new BoneWeight
        {
            boneIndex0 = boneIndices[0],
            weight0 = weights[0],
            boneIndex1 = boneIndices[1],
            weight1 = weights[1],
            boneIndex2 = boneIndices[2],
            weight2 = weights[2],
            boneIndex3 = boneIndices[3],
            weight3 = weights[3]
        };

        return result;
    }
}
