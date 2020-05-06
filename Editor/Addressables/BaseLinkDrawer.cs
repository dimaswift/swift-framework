using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings;
using SwiftFramework.EditorUtils;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SwiftFramework.Core.Editor
{
    internal class BaseLinkDrawer
    {
        public bool AllowSelectAndPing { get; set; } = true;

        private static readonly Stack<Object> linkSelectionHistory = new Stack<Object>();

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

        public static int CompareName(string x, string y)
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
            var xn = Regex.Match(x, @"\d+$", RegexOptions.RightToLeft).Value;
            var yn = Regex.Match(y, @"\d+$", RegexOptions.RightToLeft).Value;
            if (xn != null && yn != null && int.TryParse(xn, out int n1) && int.TryParse(yn, out int n2))
            {
                return n1.CompareTo(n2);
            }
            return x.CompareTo(y);
        }

        [MenuItem("SwiftFramework/Links/Select Previous Link %q", priority = -100)]
        private static void SelectPreviousLink()
        {
            if (linkSelectionHistory.Count == 0)
            {
                return;
            }
            Selection.activeObject = linkSelectionHistory.Pop();
        }

        protected readonly FieldInfo fieldInfo;
        protected readonly System.Type type;
        protected readonly bool forceFlatHierarchy;

        public BaseLinkDrawer(System.Type type, FieldInfo fieldInfo, bool forceFlatHierarchy = false)
        {
            this.type = type;
            this.fieldInfo = fieldInfo;
            this.forceFlatHierarchy = forceFlatHierarchy;
        }

        protected List<AddressableAssetEntry> assets = new List<AddressableAssetEntry>();

        protected string[] names = new string[0];

        protected virtual void Reload()
        {

        }

        protected virtual bool CanCreate => false;

        protected virtual IPromise<string> OnCreate()
        {
            return Promise<string>.Rejected(null);
        }

        private static string GetExtention(System.Type type)
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                return "prefab";
            }

            return "asset";
        }

        public static string CreateAsset(System.Type type, System.Type linkType)
        {
            LinkFolderAttribute folderAttr = linkType.GetCustomAttribute<LinkFolderAttribute>();
            string defaultFolder = folderAttr != null ? "Assets/" + folderAttr.folder : "Assets";

            string ext = GetExtention(type);

            if (System.IO.Directory.Exists(defaultFolder) == false)
            {
                System.IO.Directory.CreateDirectory(defaultFolder);
            }

            string path = EditorUtility.SaveFilePanelInProject("Create new " + type.Name, type.Name, ext, "Create new " + type.Name, defaultFolder);

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (ext == "prefab")
            {
                GameObject go = new GameObject(type.Name);
                go.AddComponent(type);
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            else
            {
                var so = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(so, path);
            }
            AddrHelper.Reload();
            AssetDatabase.Refresh();

            var entry = AddrHelper.CreateOrModifyEntry(AssetDatabase.AssetPathToGUID(path));

            return entry.address;

        }

        private string GetLinkAddress(SerializedProperty serializedProperty)
        {
            return serializedProperty.FindPropertyRelative(Link.PathPropertyName).stringValue;
        }


        private void LoadNames()
        {
            assets.Sort(sorter); 
            assets.Insert(0, null);

            names = new string[assets.Count + 1];
            names[0] = "None";

            for (int i = 1; i < assets.Count; i++)
            {
                names[i] = AddrHelper.GetAddressName(assets[i].address, type, fieldInfo, forceFlatHierarchy);
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
                AddrHelper.OnReload -= DoReload;
                AddrHelper.OnReload += DoReload;
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
                GUI.color = EditorGUIEx.warningRedColor;
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
                    property.FindPropertyRelative(Link.PathPropertyName).stringValue = assets[newSelectedIndex].address;
                }
                else
                {
                    property.FindPropertyRelative(Link.PathPropertyName).stringValue = Link.NULL;
                }
                property.serializedObject.ApplyModifiedProperties();
            }
        }

    }

}
