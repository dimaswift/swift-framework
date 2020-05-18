using System;
using System.Collections.Generic;
using System.Reflection;
using SwiftFramework.Core;
using SwiftFramework.Core.Editor;
using SwiftFramework.Core.Pooling;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Editor
{
    [CustomEditor(typeof(BaseModuleManifest), true)]
    internal class ModuleManifestEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, ModuleGroupContainer> moduleGroups = new Dictionary<string, ModuleGroupContainer>();

        private readonly SimplePool<ModuleGroupContainer> groupsPool =
            new SimplePool<ModuleGroupContainer>(() => new ModuleGroupContainer());

        public override void OnInspectorGUI()
        {
            if (moduleGroups.Count == 0)
            {
                Util.OnScriptsReloaded -= Util_OnScriptsReloaded;
                Util.OnScriptsReloaded += Util_OnScriptsReloaded;

                SerializedProperty iterator = serializedObject.GetIterator();

                iterator.NextVisible(true);


                while (iterator.NextVisible(false))
                {
                    string interfaceTypeStr = iterator.FindPropertyRelative("interfaceType")?.stringValue;

                    if (interfaceTypeStr == null)
                    {
                        break;
                    }
                    
                    Type type = Type.GetType(interfaceTypeStr);
                    if (type != null)
                    {
                        ModuleGroupAttribute moduleGroup = type.GetCustomAttribute<ModuleGroupAttribute>();

                        if (moduleGroup != null && string.IsNullOrEmpty(moduleGroup.GroupId) == false)
                        {
                            AddModuleToGroup(moduleGroup.GroupId, iterator.propertyPath);
                        }
                        else
                        {
                            AddModuleToGroup(ModuleGroups.Custom, iterator.propertyPath);
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(iterator);
                    }
                }

                iterator.Reset();
            }

            EditorGUILayout.Space();
            foreach (var groupContainer in moduleGroups)
            {
                ModuleGroupContainer container = groupContainer.Value;
                container.Shown = EditorGUILayout.BeginFoldoutHeaderGroup(container.Shown, groupContainer.Key);
                EditorGUILayout.Space();
                if (container.Shown)
                {
                    foreach (string customProp in container.moduleProperties)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(customProp));
                    }

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Find Default Implementations"))
                    {
                        FindDefaultModuleGroupImplementations(container.id);
                        return;
                    }

                    if (GUILayout.Button("Regenerate"))
                    {
                        BootEditor.GenerateModuleManifest();
                        return;
                    }

                    if (GUILayout.Button("Refresh"))
                    {
                        ModuleLinkDrawer.NotifyAboutModuleImplementationChange();
                        moduleGroups.Clear();
                        return;
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void Util_OnScriptsReloaded()
        {
            moduleGroups.Clear();
        }

        private void FindDefaultModuleGroupImplementations(string groupId)
        {
            if (moduleGroups.TryGetValue(groupId, out ModuleGroupContainer coreModules))
            {
                foreach (string moduleProp in coreModules.moduleProperties)
                {
                    SerializedProperty prop = serializedObject.FindProperty(moduleProp);
                    SerializedProperty interfaceProp = prop?.FindPropertyRelative("interfaceType");
                    if (interfaceProp != null && string.IsNullOrEmpty(interfaceProp.stringValue) == false)
                    {
                        Type interfaceType = Type.GetType(interfaceProp.stringValue);
                        if (interfaceType != null)
                        {
                            Type implementationType =
                                RuntimeModuleFactory.FindDefaultModuleImplementation(interfaceType);
                            if (implementationType != null)
                            {
                                prop.FindPropertyRelative("implementationType").stringValue =
                                    implementationType.AssemblyQualifiedName;
                                ModuleLinkDrawer.NotifyAboutModuleImplementationChange();
                            }
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        private void AddModuleToGroup(string groupId, string modulePropPath)
        {
            if (moduleGroups.TryGetValue(groupId, out ModuleGroupContainer groupContainer))
            {
                groupContainer.moduleProperties.Add(modulePropPath);
            }
            else
            {
                groupContainer = groupsPool.Take();
                groupContainer.id = groupId;
                groupContainer.moduleProperties.Add(modulePropPath);
                moduleGroups.Add(groupId, groupContainer);
            }
        }

        private class ModuleGroupContainer : BasePooled, IPooled
        {
            public bool Shown
            {
                set
                {
                    if (shown != null && value != shown.Value)
                    {
                        EditorPrefs.SetBool(ShownPrefsId, value);
                    }

                    shown = value;
                }
                get
                {
                    if (shown.HasValue == false)
                    {
                        shown = EditorPrefs.GetBool(ShownPrefsId, false);
                    }

                    return shown.Value;
                }
            }

            private string ShownPrefsId => $"show_module_group_{id}";

            public string id;
            public readonly List<string> moduleProperties = new List<string>();
            private bool? shown;

            public override void Dispose()
            {
                moduleProperties.Clear();
            }

            protected override void OnTakenFromPool()
            {
                base.OnTakenFromPool();

                moduleProperties.Clear();
            }
        }
    }
}