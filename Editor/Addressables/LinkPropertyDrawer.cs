using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    public class LinkPropertyDrawer<T> : PropertyDrawer
    {
        private BaseLinkDrawer drawer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = base.GetPropertyHeight(property, label);
            if (drawer == null)
            {
                if (typeof(T).IsInterface)
                {
                    drawer = new InterfaceLinkDrawer(typeof(T), fieldInfo);
                }
                else
                {
                    drawer = new AssetLinkDrawer(typeof(T), fieldInfo);
                }
            }
            drawer.Draw(position, property, label);

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return EditorGUI.GetPropertyHeight(property, true);
            }
            else
            {
                return base.GetPropertyHeight(property, label);
            }
        }
    }
}
