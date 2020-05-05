using System;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(UnixDuration), true)]
    public class UnixDurationPropertyDrawer : PropertyDrawer
    {
        private const string FIELD = "seconds";

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty prop = property.FindPropertyRelative(FIELD);

            TimeSpan span = TimeSpan.FromSeconds(prop.longValue);

            label.tooltip = string.IsNullOrEmpty(property.tooltip) ? span.ToTimerString() : $"{span.ToTimerString()}({property.tooltip})";

            var labelRect = EditorGUI.PrefixLabel(position, label);

            EditorGUI.indentLevel = 0;

            position.x = labelRect.x;
            position.height = 18;
            position.width = 25;
            var gap = 15;
   
            var newSeconds = Mathf.Clamp(EditorGUI.IntField(position, (int)span.Seconds), 0, int.MaxValue);
            position.x += 15 + gap;
            EditorGUI.LabelField(position, "S");

            position.x += position.width ;
            var newMinutes = Mathf.Clamp(EditorGUI.IntField(position, (int)span.Minutes), 0, int.MaxValue);
            position.x += 15 + gap;
            EditorGUI.LabelField(position, "M");

            position.x += position.width;
            var newHours = Mathf.Clamp(EditorGUI.IntField(position, (int)span.Hours), 0, int.MaxValue);
            position.x += 15 + gap;
            EditorGUI.LabelField(position, "H");

            position.x += position.width;
            var newDays = Mathf.Clamp(EditorGUI.IntField(position, (int)span.Days), 0, int.MaxValue);
            position.x += 15 + gap;
            EditorGUI.LabelField(position, "D");

            if (GUI.changed)
            {
                prop.longValue = UnixDuration.GetSeconds(newSeconds, newMinutes, newHours, newDays);

                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();

        }
    }
}
