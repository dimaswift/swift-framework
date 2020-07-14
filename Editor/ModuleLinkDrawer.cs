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
        private static float Margin => EditorGUIUtility.standardVerticalSpacing + 1;
        private const float BUTTON_WIDTH = 100;

        private static readonly GUIContent configLabel = new GUIContent("Config");
        private static readonly GUIContent behaviourLabel = new GUIContent("Behaviour");
        private static readonly List<Type> customModuleInterfaces = new List<Type>();
        private static readonly List<ModuleManifest> manifestsBuffer = new List<ModuleManifest>();
        
        private static readonly Dictionary<string, Data> cachedData = new Dictionary<string, Data>();
        
        private static Data GetData(SerializedProperty property, bool drawInterfaceType, bool drawLabel)
        {
            string id = property.serializedObject.targetObject.GetInstanceID() + property.propertyPath;
            if (cachedData.TryGetValue(id, out Data data) == false)
            {
                data = new Data(drawInterfaceType, drawLabel);
                cachedData.Add(id, data);
            }

            data.Refresh(property);
            
            return data;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        private class Data
        {
            public bool drawLabel;
            public bool drawInterfaceType;
            public string[] moduleNames;
            public ConfigurableAttribute configurable;
            public bool checkedConfigurableAttribute;
            public bool? hasValidConstructor;
            public bool dependenciesChecked;
            public readonly List<string> unresolvedDependencies = new List<string>();
            public readonly List<string> unresolvedUsedModules = new List<string>();
            public SerializedProperty typeProperty;
            public SerializedProperty interfaceTypeProperty;
            private Type interfaceType;
            public SerializedProperty property;
            public readonly List<Type> implementationTypes = new List<Type>();
            public List<BehaviourModule> filteredBehaviourModules = null;
            public string[] names = new string[0];
            public Type selectedType;
            public AssetLinkDrawer behaviourModuleDrawer;
            public AssetLinkDrawer configDrawer;
            public float baseHeight;
            
            public Type InterfaceType
            {
                get => interfaceType;
                set
                {
                    if (value == interfaceType)
                    {
                        return;
                    }

                    filteredBehaviourModules = null;
                    behaviourModuleDrawer = null;
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

            public Data(bool drawInterfaceType, bool drawLabel)
            {
                this.drawInterfaceType = drawInterfaceType;
                this.drawLabel = drawLabel;
            }

            public void Refresh(SerializedProperty property)
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
            return GetPropertyHeight(base.GetPropertyHeight(property, label), property);
        }

        public static float GetPropertyHeight(float baseHeight, SerializedProperty property)
        {
            float height = baseHeight;
            
            Data data = GetData(property, true, true);
            
            data.baseHeight = height;
            
            height += Margin * 2;

            if (data.drawLabel)
            {
                height += EditorGUIUtility.singleLineHeight + Margin;
            }
            
            if (IsConfigurable(data))
            {
                height += data.baseHeight + Margin;
            }

            if (IsBehaviourModule(data))
            {
                height += data.baseHeight + Margin;
            }

            if (data.selectedType != null)
            {
                height += (data.baseHeight + Margin * 2) * data.unresolvedDependencies.Count;
            
                height += (data.baseHeight + Margin * 2) * data.unresolvedUsedModules.Count;
            }
            
            if (data.drawInterfaceType)
            {
                height += EditorGUIUtility.singleLineHeight + Margin;
            }
            
            return height;
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
        
        private static bool IsConfigurable(Data data)
        {
            return data.selectedType != null && data.configurable != null;
        }

        private static bool IsBehaviourModule(Data data)
        {
            var isBehaviourModule = data.selectedType != null
                                    && Util.IsDerivedFrom(data.selectedType, typeof(BehaviourModule))
                                    && data.selectedType
                                        .GetCustomAttribute<DisallowCustomModuleBehavioursAttribute>() == null;

            return isBehaviourModule;
        }

        private static void FindModuleImplementationTypes(Data data)
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

            position.width = BUTTON_WIDTH;
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.HelpBox(position, $"Type not found!", MessageType.Error);
            position.x += BUTTON_WIDTH;
            position.width = viewport.width - BUTTON_WIDTH;
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
            position.width = viewport.width;
            position.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            EditorGUI.HelpBox(position, $"Module already defined in another manifest", MessageType.Error);
            position.x += BUTTON_WIDTH * 2;
            position.width = viewport.width - BUTTON_WIDTH * 2;
            position.x = viewport.x + (viewport.width - BUTTON_WIDTH);
            position.width = BUTTON_WIDTH;
            float buttonWidth = 50;
            if (GUI.Button(new Rect(position.x + position.width - buttonWidth - EditorGUIUtility.standardVerticalSpacing,
                position.y + EditorGUIUtility.standardVerticalSpacing, 
                buttonWidth, 
                EditorGUIUtility.singleLineHeight), "Ping"))
            {
                EditorGUIUtility.PingObject(manifest);
            }
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
                }
                data.typeProperty.serializedObject.ApplyModifiedProperties();
                ClearCache(data.property);
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

            void DrawPingButton(Rect pos, string dependencyName)
            {
                Rect warningRect = new Rect(pos.x, pos.y, pos.width, baseHeight);
                float buttonWidth = 60;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                if (GUI.Button(
                    new Rect(warningRect.x + warningRect.width - buttonWidth - spacing,
                        warningRect.y, buttonWidth, warningRect.height - spacing), "Resolve"))
                {
                    Type depType = new List<Type>(Util.GetAllTypes()).Find(i => i.Name == dependencyName);
                    ModuleInstaller.Install(depType);
                    ClearCache(data.property);
                }
            }

            if (data.selectedType != null)
            {
                foreach (string dep in data.unresolvedDependencies)
                {
                    position.y += baseHeight + Margin;
                    Rect warningRect = new Rect(position.x, position.y, position.width, baseHeight + Margin);
                    EditorGUI.HelpBox(warningRect, $"Depends on {dep}. Implementation not found!", MessageType.Error);
                    position.y += Margin;
                    DrawPingButton(position, dep);
                }

                foreach (string dep in data.unresolvedUsedModules)
                {
                    position.y += baseHeight + Margin;
                    Rect warningRect = new Rect(position.x, position.y, position.width, baseHeight + Margin);
                    EditorGUI.HelpBox(warningRect, $"Uses {dep} module. Implementation not found!", MessageType.Warning);
                    position.y += Margin;
                    DrawPingButton(position, dep);
                }
            }
            
            position.y += Margin;
        }

        private static void DrawBehaviourModulePopUp(ref Rect position, float baseHeight, Data data)
        {
            if (data.filteredBehaviourModules == null)
            {
                data.filteredBehaviourModules = new List<BehaviourModule>();
                foreach (GameObject module in Util.GetAssets<GameObject>())
                {
                    if (module.GetComponent(data.selectedType) != null)
                    {
                        data.filteredBehaviourModules.Add(module.GetComponent<BehaviourModule>());
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

            position.y += Margin;
        }

        
        
        private static void DrawConfigPopUp(ref Rect position, float baseHeight, Data data)
        {
            if (data.checkedConfigurableAttribute == false)
            {
                data.configurable = data.selectedType.GetCustomAttribute<ConfigurableAttribute>();
                data.checkedConfigurableAttribute = true;
            }

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

        public static void Draw(Rect position, SerializedProperty property, GUIContent label, bool drawInterfaceField, bool drawLabel)
        {
            Data data = GetData(property, drawInterfaceField, drawLabel);
            data.drawInterfaceType = drawInterfaceField;
            data.drawLabel = drawLabel;
            
            string interfaceTypeName = data.property.FindPropertyRelative("interfaceType").stringValue;

            FindModuleImplementationTypes(data);

            AssetsUtil.OnAssetsPostProcessed -= OnAssetsPostProcessed;
            AssetsUtil.OnAssetsPostProcessed += OnAssetsPostProcessed;
            
            Rect titleRect = new Rect(position.x, position.y, position.width, data.baseHeight);
            
            GUI.Label(
                new Rect(position.x, position.y, position.width, position.height),
                "", EditorStyles.helpBox);
            
            
            Color color = GUI.color;

            Type interfaceType = string.IsNullOrEmpty(interfaceTypeName) ? null : Type.GetType(interfaceTypeName);

            if (data.InterfaceType != interfaceType)
            {
                ClearCache(property);
                data = GetData(property, drawInterfaceField, drawLabel);
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
                        data.interfaceTypeProperty.stringValue = null;
                        data.interfaceTypeProperty.serializedObject.ApplyModifiedProperties();
                    }
                    
                    return;
                }
                
            }
            else
            {
                if (drawLabel)
                {
                    GUI.Label(titleRect, $"{label.text} ({interfaceType.Name})", EditorGUIEx.GroupScope.GetStyleHeader());
                }
            }
            
            GUI.color = color;
            position.x += Margin;
            position.y += Margin;
            position.height -= Margin * 2;
            position.width -= Margin * 2;
            
            if (data.moduleNames == null)
            {
                customModuleInterfaces.Clear();
                customModuleInterfaces.AddRange(Util.GetModuleInterfaces());
                List<string> names = new List<string>(customModuleInterfaces.Select(s => s.Name));
                names.Insert(0, "None");
                data.moduleNames = names.ToArray();
            }
            
            if (drawInterfaceField)
            {
                position.y += data.baseHeight + Margin;
                position.height = data.baseHeight;
                
                int prevSelectedInterfaceIndex =
                    customModuleInterfaces.FindIndex(m => m.AssemblyQualifiedName == interfaceTypeName) + 1;

                int newSelectedInterfaceIndex = EditorGUI.Popup(position, prevSelectedInterfaceIndex, data.moduleNames);
            
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
                    ClearCache(property);
                    return;
                }
                
                position.y += Margin;

            }

            if (data.drawLabel)
            {
                position.y += data.baseHeight;
            }
            
            Rect viewPort = position;
            
            if (interfaceType != null)
            {
                if (manifestsBuffer.Count == 0)
                {
                    manifestsBuffer.AddRange(Util.GetAssets<ModuleManifest>());
                }
                foreach (ModuleManifest otherManifest in manifestsBuffer)
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

        private static void ClearCache(SerializedProperty property)
        {
            string id = property.serializedObject.targetObject.GetInstanceID() + property.propertyPath;
            if (cachedData.ContainsKey(id))
            {
                cachedData.Remove(id);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, true, true);
        }

        private static void OnAssetsPostProcessed()
        {
            cachedData.Clear();
            manifestsBuffer.Clear();
        }
    }
}