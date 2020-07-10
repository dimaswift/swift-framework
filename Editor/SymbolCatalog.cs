using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    public class SymbolCatalog : ScriptableEditorSettings<SymbolCatalog>
    {
        public List<Symbol> list = new List<Symbol>()
        {
        };

        public void Apply()
        {
            List<string> defines = list
                .Where(x => x.style == SymbolStyle.Symbol && !string.IsNullOrEmpty(x.name) && x.Enabled)
                .Select(x => x.name)
                .Distinct()
                .ToList();

            string defineSymbols = defines.Any() ? defines.Aggregate((a, b) => a + ";" + b) : string.Empty;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defineSymbols);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, defineSymbols);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, defineSymbols);
        }

        public static bool Add(string name, string description)
        {
            Instance.Revert();
            
            foreach (Symbol symbol in Instance.list)
            {
                if (symbol.name == name)
                {
                    if (symbol.Enabled)
                    {
                        return false;
                    }
                    symbol.Enabled = true;
                    EditorUtility.SetDirty(Instance);
                    Instance.Apply();
                    return true;
                }
            }

            Instance.list.Add(new Symbol()
                {Enabled = true, name = name, description = description, style = SymbolStyle.Symbol});
            EditorUtility.SetDirty(Instance);
            Instance.Apply();
            return true;
        }

        public static bool Add(IEnumerable<(string name, string description)> symbols)
        {
            Instance.Revert();
            
            bool added = false;

            foreach (var (symbol, description) in symbols)
            {
                bool exists = false;
                foreach (Symbol existingSymbol in Instance.list)
                {
                    if (existingSymbol.name == symbol)
                    {
                        exists = true;

                        if (existingSymbol.Enabled == false)
                        {
                            added = true;
                            existingSymbol.Enabled = true;
                        }
                    }
                }

                if (exists == false)
                {
                    added = true;
                    Instance.list.Add(new Symbol()
                    {
                        Enabled = true, name = symbol, description = description,
                        style = SymbolStyle.Symbol
                    });
                }
            }

            if (!added)
            {
                return false;
            }
            
            EditorUtility.SetDirty(Instance);
            Instance.Apply();
            return true;
        }

        public static bool Disable(string name)
        {
            Instance.Revert();
            
            foreach (Symbol symbol in Instance.list)
            {
                if (symbol.name == name && symbol.Enabled)
                {
                    symbol.Enabled = false;
                    EditorUtility.SetDirty(Instance);
                    Instance.Apply();
                    return true;
                }
            }

            return false;
        }

        public static bool Disable(IEnumerable<(string name, string description)> symbols)
        {
            Instance.Revert();
            
            bool disabled = false;
            foreach (var (symbol, _) in symbols)
            {
                foreach (Symbol existingSymbol in Instance.list)
                {
                    if (existingSymbol.name == symbol && existingSymbol.Enabled)
                    {
                        existingSymbol.Enabled = false;
                        disabled = true;
                    }
                }
            }

            if (!disabled)
            {
                return false;
            }

            EditorUtility.SetDirty(Instance);
            Instance.Apply();
            return true;
        }

        public void Revert()
        {
            string define =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            IEnumerable<string> currentDefines = define.Replace(" ", "")
                .Split(';')
                .Where(x => !string.IsNullOrEmpty(x));

            list.ForEach(symbol => symbol.Enabled = currentDefines.Contains(symbol.name));

            foreach (string symbolName in currentDefines.Where(x => list.All(y => y.name != x)))
            {
                list.Add(new Symbol() {Enabled = true, name = symbolName});
            }
        }

        public enum SymbolStyle
        {
            Symbol = 1,
            Separator = 10,
            Header,
        }

        [Serializable]
        public class Symbol
        {
            public SymbolStyle style = SymbolStyle.Symbol;

            public bool Enabled { get; set; }

            public string name = "";

            public string description = "";
        }
    }

    internal class SymbolCatalogEditor : EditorWindow
    {
        private string currentDefine;
        private string focus;
        private Vector2 scrollPosition;

        private static ReorderableList ro;
        private static GUIStyle styleTitle;
        private static GUIStyle styleHeader;
        private static GUIStyle styleName;
        private static GUIStyle styleDescription;
        private static readonly Color enableStyleColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color enableTextColor = Color.white;
        private static readonly Color disableStyleColor = Color.white;
        private static readonly Color disableTextColor = new Color(1, 1, 1, 0.8f);

        private void Initialize()
        {
            if (styleDescription != null)
                return;

            styleTitle = new GUIStyle("IN BigTitle")
            {
                alignment = TextAnchor.UpperLeft, fontSize = 12, stretchWidth = true, margin = new RectOffset()
            };

            styleName = new GUIStyle(EditorStyles.label);
            styleName.active.textColor =
                styleName.normal.textColor =
                    styleName.focused.textColor =
                        styleName.hover.textColor = Color.white;

            styleDescription = new GUIStyle("HelpBox")
            {
                richText = true, padding = new RectOffset(3, 3, 5, 1), fontSize = 10
            };

            styleHeader = new GUIStyle("IN BigTitle")
            {
                richText = true,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(25, 3, 2, 2),
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };

            ro = new ReorderableList(new List<SymbolCatalog.Symbol>(), typeof(SymbolCatalog.Symbol))
            {
                drawElementCallback = DrawSymbol,
                headerHeight = 0,
                onAddDropdownCallback = (rect, list) =>
                {
                    GenericMenu gm = new GenericMenu();
                    gm.AddItem(new GUIContent("Symbol"), false, () => AddSymbol(SymbolCatalog.SymbolStyle.Symbol));
                    gm.AddItem(new GUIContent("Header"), false, () => AddSymbol(SymbolCatalog.SymbolStyle.Header));
                    gm.AddItem(new GUIContent("Separator"), false,
                        () => AddSymbol(SymbolCatalog.SymbolStyle.Separator));
                    gm.DropDown(rect);
                },
                onRemoveCallback = list => RemoveSymbol(SymbolCatalog.Instance.list[list.index]),
                onCanRemoveCallback = list => (0 <= list.index && list.index < SymbolCatalog.Instance.list.Count),
                elementHeight = 44,
                onSelectCallback = (list) => GUIUtility.keyboardControl = 0
            };

            minSize = new Vector2(300, 300);
        }
#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Define Symbols")]
#endif
        private static void OnOpenFromMenu()
        {
            GetWindow<SymbolCatalogEditor>("Symbol Catalog");
        }

        private void OnGUI()
        {
            Initialize();

            SymbolCatalog catalog = SymbolCatalog.Instance;

            string define =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (currentDefine != define)
            {
                currentDefine = define;
                catalog.Revert();
            }

            using (EditorGUILayout.ScrollViewScope svs = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = svs.scrollPosition;
                EditorGUI.BeginChangeCheck();

                GUILayout.Label(
                    new GUIContent("   Available Scripting Define Symbols",
                        EditorGUIUtility.ObjectContent(catalog, typeof(SymbolCatalog)).image), styleTitle);

                ro.list = catalog.list;
                ro.DoLayoutList();

                using (new EditorGUI.DisabledGroupScope(EditorApplication.isCompiling))
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        if (GUILayout.Button("Apply"))
                        {
                            catalog.Apply();
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(catalog);
            }

            if (EditorApplication.isCompiling)
            {
                Repaint();
            }

            if (!string.IsNullOrEmpty(focus))
            {
                EditorGUI.FocusTextInControl(focus);
                focus = null;
            }
        }

        private void AddSymbol(SymbolCatalog.SymbolStyle style)
        {
            SymbolCatalog.Symbol symbol = new SymbolCatalog.Symbol() {style = style};
            switch (style)
            {
                case SymbolCatalog.SymbolStyle.Symbol:
                    symbol.name = "SYMBOL_NAME";
                    symbol.description = "symbol description(<i>rich-text is available</i>)";
                    break;
                case SymbolCatalog.SymbolStyle.Header:
                    symbol.name = "Header(<i>rich-text is available</i>)";
                    break;
                case SymbolCatalog.SymbolStyle.Separator:
                    break;
            }

            SymbolCatalog.Instance.list.Add(symbol);

            focus = $"symbol name {SymbolCatalog.Instance.list.IndexOf(symbol)}";

            EditorUtility.SetDirty(SymbolCatalog.Instance);
        }

        private void RemoveSymbol(SymbolCatalog.Symbol symbol)
        {
            EditorApplication.delayCall += () =>
            {
                SymbolCatalog.Instance.list.Remove(symbol);
                ro.index = Mathf.Clamp(ro.index, 0, SymbolCatalog.Instance.list.Count - 1);
                EditorUtility.SetDirty(SymbolCatalog.Instance);
                Repaint();
            };
        }

        private void DrawSymbol(Rect rect, int index, bool isActive, bool isFocused)
        {
            SymbolCatalog.Symbol symbol = ro.list[index] as SymbolCatalog.Symbol;
                
            switch (symbol.style)
            {
                case SymbolCatalog.SymbolStyle.Symbol:
                    DrawDefaultSymbol(rect, symbol);
                    break;
                case SymbolCatalog.SymbolStyle.Separator:
                    GUI.Label(new Rect(rect.x + 10, rect.y + 24, rect.width - 20, 16), GUIContent.none,
                        "sv_iconselector_sep");
                    break;
                case SymbolCatalog.SymbolStyle.Header:
                    DrawHeader(rect, symbol);
                    break;
            }

            GUI.color = Color.white;
            GUI.contentColor = Color.white;
        }

        private static void DrawHeader(Rect rect, SymbolCatalog.Symbol symbol)
        {
            int index = SymbolCatalog.Instance.list.IndexOf(symbol);

            GUI.contentColor = Color.black;
            string symbolNameId = $"symbol name {index}";
            GUI.SetNextControlName(symbolNameId);
            styleHeader.richText = GUI.GetNameOfFocusedControl() != symbolNameId;
            symbol.name = GUI.TextField(new Rect(rect.x - 19, rect.y + rect.height - 24, rect.width + 23, 20),
                symbol.name, styleHeader);
            GUI.contentColor = Color.white;
        }

        private void DrawDefaultSymbol(Rect rect, SymbolCatalog.Symbol symbol)
        {
            int index = SymbolCatalog.Instance.list.IndexOf(symbol);

            string symbolDescriptionId = $"symbol description {index}";
            GUI.SetNextControlName(symbolDescriptionId);
            styleDescription.richText = GUI.GetNameOfFocusedControl() != symbolDescriptionId;
            symbol.description = GUI.TextArea(new Rect(rect.x, rect.y + 12, rect.width, rect.height - 13),
                symbol.description, styleDescription);

            GUI.color = symbol.Enabled ? enableStyleColor : disableStyleColor;
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 16), GUIContent.none,
                "ShurikenEffectBg"); 
            GUI.color = Color.white;

            symbol.Enabled = GUI.Toggle(new Rect(rect.x + 5, rect.y, 15, 16), symbol.Enabled, GUIContent.none);

            string symbolNameId = $"symbol name {index}";
            GUI.SetNextControlName(symbolNameId);
            GUI.color = symbol.Enabled ? enableTextColor : disableTextColor;
            styleName.fontStyle = GUI.GetNameOfFocusedControl() != symbolNameId ? FontStyle.Bold : FontStyle.Normal;
            symbol.name = GUI.TextField(new Rect(rect.x + 20, rect.y, rect.width - 40, 16), symbol.name, styleName);
            GUI.color = Color.white;

            if (GUI.Button(new Rect(rect.x + rect.width - 20, rect.y, 20, 20),
                EditorGUIUtility.FindTexture("treeeditor.trash"), EditorStyles.label))
            {
                RemoveSymbol(symbol);
            }
        }
    }
}