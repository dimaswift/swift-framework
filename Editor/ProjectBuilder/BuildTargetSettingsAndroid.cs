using System;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    [Serializable]
    internal class BuildTargetSettingsAndroid : IBuildTargetSettings
    {
        public BuildTarget BuildTarget => BuildTarget.Android;

        public Texture Icon => EditorGUIUtility.FindTexture("BuildSettings.Android.Small");

        [Tooltip("Keystore file path.")] public string keystoreFile = "";

        [Tooltip("Keystore password.")] public string keystorePassword = "";

        [Tooltip("Keystore alias name.")] public string keystoreAliasName = "";

        [Tooltip("Keystore alias password.")] public string keystoreAliasPassword = "";

        public void Reset()
        {
            keystoreFile = PlayerSettings.Android.keystoreName.Replace("\\", "/")
                .Replace(BuilderUtil.ProjectDir + "/", "");
            keystorePassword = PlayerSettings.Android.keystorePass;
            keystoreAliasName = PlayerSettings.Android.keyaliasName;
            keystoreAliasPassword = PlayerSettings.Android.keyaliasPass;
        }

        public void ApplySettings(Builder builder)
        {
            PlayerSettings.Android.bundleVersionCode = builder.versionCode;
            PlayerSettings.Android.useCustomKeystore = string.IsNullOrEmpty(keystoreFile) == false;
            PlayerSettings.Android.keystoreName = keystoreFile;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = keystoreAliasName;
            PlayerSettings.Android.keyaliasPass = keystoreAliasPassword;
        }

        public void DrawSetting(SerializedObject serializedObject)
        {
            SerializedProperty settings = serializedObject.FindProperty("androidSettings");

            using (new EditorGUIEx.GroupScope("Android Settings"))
            {
                EditorGUILayout.LabelField("Keystore", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                {
                    EditorGUIEx.FilePathField(settings.FindPropertyRelative("keystoreFile"), "Select keystore file.",
                        "", "");
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("keystorePassword"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("keystoreAliasName"),
                        new GUIContent("Alias"));
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("keystoreAliasPassword"),
                        new GUIContent("Alias Password"));
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}