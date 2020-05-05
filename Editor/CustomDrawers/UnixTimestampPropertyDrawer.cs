using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(UnixTimestamp))]
    public class UnixTimestampPropertyDrawer : PropertyDrawer
    {
        private const string FIELD = "timestampSeconds";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect rect = position;

            SerializedProperty prop = property.FindPropertyRelative(FIELD);

            DateTimeOffset date = DateTimeOffset.FromUnixTimeSeconds(prop.longValue);

            EditorGUI.BeginProperty(position, label, property);

          
            if (prop.longValue == 0)
            {
                date = DateTimeOffset.Now;
            }
 
            string dateString = date.ToString("MM/dd/yyyy HH:mm");

            label.tooltip = string.IsNullOrEmpty(property.tooltip) ? dateString : $"{dateString}({property.tooltip})";

            var labelRect = EditorGUI.PrefixLabel(position, label);
            EditorGUI.indentLevel = 0;

            rect.x = labelRect.x;
            rect.height = 18;
            rect.width = 20;
            var gap = 5;

            var newMonth = Mathf.Clamp(EditorGUI.IntField(rect, date.Month), 1, 12);
            rect.x += rect.width + gap;
            var newDay = Mathf.Clamp(EditorGUI.IntField(rect, date.Day), 1, 31);
            rect.x += rect.width + gap;
            rect.width = 55;
            var newYear = Mathf.Clamp(EditorGUI.IntField(rect, date.Year), 2020, 2100);
            rect.x += rect.width + gap;
            rect.width = 20;
            var newHour = Mathf.Clamp(EditorGUI.IntField(rect, date.Hour), 0, 23);
            rect.x += rect.width + gap;
            var newMinute = Mathf.Clamp(EditorGUI.IntField(rect, date.Minute), 0, 59);
            rect.x = labelRect.x - 45;
            rect.width = 40;
            date = new DateTimeOffset(newYear, newMonth, newDay, newHour, newMinute, 0, TimeSpan.Zero);

            if (GUI.Button(rect, "now"))
            {
                date = DateTimeOffset.UtcNow;
            }

            prop.longValue = date.ToUnixTimeSeconds();
            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}
