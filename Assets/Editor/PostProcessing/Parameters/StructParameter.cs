using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
[DebuggerDisplay("{m_Value} ({m_OverrideState})")]
public class StructParameter<T> : VolumeParameter<T> where T : IStructParameter { }

public interface IStructParameter { }

[CustomPropertyDrawer(typeof(IStructParameter), true)]
public class StructDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.indentLevel++;
        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.indentLevel--;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUI.GetPropertyHeight(property);
    }
}