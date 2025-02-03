using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;


public class MeshSaver
{
    static bool saved = false;

    public static void SaveDebugInfo(Mesh mesh, Transform objectTransform, Vector3 slicePosition, Vector3 sliceNormal)
    {
        if (saved) return;
        saved = true;

        if (mesh == null || objectTransform == null)
        {
            Debug.LogError("Invalid input: Mesh and both transforms must be provided.");
            return;
        }

        // Ensure directory exists
        string directory = "Assets/Debug/BuggedInfo";
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Sanitize the mesh name to remove whitespace and special characters
        string safeMeshName = SanitizeFileName(mesh.name);

        // Generate paths
        string meshPath = $"{directory}/{safeMeshName}_Mesh.asset";
        string debugInfoPath = $"{directory}/{safeMeshName}_DebugInfo.asset";

        // Create and save the mesh asset
        Mesh newMesh = Object.Instantiate(mesh); // Duplicate mesh to avoid modifying original
        AssetDatabase.CreateAsset(newMesh, meshPath);

        // Create and save the debug info asset
        MeshDebugInfo debugInfo = ScriptableObject.CreateInstance<MeshDebugInfo>();
        debugInfo.mesh = newMesh;
        debugInfo.objectPosition = objectTransform.position;
        debugInfo.objectRotation = objectTransform.rotation;
        debugInfo.objectScale = objectTransform.localScale;
        debugInfo.slicePosition = objectTransform.TransformPoint(slicePosition);
        debugInfo.sliceNormal = objectTransform.TransformVector(sliceNormal);

        AssetDatabase.CreateAsset(debugInfo, debugInfoPath);

        // Save and refresh asset database
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Mesh saved at: {meshPath}");
        Debug.Log($"Debug info saved at: {debugInfoPath}");
    }

    /// <summary>
    /// Removes spaces and special characters from a file name.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "UnnamedMesh";

        // Remove whitespace
        fileName = fileName.Replace(" ", "_");

        // Remove invalid file characters
        fileName = Regex.Replace(fileName, "[^a-zA-Z0-9_-]", "");

        return fileName;
    }
}