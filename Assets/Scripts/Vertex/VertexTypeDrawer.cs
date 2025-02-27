using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(VertexType))]
public class VertexTypeDrawer : PropertyDrawer
{
    private Type[] selectableTypes;
    private string[] selectableNames;

    public VertexTypeDrawer()
    {
        // Find all classes marked with VertexVariant
        selectableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsValueType && type.GetCustomAttribute<VertexVariant>() != null)
            .ToArray();

        selectableNames = selectableTypes.Select(t => t.FullName).ToArray(); // Store full name for better Type resolution
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty typeProperty = property.FindPropertyRelative("name");

        if (selectableTypes.Length == 0)
        {
            EditorGUI.LabelField(position, label.text, "No selectable classes found");
            return;
        }

        // Find the currently selected index
        int currentIndex = Array.IndexOf(selectableNames, typeProperty.stringValue);
        if (currentIndex < 0) currentIndex = 0;

        // Show dropdown
        int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, selectableNames);
        typeProperty.stringValue = selectableNames[selectedIndex];
    }
}