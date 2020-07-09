using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(ModuleLink), false)]
    internal class ModuleLinkDrawer : PropertyDrawer
    {
        private const string NULL = "null";

        private const float SIDE_BUTTON_WIDTH = 120;
        private const float LABEL_WIDTH = 100;
        private const float MARGIN = 5;
        private const float BUTTON_WIDTH = 100;

        private static readonly GUIContent configLabel = new GUIContent("Config");
        private static readonly GUIContent behaviourLabel = new GUIContent("Behaviour");
        private static readonly List<Type> customModuleInterfaces = new List<Type>();
        
        private Data cachedData;
        private string[] moduleNames;

        private Data GetData(SerializedProperty property)
        {
            if (cachedData == null)
            {
                cachedData = new Data(property);
            }
            return cachedData;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        private class Data
        {
            public ConfigurableAttribute configurable;
            public bool? hasValidConstructor;
            public bool dependenciesChecked;
            public readonly List<string> unresolvedDependencies = new List<string>();
            public readonly List<string> unresolvedUsedModules = new List<string>();
            public readonly SerializedProperty typeProperty;
            public readonly SerializedProperty interfaceTypeProperty;

            public readonly SerializedProperty property;
            public readonly List<Type> implementationTypes = new List<Type>();
            public List<BehaviourModule> filteredBehaviourModules = null;
            public string[] names = new string[0];
            public Type selectedType;
            public AssetLinkDrawer behaviourModuleDrawer;
            public AssetLinkDrawer configDrawer;

            public Type InterfaceType
            {
                get => interfaceType;
                set
                {
                    implementationTypes.Clear();
                    interfaceType = value;
                    if (value == null)
                    {
                        return;
                    }
                    
                    foreach (Type type in Util.GetAllTypes())
                    {
                        if (value.IsAssignableFrom(type) && type.IsInterface == false)
                        {
                            implementationTypes.Add(type);
                        }
                    }
                    
                 
                }
            }
            public float baseHeight;
            private Type interfaceType;
 
            public Data(SerializedProperty property)
            {
                this.property = property;
                typeProperty = property.FindPropertyRelative("implementationType");
                interfaceTypeProperty = property.FindPropertyRelative("interfaceType");
                if (string.IsNullOrEmpty(interfaceTypeProperty.stringValue) == false)
                {
                    InterfaceType = Type.GetType(interfaceTypeProperty.stringValue);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = base.GetPropertyHeight(property, label);
            Data data = GetData(property);
            data.baseHeight = h + 5;
            h += h;
            h += 25;
            if (IsConfigurable(data))
            {
                h += data.baseHeight;
            }

            if (IsBehaviourModule(data))
            {
                h += data.baseHeight;
            }

            h += (data.baseHeight + MARGIN) * data.unresolvedDependencies.Count;
            h += (data.baseHeight + MARGIN) * data.unresolvedUsedModules.Count;
            h += data.baseHeight + MARGIN;
            return h;
        }


        private static bool IsDependencyResolved(Type type, Data data)
        {
            foreach (ModuleManifest moduleManifest in Util.GetModuleManifests())
            {
                if (moduleManifest.InterfaceType == type && moduleManifest.ImplementationType != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void UndoRedoPerformed()
        {
            cachedData = null;
        }

        private static bool IsConfigurable(Data data)
        {
            return data.selectedType != null && data.selectedType.GetCustomAttribute<ConfigurableAttribute>() != null;
        }

        private static bool IsBehaviourModule(Data data)
        {
            var isBehaviourModule = data.selectedType != null
                                    && Util.IsDerivedFrom(data.selectedType, typeof(BehaviourModule))
                                    && data.selectedType
                                        .GetCustomAttribute<DisallowCustomModuleBehavioursAttribute>() == null;

            return isBehaviourModule;
        }

        private void FindModuleImplementationTypes(Data data)
        {
            string interfaceTypeStr = data.property.FindPropertyRelative("interfaceType").stringValue;
            if (string.IsNullOrEmpty(interfaceTypeStr))
            {
                return;
            }

            Type interfaceType = Type.GetType(interfaceTypeStr);
            
            if (interfaceType == null)
            {
                return;
            }

            if (data.names.Length != data.implementationTypes.Count + 1)
            {
                data.names = new string[data.implementationTypes.Count + 1];
                data.names[0] = "None";
                for (int i = 1; i < data.names.Length; i++)
                {
                    data.names[i] = data.implementationTypes[i - 1].Name;
                }
            }
        }

        private static void DrawTypeNotFound(ref Rect position, Rect viewport, float baseHeight, Data data)
        {
            position.width -= BUTTON_WIDTH;
            string typeName = data.typeProperty.stringValue.Split(',')[0];

            position.width = BUTTON_WIDTH * 2;
            position.height = 16;
            EditorGUI.HelpBox(position, $"Type not found!", MessageType.Error);
            position.x += BUTTON_WIDTH * 2;
            position.width = viewport.width - BUTTON_WIDTH * 2;
            EditorGUI.LabelField(position, typeName);

            position.x = viewport.x + (viewport.width - BUTTON_WIDTH);
            position.width = BUTTON_WIDTH;
            if (GUI.Button(position, "Reset"))
            {
                data.typeProperty.stringValue = NULL;
                data.typeProperty.serializedObject.ApplyModifiedProperties();
            }
        }
        
        private static void DrawDuplicateModule(ref Rect position, Rect viewport, ModuleManifest manifest)
        {
            position.width -= BUTTON_WIDTH;
            position.width = viewport.width;
            position.height = 33;
            EditorGUI.HelpBox(position, $"Module already defined in manifest: {AssetDatabase.GetAssetPath(manifest)}", MessageType.Error);
            position.x += BUTTON_WIDTH * 2;
            position.width = viewport.width - BUTTON_WIDTH * 2;
            position.x = viewport.x + (viewport.width - BUTTON_WIDTH);
            position.width = BUTTON_WIDTH;
        }

        private static void DrawImplementationPopUp(ref Rect position, Rect viewPort, float baseHeight, Data data)
        {
            string type = data.typeProperty.stringValue;

            int selectedTypeIndex = data.implementationTypes.FindIndex(t => t.AssemblyQualifiedName == type);

            if (selectedTypeIndex == -1 && type != NULL && string.IsNullOrEmpty(type) == false)
            {
                DrawTypeNotFound(ref position, viewPort, baseHeight, data);
                return;
            }

            int newIndex = EditorGUI.Popup(new Rect(position.x, position.y, position.width, baseHeight),
                "Implementation", selectedTypeIndex + 1, data.names);

            if (newIndex != selectedTypeIndex + 1)
            {
                if (newIndex <= 0 && selectedTypeIndex != -1)
                {
                    data.typeProperty.stringValue = NULL;
                }
                else if (data.implementationTypes.Count > newIndex - 1)
                {
                    data.typeProperty.stringValue = data.implementationTypes[newIndex - 1].AssemblyQualifiedName;
                    data.typeProperty.serializedObject.ApplyModifiedProperties();
                }
            }

            data.selectedType = selectedTypeIndex != -1 ? data.implementationTypes[selectedTypeIndex] : null;

            if (data.dependenciesChecked == false)
            {
                foreach (Type depType in Module.GetDependenciesForType(data.selectedType))
                {
                    if (IsDependencyResolved(depType, data) == false)
                    {
                        data.unresolvedDependencies.Add(depType.Name);
                    }
                }

                foreach (Type depType in Module.GetOtherUsedModules(data.selectedType))
                {
                    if (IsDependencyResolved(depType, data) == false)
                    {
                        data.unresolvedUsedModules.Add(depType.Name);
                    }
                }

                data.dependenciesChecked = true;
            }

            foreach (string dep in data.unresolvedDependencies)
            {
                position.y += baseHeight;
                Rect warningRect = new Rect(position.x, position.y, position.width, baseHeight);
                EditorGUI.HelpBox(warningRect, $"Depends on {dep}. Implementation not found!", MessageType.Error);
                position.y += MARGIN;
            }

            foreach (string dep in data.unresolvedUsedModules)
            {
                position.y += baseHeight;
                Rect warningRect = new Rect(position.x, position.y, position.width, baseHeight);
                EditorGUI.HelpBox(warningRect, $"Uses {dep} module. Implementation not found!", MessageType.Warning);
                position.y += MARGIN;
            }
        }

        private void DrawBehaviourModulePopUp(ref Rect position, float baseHeight, Data data)
        {
            if (data.filteredBehaviourModules == null)
            {
                data.filteredBehaviourModules = new List<BehaviourModule>();
                foreach (BehaviourModule module in Util.GetAssets<BehaviourModule>())
                {
                    if (module.GetComponent(data.selectedType) != null)
                    {
                        data.filteredBehaviourModules.Add(module);
                    }
                }
            }

            if (data.behaviourModuleDrawer == null)
            {
                data.behaviourModuleDrawer = new AssetLinkDrawer(data.selectedType, null, true);
            }

            position.y += baseHeight;

            Color defaultColor = GUI.color;


            SerializedProperty behaviourLinkProperty = data.property.FindPropertyRelative("behaviourLink");

            if (behaviourLinkProperty.FindPropertyRelative("Path").stringValue == Link.NULL)
            {
                GUI.color = GetRedErrorColor();
            }

            GUI.color = defaultColor;

            BehaviourModuleLink behaviourModuleLink = behaviourLinkProperty.ToLink<BehaviourModuleLink>();
            var width = position.width;
            if (behaviourModuleLink.HasValue == false)
            {
                width -= SIDE_BUTTON_WIDTH;
                if (GUI.Button(new Rect(position.x + width, position.y, SIDE_BUTTON_WIDTH, 18), "Create Prefab"))
                {
                    Util.CreateModuleBehaviour(data.selectedType, behaviourLinkProperty);
                }
            }

            data.behaviourModuleDrawer.Draw(new Rect(position.x, position.y, width, baseHeight), behaviourLinkProperty,
                behaviourLabel, true);
        }

        
        
        private void DrawConfigPopUp(ref Rect position, float baseHeight, Data data)
        {
            data.configurable = data.selectedType.GetCustomAttribute<ConfigurableAttribute>();

            if (data.configurable == null)
            {
                return;
            }

            if (data.configDrawer == null)
            {
                data.configDrawer = new AssetLinkDrawer(data.configurable.configType, null, true);
            }

            position.y += baseHeight;

            if (!data.hasValidConstructor.HasValue)
            {
                if (IsBehaviourModule(data) == false)
                {
                    foreach (var c in data.selectedType.GetConstructors())
                    {
                        int amount = 0;
                        foreach (var p in c.GetParameters())
                        {
                            amount++;
                            if (p.ParameterType == typeof(ModuleConfigLink) && c.IsPublic)
                            {
                                data.hasValidConstructor = true;
                                break;
                            }
                        }

                        if (amount > 1)
                        {
                            data.hasValidConstructor = false;
                        }
                    }
                }
            }

            if (data.hasValidConstructor != null && (IsBehaviourModule(data) == false && data.hasValidConstructor.Value == false))
            {
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, baseHeight - 2),
                    $"Add public constructor with ModuleConfigLink!", MessageType.Error);
                return;
            }

            SerializedProperty configProperty = data.property.FindPropertyRelative("configLink");

            Color defaultColor = GUI.color;

            if (configProperty.FindPropertyRelative("Path").stringValue == Link.NULL)
            {
                GUI.color = GetRedErrorColor();
            }

            GUI.color = defaultColor;

            if (configProperty.HasLinkValue<ModuleConfigLink>() == false)
            {
                position.width -= SIDE_BUTTON_WIDTH;
                if (GUI.Button(new Rect(position.x + position.width, position.y, SIDE_BUTTON_WIDTH, 18), "Create Config"))
                {
                    Util.CreateModuleConfig(data.configurable.configType, configProperty);

                }
            }

            data.configDrawer.Draw(new Rect(position.x, position.y, position.width, baseHeight), configProperty,
                configLabel, true);
        }

        private static Color GetRedErrorColor()
        {
            return EditorGUIEx.WarningRedColor;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Data data = GetData(property);

            string interfaceTypeName = data.property.FindPropertyRelative("interfaceType").stringValue;

            FindModuleImplementationTypes(data);

            Rect titleRect = new Rect(position.x, position.y, position.width, data.baseHeight);
            GUI.Label(
                new Rect(position.x, position.y + data.baseHeight, position.width, position.height - data.baseHeight),
                "", EditorStyles.helpBox);

            Color color = GUI.color;

            Type interfaceType = string.IsNullOrEmpty(interfaceTypeName) ? null : Type.GetType(interfaceTypeName);

            if (data.InterfaceType != interfaceType)
            {
                cachedData = null;
                data = GetData(property);
            }
            
            if (interfaceType == null)
            {
                GUI.Label(titleRect, "None", EditorGUIEx.GroupScope.GetStyleHeader());

                if (string.IsNullOrEmpty(interfaceTypeName) == false)
                {
                    titleRect.y += data.baseHeight;
                    GUI.color = GetRedErrorColor();
                    GUI.Label(titleRect, $"Type '{interfaceTypeName.Split(',')[0]}' not found!", EditorGUIEx.GroupScope.GetStyleHeader());
                    GUI.color = color;
                    
                    titleRect.y += data.baseHeight;

                    if (GUI.Button(titleRect, "Reset"))
                    {
                        cachedData.interfaceTypeProperty.stringValue = null;
                        cachedData.interfaceTypeProperty.serializedObject.ApplyModifiedProperties();
                    }
                    
                    return;
                }
                
            }
            else
            {
                GUI.Label(titleRect, $"{label.text} ({interfaceType.Name})", EditorGUIEx.GroupScope.GetStyleHeader());
            }
            
       
            GUI.color = color;
            position.x += MARGIN;
            position.y += MARGIN;
            position.height -= MARGIN * 2;
            position.width -= MARGIN * 2;

            position.y += data.baseHeight;

            if (moduleNames == null)
            {
                customModuleInterfaces.Clear();
                ModuleManifest manifest = property.serializedObject.targetObject as ModuleManifest;
                string group = ModuleGroups.Custom;
                if (manifest != null)
                {
                    group = manifest.ModuleGroup;
                }
                customModuleInterfaces.AddRange(Util.GetModuleInterfaces(group));
                List<string> names = new List<string>(customModuleInterfaces.Select(s => s.Name));
                names.Insert(0, "None");
                moduleNames = names.ToArray();
            }
            
            position.height = data.baseHeight;

            int prevSelectedInterfaceIndex =
                customModuleInterfaces.FindIndex(m => m.AssemblyQualifiedName == interfaceTypeName) + 1;

            int newSelectedInterfaceIndex = EditorGUI.Popup(position, prevSelectedInterfaceIndex, moduleNames);
            
            if (newSelectedInterfaceIndex != prevSelectedInterfaceIndex)
            {
                if (newSelectedInterfaceIndex == 0)
                {
                    data.property.FindPropertyRelative("interfaceType").stringValue = null;
                    data.property.FindPropertyRelative("configLink").FindPropertyRelative("Path").stringValue = null;
                    data.property.FindPropertyRelative("behaviourLink").FindPropertyRelative("Path").stringValue = null;
                    data.InterfaceType = null;
                }
                else
                {
                    interfaceTypeName = customModuleInterfaces[newSelectedInterfaceIndex - 1].AssemblyQualifiedName;
                    data.property.FindPropertyRelative("interfaceType").stringValue = interfaceTypeName;
                    data.InterfaceType = Type.GetType(interfaceTypeName);
                }

                data.typeProperty.stringValue = null;
                data.typeProperty.serializedObject.ApplyModifiedProperties();
                cachedData = null;
                return;
            }


            position.y += data.baseHeight;
            
            Rect labelRect = new Rect(position.x, position.y, LABEL_WIDTH, data.baseHeight);

            Rect viewPort = position;
            
            
            if (interfaceType != null)
            {
                foreach (ModuleManifest otherManifest in Util.GetModuleManifests())
                {
                    if (interfaceType == otherManifest.InterfaceType)
                    {
                        ModuleManifest thisManifest = property.serializedObject.targetObject as ModuleManifest;
                        if (thisManifest != null && thisManifest != otherManifest)
                        {
                            if (otherManifest.State == ModuleState.Enabled && thisManifest.State == ModuleState.Enabled)
                            {
                                DrawDuplicateModule(ref position, viewPort, otherManifest);
                                return;
                            }
                        }
                    }
                }
            }
            
            if (string.IsNullOrEmpty(interfaceTypeName))
            {
                GUI.Label(position, $"Module Interface not selected", EditorGUIEx.GroupScope.GetStyleHeader());

                return;
            } 

            labelRect.y = position.y;

            DrawImplementationPopUp(ref position, viewPort, data.baseHeight, data);

            if (data.selectedType != null)
            {
                if (IsBehaviourModule(data))
                {
                    DrawBehaviourModulePopUp(ref position, data.baseHeight, data);
                }

                DrawConfigPopUp(ref position, data.baseHeight, data);
            }
        }
    }
}