using UnityEditor;
using UnityEngine;

namespace Swift.Core.Editor
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
                    drawer = new AssetLinkDrawer(typeof(T), fieldInfo, false, AllowCreation);
                }
            }
            drawer.Draw(position, property, label);
            EditorGUI.PropertyField(position, property, label, true);
        }

        protected virtual bool AllowCreation => true;

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
