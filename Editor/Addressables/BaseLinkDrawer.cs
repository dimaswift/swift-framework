using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if USE_ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
#endif
using SwiftFramework.EditorUtils;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core.Editor
{
    internal class BaseLinkDrawer
    {
        public bool AllowSelectAndPing { get; set; } = true;

        private static readonly Stack<Object> linkSelectionHistory = new Stack<Object>();

#if USE_ADDRESSABLES
        private static readonly AddrSorter sorter = new AddrSorter();

        private class AddrSorter : IComparer<AddressableAssetEntry>
        {
            public int Compare(AddressableAssetEntry x, AddressableAssetEntry y)
            {
                if (x == null)
                {
                    return 1;
                }
                if (y == null)
                {
                    return -1;
                }
                return CompareName(x.address, y.address);
            }
        }
#endif


        private static int CompareName(string x, string y)
        {
            if (x == "None")
            {
                return -1;
            }
            if (y == "None")
            {
                return 1;
            }
            if (x == null)
            {
                return 1;
            }
            if (y == null)
            {
                return -1;
            }
            string xn = Regex.Match(x, @"\d+$", RegexOptions.RightToLeft).Value;
            string yn = Regex.Match(y, @"\d+$", RegexOptions.RightToLeft).Value;
            if (int.TryParse(xn, out int n1) && int.TryParse(yn, out int n2))
            {
                return n1.CompareTo(n2);
            }
            return string.Compare(x, y, StringComparison.InvariantCulture);
        }
#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Links/Select Previous Link %q", priority = -100)]
#endif
        private static void SelectPreviousLink()
        {
            if (linkSelectionHistory.Count == 0)
            {
                return;
            }
            Selection.activeObject = linkSelectionHistory.Pop();
        }

        protected readonly FieldInfo fieldInfo;
        protected readonly Type type;
        private readonly bool forceFlatHierarchy;

        protected BaseLinkDrawer(Type type, FieldInfo fieldInfo, bool forceFlatHierarchy = false)
        {
            this.type = type;
            this.fieldInfo = fieldInfo;
            this.forceFlatHierarchy = forceFlatHierarchy;
        }
#if USE_ADDRESSABLES
        protected readonly List<AddressableAssetEntry> assets = new List<AddressableAssetEntry>();
#else
        protected readonly List<ResourcesAssetEntry> assets = new List<ResourcesAssetEntry>();
#endif

        private string[] names = new string[0];

        protected virtual void Reload()
        {

        }

        protected virtual bool CanCreate => false;
        
        protected virtual IPromise<string> OnCreate()
        {
            return Promise<string>.Rejected(null);
        }

        private static string GetExtension(Type type)
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                return "prefab";
            }

            return "asset";
        }

        protected static string CreateAsset(Type type, Type linkType, FieldInfo fieldInfo)
        {
            LinkFolderAttribute folderAttr = linkType.GetCustomAttribute<LinkFolderAttribute>();
            string defaultFolder = folderAttr != null ? ResourcesAssetHelper.RootFolder + "/" + folderAttr.folder : ResourcesAssetHelper.RootFolder;

            if (fieldInfo != null && fieldInfo.GetCustomAttribute<LinkFolderAttribute>() != null)
            {
                defaultFolder = ResourcesAssetHelper.RootFolder + "/" + fieldInfo.GetCustomAttribute<LinkFolderAttribute>().folder;
            }
            
            string ext = GetExtension(type);

            if (Directory.Exists(defaultFolder) == false)
            {
                Directory.CreateDirectory(defaultFolder);
            }

            string path = EditorUtility.SaveFilePanelInProject("Create new " + type.Name, fieldInfo?.Name.ToUpper(), ext, "Create new " + type.Name, defaultFolder);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (ext == "prefab")
            {
                GameObject go = new GameObject(type.Name);
                go.AddComponent(type);
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }
            else
            {
                ScriptableObject so = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(so, path);
            }

            string address = null;

#if USE_ADDRESSABLES
            AddrHelper.Reload();
            
            AddressableAssetEntry entry = AddrHelper.CreateOrModifyEntry(AssetDatabase.AssetPathToGUID(path));
            address = entry.address;
#else
            address = ResourcesAssetEntry.GetAddress(path);
#endif
            AssetDatabase.Refresh();

            return address;

        }

        private static string GetLinkAddress(SerializedProperty serializedProperty)
        {
            return serializedProperty.FindPropertyRelative(Link.PathPropertyName).stringValue;
        }

        private static string GetAddressName(string address, Type assetType, FieldInfo fieldInfo = null, bool forceFlatHierarchy = false)
        {
            string rootFolder = "";

            bool flatHierarchy = false;

            Type fieldType = fieldInfo.GetChildValueType();

            if (fieldType != null)
            {
                LinkFolderAttribute folderAttr = fieldType.GetCustomAttribute<LinkFolderAttribute>();

                FlatHierarchy flatHierarchyAttr = fieldType.GetCustomAttribute<FlatHierarchy>();

                if (flatHierarchyAttr == null)
                {
                    flatHierarchyAttr = assetType.GetCustomAttribute<FlatHierarchy>();
                }

                if (flatHierarchyAttr != null)
                {
                    flatHierarchy = true;
                }

                if (folderAttr != null)
                {
                    rootFolder += folderAttr.folder;
                }
            }

            if (typeof(ModuleConfig).IsAssignableFrom(assetType))
            {
                rootFolder = $"{Folders.Configs}";
            }

            if (typeof(BehaviourModule).IsAssignableFrom(assetType))
            {
                rootFolder = $"{Folders.Modules}";
            }

            AddrSingletonAttribute singletonAttr = assetType.GetCustomAttribute<AddrSingletonAttribute>();

            if (singletonAttr != null && fieldType != null)
            {
                address = fieldType.Name + " (Singleton)";
                return address;
            }

            if (address.StartsWith(rootFolder))
            {
                address = address.Substring(rootFolder.Length, address.Length - rootFolder.Length).RemoveExtention();
            }

            if (address.StartsWith("/"))
            {
                address = address.Substring(1, address.Length - 1);
            }

            return flatHierarchy || forceFlatHierarchy ? Path.GetFileNameWithoutExtension(address) : address;
        }



        private void LoadNames()
        {
#if USE_ADDRESSABLES
            assets.Sort(sorter); 
#endif

            assets.Insert(0, null);

            names = new string[assets.Count + 1];
            names[0] = "None";

            for (int i = 1; i < assets.Count; i++)
            {
                names[i] = GetAddressName(assets[i].address, type, fieldInfo, forceFlatHierarchy);
            }
        }

        private void DoReload()
        {
            Reload();
            LoadNames();
        }

        public void Draw(Rect position, SerializedProperty property, GUIContent label = null, bool overrideCreateMethod = false)
        {
            const float buttonWidth = 50;
            
            Rect buttonRect = new Rect(position.x + position.width - buttonWidth * 2, position.y, buttonWidth, 18);
            Rect popupRect = position;

            if (assets.Count == 0)
            {

#if USE_ADDRESSABLES
                AddrHelper.OnReload -= DoReload;
                AddrHelper.OnReload += DoReload;
#else
                ResourcesAssetHelper.OnReload -= DoReload;
                ResourcesAssetHelper.OnReload += DoReload;
#endif

                DoReload();
            }

            string address = GetLinkAddress(property);

            int selectedIndex = assets.FindIndex(n => n != null && n.address == address);

            bool notFound = false;

            if (selectedIndex == -1)
            {
                if (string.IsNullOrEmpty(address) == false && address != Link.NULL)
                {
                    notFound = true;
                }
                selectedIndex = 0;
            }

            Object asset = AssetDatabase.LoadAssetAtPath(assets[selectedIndex]?.AssetPath, type);

            if (asset != null && AllowSelectAndPing)
            {
                popupRect.width -= buttonWidth * 2;
                if (GUI.Button(buttonRect, "Select"))
                {
                    linkSelectionHistory.Push(Selection.activeObject);
                    Selection.activeObject = asset;
                }

                buttonRect.x += buttonWidth;
                if (GUI.Button(buttonRect, "Ping"))
                {
                    EditorGUIUtility.PingObject(asset);
                }
            }

            if ((address == Link.NULL || string.IsNullOrEmpty(address)) && CanCreate && overrideCreateMethod == false)
            {
                popupRect.width -= buttonWidth;
                buttonRect.x += buttonWidth;
                if (GUI.Button(buttonRect, "Create"))
                {
                    OnCreate().Done(addr =>
                    {
                        if (addr != null)
                        {
                            property.FindPropertyRelative(Link.PathPropertyName).stringValue = addr;
                            property.serializedObject.ApplyModifiedProperties();
                            DoReload();
                        }
                    });
                }
            }

            int newSelectedIndex = -1;

            if (notFound)
            {
                Color color = GUI.color;
                GUI.color = EditorGUIEx.WarningRedColor;
                popupRect.width -= buttonWidth;
                if (label != null)
                {
                    EditorGUI.LabelField(new Rect(popupRect.x, popupRect.y, popupRect.width, 17), label.text, address, EditorStyles.helpBox);
                }
                GUI.color = color;
                buttonRect.x += buttonWidth;
                if (GUI.Button(buttonRect, "Reset"))
                {
                    property.FindPropertyRelative(Link.PathPropertyName).stringValue = Link.NULL;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {

                newSelectedIndex = label != null ? EditorGUI.Popup(popupRect, label.text, selectedIndex, names) : EditorGUI.Popup(popupRect, selectedIndex, names);
            }

            if (newSelectedIndex != selectedIndex && newSelectedIndex != -1)
            {
                if (assets[newSelectedIndex] != null)
                {
                    string prevPath = assets.Find(a => a != null && a.address == property.FindPropertyRelative(Link.PathPropertyName).stringValue)?.AssetPath;
                    property.FindPropertyRelative(Link.PathPropertyName).stringValue = assets[newSelectedIndex].address;
                    OnAssetChanged(prevPath, assets[newSelectedIndex].AssetPath);
                }
                else
                {
                    OnNullSelected(assets.Find(a => a != null && a.address == property.FindPropertyRelative(Link.PathPropertyName).stringValue)?.AssetPath);
                    property.FindPropertyRelative(Link.PathPropertyName).stringValue = Link.NULL;
                }
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        protected virtual void OnNullSelected(string previousAssetPath) { }

        protected  virtual void OnAssetChanged(string previousAssetPath, string newAssetPath) { }

    }

}
