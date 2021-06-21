using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Swift.Core.Editor
{
    [CustomPropertyDrawer(typeof(ModuleInterface))]
    public class ModuleInterfaceDrawer : PropertyDrawer
    {
        private ClassPropertyDrawer interfaceDrawer;
        private ClassPropertyDrawer implementationDrawer;
        private ClassPropertyDrawer configDrawer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);
            
            position.height = EditorGUIUtility.singleLineHeight;
            
            interfaceDrawer.Draw(position);

            position.y += interfaceDrawer.TotalPropertyHeight;

            implementationDrawer.Draw(position);
            
            position.y += implementationDrawer.TotalPropertyHeight;

            configDrawer.Draw(position);

            position.y += configDrawer.TotalPropertyHeight;
            
            EditorGUI.PropertyField(position, property.FindPropertyRelative("behaviour"));

            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            EditorGUI.PropertyField(position, property.FindPropertyRelative("config"));
            
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(position, property.FindPropertyRelative("initializeOnLoad"));
            
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            EditorGUI.PropertyField(position, property.FindPropertyRelative("createAfterAppLoaded"));
            
            if (GUI.changed)
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void Init(SerializedProperty property)
        {
            if (interfaceDrawer == null)
            {
                interfaceDrawer = new ManualClassPropertyDrawer("Module Interface", IsModule,
                    property.FindPropertyRelative("interfaceType").FindPropertyRelative("type"));

                implementationDrawer = new ManualClassPropertyDrawer("Implementation", IsImplementation,
                    property.FindPropertyRelative("implementationType").FindPropertyRelative("type"));
                
                configDrawer = new ManualClassPropertyDrawer("Config", IsConfig, 
                    property.FindPropertyRelative("configType").FindPropertyRelative("type"));
                
                interfaceDrawer.OnSelectionChanged += () =>
                {
                    configDrawer.Rebuild();
                    implementationDrawer.Rebuild();
                };
                
                implementationDrawer.OnSelectionChanged += () =>
                {
                    configDrawer.Rebuild();
                };
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        { 
            Init(property);
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4 +
                   (implementationDrawer.TotalPropertyHeight + interfaceDrawer.TotalPropertyHeight + configDrawer.TotalPropertyHeight);
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
        
        private bool IsConfig(Type type)
        {
            if (interfaceDrawer.SelectedType == null || implementationDrawer.SelectedType == null)
            {
                return false;
            }

            if (implementationDrawer.SelectedType != null)
            {
                ConfigurableAttribute configurableAttribute =
                    implementationDrawer.SelectedType.GetCustomAttribute<ConfigurableAttribute>();
                if (configurableAttribute != null)
                {
                    return configurableAttribute.configType == type;
                }
            }

            if (typeof(ModuleConfig).IsAssignableFrom(type))
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