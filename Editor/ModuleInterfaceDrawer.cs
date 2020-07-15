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
            Init(property);
            
            position.height = EditorGUIUtility.singleLineHeight;
            
            interfaceDrawer.Draw(position);

            position.y += interfaceDrawer.TotalPropertyHeight;

            implementationDrawer.Draw(position);
            
            position.y += implementationDrawer.TotalPropertyHeight;

            EditorGUI.PropertyField(position, property.FindPropertyRelative("config"));
            
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            EditorGUI.PropertyField(position, property.FindPropertyRelative("behaviour"));
        }

        private void Init(SerializedProperty property)
        {
            if (interfaceDrawer == null)
            {
                interfaceDrawer = new ClassPropertyDrawer("Module Interface", IsModule,
                    property.FindPropertyRelative("interfaceType"));

                implementationDrawer = new ManualClassPropertyDrawer("Implementation", IsImplementation,
                    property.FindPropertyRelative("implementationType"));

                interfaceDrawer.OnSelectionChanged += implementationDrawer.Rebuild;
            }

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        { 
            Init(property);
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 +
                   (implementationDrawer.TotalPropertyHeight + interfaceDrawer.TotalPropertyHeight);
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