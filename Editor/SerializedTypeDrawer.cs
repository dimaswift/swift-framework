using UnityEditor;
using UnityEngine;

namespace Swift.Core.Editor
{
    /*[CustomPropertyDrawer(typeof(SerializedType))]
    public class SerializedTypeDrawer : PropertyDrawer
    {
        private ManualClassPropertyDrawer drawer;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheDrawer(property, label);
            position.height = EditorGUIUtility.singleLineHeight;
            drawer.Draw(position);
        }

        private void CacheDrawer(SerializedProperty property, GUIContent label)
        {
            if (drawer == null)
            {
                drawer = new ManualClassPropertyDrawer(label.text, c => typeof(ModuleConfig).IsAssignableFrom(c), property.FindPropertyRelative("type"));
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CacheDrawer(property, label);
            return drawer.TotalPropertyHeight;
        }
    }*/
}