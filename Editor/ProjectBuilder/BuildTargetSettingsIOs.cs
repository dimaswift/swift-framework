using System;
using UnityEditor;
using UnityEngine;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace SwiftFramework.EditorUtils
{
    [Serializable]
    internal class BuildTargetSettingsIOs : IBuildTargetSettings
    {
        public BuildTarget BuildTarget => BuildTarget.iOS;

        public Texture Icon => EditorGUIUtility.FindTexture("BuildSettings.iPhone.Small");

        [Tooltip("Enable automatically sign.")]
        public bool automaticallySign = false;

        [Tooltip("Developer Team Id.")] public string developerTeamId = "";

        [Tooltip("Code Sign Identifier.")] public string codeSignIdentity = "";

        [Tooltip("Provisioning Profile Id.\nFor example: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")]
        public string profileId = "";

        [Tooltip("Provisioning Profile Specifier.\nFor example: com company app_name")]
        public string profileSpecifier = "";

        [Tooltip("Support languages.\nIf you have multiple definitions, separate with a semicolon(;)")]
        public string languages = "jp;en";

        [Tooltip("Generate exportOptions.plist under build path for xcodebuild (XCode7 and later).")]
        public bool generateExportOptionPlist = false;

        [Tooltip(
            "The method of distribution, which can be set as any of the following:\napp-store, ad-hoc, package, enterprise, development, developer-id.")]
        public string exportMethod = "development";

        [Tooltip("Option to include Bitcode.")]
        public bool uploadBitcode = false;

        [Tooltip("Option to include symbols in the generated ipa file.")]
        public bool uploadSymbols = false;

        [Tooltip("Entitlements file(*.entitlements).")]
        public string entitlementsFile = "";

        [Tooltip("Apple services.\nIf you have multiple definitions, separate with a semicolon(;)")]
        public string services = "";

        [Tooltip("Additional frameworks.\nIf you have multiple definitions, separate with a semicolon(;)")]
        public string frameworks = "";

        private static readonly string[] availableExportMethods =
        {
            "app-store",
            "ad-hoc",
            "package",
            "enterprise",
            "development",
            "developer-id",
        };

        private static readonly string[] availableLanguages =
        {
            "ru",
            "en",
        };


        private static readonly string[] availableFrameworks =
        {
            "iAd.framework",
        };

        private static readonly string[] availableServices =
        {
            "com.apple.ApplePay",
            "com.apple.ApplicationGroups.iOS",
            "com.apple.BackgroundModes",
            "com.apple.DataProtection",
            "com.apple.GameCenter",
            "com.apple.GameControllers.appletvos",
            "com.apple.HealthKit",
            "com.apple.HomeKit",
            "com.apple.InAppPurchase",
            "com.apple.InterAppAudio",
            "com.apple.Keychain",
            "com.apple.Maps.iOS",
            "com.apple.NetworkExtensions",
            "com.apple.Push",
            "com.apple.SafariKeychain",
            "com.apple.Siri",
            "com.apple.VPNLite",
            "com.apple.WAC",
            "com.apple.Wallet",
            "com.apple.iCloud",
        };


        public void Reset()
        {
#if UNITY_5_4_OR_NEWER
            developerTeamId = PlayerSettings.iOS.appleDeveloperTeamID;
#endif
#if UNITY_5_5_OR_NEWER
            automaticallySign = PlayerSettings.iOS.appleEnableAutomaticSigning;
            profileId = PlayerSettings.iOS.iOSManualProvisioningProfileID;
#endif
        }

        public void ApplySettings(Builder builder)
        {
            PlayerSettings.iOS.buildNumber = builder.versionCode.ToString();
#if UNITY_5_4_OR_NEWER
            PlayerSettings.iOS.appleDeveloperTeamID = developerTeamId;
#endif
#if UNITY_5_5_OR_NEWER
            PlayerSettings.iOS.appleEnableAutomaticSigning = automaticallySign;
            if (!automaticallySign)
            {
                PlayerSettings.iOS.iOSManualProvisioningProfileID = profileId;
            }
#endif
        }


        public void DrawSetting(SerializedObject serializedObject)
        {
            SerializedProperty settings = serializedObject.FindProperty("iosSettings");

            using (new EditorGUIEx.GroupScope("iOS Settings"))
            {
                EditorGUILayout.LabelField("XCode Project", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                {
                    EditorGUIEx.TextFieldWithTemplate(settings.FindPropertyRelative("languages"), availableLanguages,
                        true);
                    EditorGUIEx.TextFieldWithTemplate(settings.FindPropertyRelative("frameworks"),
                        availableFrameworks, true);
                    EditorGUIEx.TextFieldWithTemplate(settings.FindPropertyRelative("services"), availableServices,
                        true);
                    EditorGUIEx.FilePathField(settings.FindPropertyRelative("entitlementsFile"),
                        "Select entitlement file.", "", "entitlements");
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField("Signing", EditorStyles.boldLabel);
                SerializedProperty spAutomaticallySign = settings.FindPropertyRelative("automaticallySign");
                EditorGUI.indentLevel++;
                {
                    EditorGUILayout.PropertyField(spAutomaticallySign);
                    EditorGUILayout.PropertyField(settings.FindPropertyRelative("developerTeamId"));
                    if (!spAutomaticallySign.boolValue)
                    {
                        EditorGUILayout.PropertyField(settings.FindPropertyRelative("codeSignIdentity"));
                        EditorGUILayout.PropertyField(settings.FindPropertyRelative("profileId"));
                        EditorGUILayout.PropertyField(settings.FindPropertyRelative("profileSpecifier"));
                    }
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField("exportOptions.plist Setting", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                {
                    SerializedProperty spGenerate = settings.FindPropertyRelative("generateExportOptionPlist");
                    EditorGUILayout.PropertyField(spGenerate, new GUIContent("Generate Automatically"));
                    if (spGenerate.boolValue)
                    {
                        EditorGUIEx.TextFieldWithTemplate(settings.FindPropertyRelative("exportMethod"),
                            availableExportMethods, false);
                        EditorGUILayout.PropertyField(settings.FindPropertyRelative("uploadBitcode"));
                        EditorGUILayout.PropertyField(settings.FindPropertyRelative("uploadSymbols"));
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}