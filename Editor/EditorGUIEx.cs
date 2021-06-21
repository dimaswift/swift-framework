using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Swift.EditorUtils
{
    public static class EditorGUIEx
    {
        private static GUIStyle warningStyle;

        public static readonly Color WarningRedColor = new Color(1, 0.5130347f, 0.4470588f, 1);

        public static Color GreenColor { get; } = new Color(0.4481132f, 1, 0.6813686f, 1);

        public static Color YellowColor { get; } = new Color(1f, 0.9068019f, 0.4470588f, 1);

        public static void FilePathField(SerializedProperty property, string title, string directory, string extension,
            params GUILayoutOption[] options)
        {
            FilePathField(property, new GUIContent(property.displayName), title, directory, extension, options);
        }

        private static GUIStyle boldCenteredLabel;

        public static GUIStyle BoldCenteredLabel
        {
            get
            {
                if (boldCenteredLabel == null)
                {
                    boldCenteredLabel = new GUIStyle("RL Header")
                    {
                        alignment = TextAnchor.MiddleCenter,
                        richText = true,
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        stretchWidth = true
                    };

                    boldCenteredLabel.stretchWidth = true;
                    boldCenteredLabel.stretchHeight = false;
                    boldCenteredLabel.normal.textColor = EditorStyles.label.normal.textColor;

                    boldCenteredLabel.clipping = TextClipping.Overflow;
                }

                return boldCenteredLabel;
            }
        }


        public static void DrawWarning(Rect rect, string message)
        {
            if (warningStyle == null)
            {
                warningStyle = new GUIStyle(EditorStyles.helpBox);
            }

            EditorGUI.LabelField(rect, message, warningStyle);
        }

        private static void FilePathField(SerializedProperty property, GUIContent label, string title, string directory,
            string extension, params GUILayoutOption[] options)
        {
            Rect r = GUILayoutUtility.GetRect(label, EditorStyles.textField, options);
            label = EditorGUI.BeginProperty(r, label, property);

            EditorGUI.BeginChangeCheck();
            {
                r.width -= 14;
                string newValue = EditorGUI.TextField(r, label, property.stringValue);
                if (EditorGUI.EndChangeCheck())
                    property.stringValue = newValue;
            }

            Rect rButton = new Rect(r.x + r.width - 1, r.y, 20, 17);
            if (GUI.Button(rButton, EditorGUIUtility.FindTexture("project"), EditorStyles.label))
            {
                string path = EditorUtility.OpenFilePanel(title, directory, extension);
                if (!string.IsNullOrEmpty(path))
                {
                    property.stringValue = path.Replace(Environment.CurrentDirectory + Path.DirectorySeparatorChar, "");
                }

                GUIUtility.keyboardControl = 0;
            }

            EditorGUI.EndProperty();
        }


        public static void DirectoryPathField(Rect position, SerializedProperty property, GUIContent label,
            string title, params GUILayoutOption[] options)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            {
                position.width -= 14;
                string newValue = EditorGUI.TextField(position, label, property.stringValue);
                if (EditorGUI.EndChangeCheck())
                    property.stringValue = newValue;
            }

            Rect rButton = new Rect(position.x + position.width - 1, position.y, 20, 17);
            if (GUI.Button(rButton, EditorGUIUtility.FindTexture("project"), EditorStyles.label))
            {
                string directory = 0 < property.stringValue.Length && Directory.Exists(property.stringValue)
                    ? property.stringValue
                    : "Assets/";
                string path = EditorUtility.OpenFolderPanel(title, directory, "");
                if (!string.IsNullOrEmpty(path))
                {
                    property.stringValue = path.Replace(Environment.CurrentDirectory + Path.DirectorySeparatorChar, "");
                }

                GUIUtility.keyboardControl = 0;
            }

            EditorGUI.EndProperty();
        }

        public static void TextFieldWithTemplate(SerializedProperty property, string[] displayedOptions, bool maskable,
            params GUILayoutOption[] options)
        {
            TextFieldWithTemplate(property, new GUIContent(property.displayName), displayedOptions, maskable, options);
        }

        private static void TextFieldWithTemplate(SerializedProperty property, GUIContent label,
            string[] displayedOptions, bool maskable, params GUILayoutOption[] options)
        {
            TextFieldWithTemplate(GUILayoutUtility.GetRect(label, EditorStyles.textField, options), property,
                new GUIContent(property.displayName), displayedOptions, maskable, options);
        }

        public static void TextFieldWithTemplate(Rect r, SerializedProperty property, GUIContent label,
            IEnumerable<string> displayedOptions, bool maskable, params GUILayoutOption[] options)
        {
            GUIContent content = EditorGUI.BeginProperty(r, label, property);
            if (maskable)
                content.text += " (;)";

            EditorGUI.BeginChangeCheck();
            {
                r.width -= 14;
                string newValue = EditorGUI.TextField(r, content, property.stringValue);
                if (EditorGUI.EndChangeCheck())
                    property.stringValue = newValue;
            }

            Rect rButton = new Rect(r.x + r.width + 2, r.y + 5, 14, 10);
            if (GUI.Button(rButton, EditorGUIUtility.FindTexture("icon dropdown"), EditorStyles.label))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var op in displayedOptions)
                {
                    string item = op;
                    bool active = maskable ? property.stringValue.Contains(item) : property.stringValue == item;
                    menu.AddItem(new GUIContent(item), active,
                        () =>
                        {
                            if (maskable)
                            {
                                property.stringValue =
                                    active ? property.stringValue.Replace(item, "") : property.stringValue + ";" + item;
                                property.stringValue = property.stringValue.Replace(";;", ";").Trim(';');
                            }
                            else
                            {
                                property.stringValue = item;
                            }

                            property.serializedObject.ApplyModifiedProperties();
                        });
                }


                GUIUtility.keyboardControl = 0;
                menu.DropDown(new Rect(r.x + EditorGUIUtility.labelWidth, r.y,
                    r.width - EditorGUIUtility.labelWidth + 14, r.height));
            }

            EditorGUI.EndProperty();
        }

        public class GroupScope : IDisposable
        {
            private static GUIStyle styleHeader;
            private static GUIStyle styleInner;

            private static void CacheGUI()
            {
                if (styleHeader != null)
                    return;

                styleHeader = new GUIStyle("RL Header")
                {
                    alignment = TextAnchor.MiddleLeft,
                    richText = true,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    stretchWidth = true,
                    margin = new RectOffset(4, 0, 2, 0),
                    padding = new RectOffset(6, 4, 0, 0)
                };
                styleHeader.stretchWidth = true;
                styleHeader.stretchHeight = false;
                styleHeader.normal.textColor = EditorStyles.label.normal.textColor;

                styleInner = new GUIStyle("RL Background")
                {
                    border = new RectOffset(10, 10, 1, 8),
                    margin = new RectOffset(4, 0, 0, 2),
                    padding = new RectOffset(4, 4, 3, 6),
                    clipping = TextClipping.Clip
                };
            }

            public static GUIStyle GetInnerStyle()
            {
                CacheGUI();
                return styleInner;
            }

            public static GUIStyle GetStyleHeader()
            {
                CacheGUI();
                return styleHeader;
            }

            static void SetScope(GUIContent content, params GUILayoutOption[] option)
            {
                CacheGUI();

                Rect r = GUILayoutUtility.GetRect(18, 18, styleHeader);
                GUI.Label(r, content, styleHeader);

                Color backgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.BeginVertical(styleInner, option);
                GUI.backgroundColor = backgroundColor;
            }

            public GroupScope(string text, params GUILayoutOption[] option)
            {
                SetScope(new GUIContent(text), option);
            }

            public void Dispose()
            {
                EditorGUILayout.EndVertical();
            }
        }
    }
}