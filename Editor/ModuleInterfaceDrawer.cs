using System;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(ModuleInterface))]
    public class ModuleInterfaceDrawer : PropertyDrawer
    {
        private ClassPropertyDrawer interfaceDrawer;
        private ClassPropertyDrawer implementationDrawer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (interfaceDrawer == null)
            {
                interfaceDrawer = new ClassPropertyDrawer("Module Interface", IsModule,
                    property.FindPropertyRelative("interfaceType"));

                implementationDrawer = new ClassPropertyDrawer("Implementation", IsImplementation,
                    property.FindPropertyRelative("implementationType"));

                interfaceDrawer.OnSelectionChanged += implementationDrawer.Rebuild;
            }

            position.height = EditorGUIUtility.singleLineHeight;

            interfaceDrawer.Draw(position);

            position.y += EditorGUIUtility.singleLineHeight;

            implementationDrawer.Draw(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2;
        }

        private bool IsImplementation(Type type)
        {
            if (interfaceDrawer.SelectedType == null)
            {
                return false;
            }

            if (interfaceDrawer.SelectedType != type && interfaceDrawer.SelectedType.IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }

        private bool IsModule(Type type)
        {
            if (type.IsInterface == false || type.IsVisible == false || type == typeof(IModule))
            {
                return false;
            }

            return typeof(IModule).IsAssignableFrom(type);
        }
    }
}