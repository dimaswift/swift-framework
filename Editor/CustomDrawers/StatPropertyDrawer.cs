using SwiftFramework.Core.SharedData.Upgrades;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(BaseStat), true)]
    public class StatPropertyDrawer : PropertyDrawer
    {
        private const string STATS = "stats";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative(STATS), label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(STATS), label = null, true);
        }
    }
}
