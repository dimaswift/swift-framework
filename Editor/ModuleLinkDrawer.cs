using System;
using System.Collections.Generic;
using System.Reflection;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(ModuleLink), false)]
    internal class ModuleLinkDrawer : PropertyDrawer
    {
        private static event Action OnModuleImplementationChanged = () => { };

        private const string NULL = "null";

        private const float SIDE_BUTTON_WIDTH = 120;
        private const float LABEL_WIDTH = 100;
        private const float MARGIN = 5;
        private const float BUTTON_WIDTH = 100;

        private static readonly GUIContent configLabel = new GUIContent("Config");
        private static readonly GUIContent behaviourLabel = new GUIContent("Behaviour");

        private bool checkedForImplementation;

        private readonly Dictionary<string, Data> dataCache = new Dictionary<string, Data>();

        private Data GetData(SerializedProperty property)
        {
            var key = property.propertyPath + property.serializedObject.targetObject.GetInstanceID().ToString();
            if (dataCache.TryGetValue(key, out Data data) == false)
            {
                data = new Data(property);
                dataCache.Add(key, data);
            }

            if (data.property?.serializedObject == null)
            {
                dataCache.Remove(key);
                return GetData(property);
            }

            return data;
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
            public SerializedProperty typeProperty;
            public readonly SerializedProperty property;
            public readonly List<Type> implementationTypes = new List<Type>();
            public List<BehaviourModule> filteredBehaviourModules = null;
            public string[] names = new string[0];
            public LinkFilterAttribute interfaceAttribute;
            public Type selectedType;
            public AssetLinkDrawer behaviourModuleDrawer;
            public AssetLinkDrawer configDrawer;
            public float baseHeight;

            public Data(SerializedProperty property)
            {
                this.property = property;
            }

            public void ClearDeps()
            {
                unresolvedDependencies.Clear();
                unresolvedUsedModules.Clear();
                dependenciesChecked = false;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = base.GetPropertyHeight(property, label);
            Data data = GetData(property);
            data.baseHeight = h + 5;
            h += h;
            h += 15;
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

            return h;
        }


        private static bool IsDependencyResolved(Type type, Data data)
        {
            if (data.property.serializedObject.targetObject is BaseModuleManifest == false)
            {
                return true;
            }

            SerializedProperty current = data.property.serializedObject.GetIterator();

            current.Next(true);

            while (current.Next(false))
            {
                if (current.propertyType == SerializedPropertyType.Generic)
                {
                    string interfaceTypeStr = current.FindPropertyRelative("interfaceType")?.stringValue;
                    if (string.IsNullOrEmpty(interfaceTypeStr) == false)
                    {
                        Type interfaceType = Type.GetType(interfaceTypeStr);
                        string implementationTypeStr = current.FindPropertyRelative("implementationType")?.stringValue;

                        if (string.IsNullOrEmpty(implementationTypeStr))
                        {
                            continue;
                        }

                        Type implementationType = Type.GetType(implementationTypeStr);

                        if (interfaceType == type && implementationType != null)
                        {
                            return true;
                        }
                    }
                }
            }

            current.Reset();

            return false;
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
            if (data.interfaceAttribute == null)
            {
                data.interfaceAttribute = fieldInfo.GetCustomAttribute<LinkFilterAttribute>();
            }

            if (data.interfaceAttribute != null && data.implementationTypes.Count == 0 &&
                checkedForImplementation == false)
            {
                checkedForImplementation = true;
                foreach (Type type in Util.GetAllTypes())
                {
                    if (data.interfaceAttribute.interfaceType != null
                        && data.interfaceAttribute.interfaceType.IsAssignableFrom(type)
                        && type.IsInterface == false)
                    {
                        data.implementationTypes.Add(type);
                    }
                }
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

            bool moduleSelected = false;

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

                moduleSelected = true;
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

            if (moduleSelected)
            {
                OnModuleImplementationChanged();
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
#if USE_ADDRESSABLES
                    AddrHelper.Reload();
#endif
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

            data.typeProperty = property.FindPropertyRelative("implementationType");

            FindModuleImplementationTypes(data);

            OnModuleImplementationChanged -= data.ClearDeps;
            OnModuleImplementationChanged += data.ClearDeps;

            string interfaceTypeName = null;

            if (data.interfaceAttribute != null && data.interfaceAttribute.interfaceType != null)
            {
                interfaceTypeName = data.interfaceAttribute.interfaceType.Name;
                property.FindPropertyRelative("interfaceType").stringValue =
                    data.interfaceAttribute.interfaceType.AssemblyQualifiedName;
            }

            Rect titleRect = new Rect(position.x, position.y, position.width, data.baseHeight);
            GUI.Label(
                new Rect(position.x, position.y + data.baseHeight, position.width, position.height - data.baseHeight),
                "", EditorStyles.helpBox);

            Color color = GUI.color;

            GUI.Label(titleRect, $"{label.text} ({interfaceTypeName})", EditorGUIEx.GroupScope.GetStyleHeader());
            GUI.color = color;
            position.x += MARGIN;
            position.y += MARGIN;
            position.height -= MARGIN * 2;
            position.width -= MARGIN * 2;

            position.y += data.baseHeight;

            Rect labelRect = new Rect(position.x, position.y, LABEL_WIDTH, data.baseHeight);

            Rect viewPort = position;

            if (data.interfaceAttribute == null || data.interfaceAttribute.interfaceType == null)
            {
                GUI.Label(position, $"LinkFilter attribute is missing", EditorGUIEx.GroupScope.GetStyleHeader());
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

        public static void NotifyAboutModuleImplementationChange()
        {
            OnModuleImplementationChanged();
        }
    }
}