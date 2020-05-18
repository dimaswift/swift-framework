using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SwiftFramework.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    internal sealed class Builder : ScriptableObject
    {
        public ModuleManifestLink manifest = null;

        public const string K_LOG_TYPE = "#### [Builder] ";

        public BuildTarget buildTarget = BuildTarget.Android;

        public BuildTarget ActualBuildTarget => buildTarget;

        private BuildTargetGroup BuildTargetGroup => BuildPipeline.GetBuildTargetGroup(ActualBuildTarget);

        public string productName;

        public string companyName;

        public string applicationIdentifier;

        public bool buildAppBundle = false;

        public string BuildName
        {
            get
            {
                if (ActualBuildTarget == BuildTarget.Android && !EditorUserBuildSettings.exportAsGoogleAndroidProject)
                {
                    if (EditorUserBuildSettings.buildAppBundle || buildAppBundle)
                    {
                        return $"{name}_{version}.{versionCode}.aab";
                    }
                    else
                    {
                        return $"{name}_{version}.{versionCode}.apk";
                    }
                }

                else
                    return "build";
            }
        }

        public string OutputFolderPath
        {
            get => EditorPrefs.GetString(name + "_outputFolderPath");
            private set => EditorPrefs.SetString(name + "_outputFolderPath", value);
        }

        public string version = "0.1";

        public Texture2D icon = null;

        public Texture2D defaultIconToOverwrite = null;

        public int versionCode = 0;

        public bool developmentBuild = false;

        public SceneSetting[] scenes = new SceneSetting[] { };

        public bool showUnitySplashScreen = false;

        public bool showSplashScreen = true;

        public BuildTargetSettingsIOs iosSettings = new BuildTargetSettingsIOs();
        public BuildTargetSettingsAndroid androidSettings = new BuildTargetSettingsAndroid();

        [ContextMenu("Save Json")]
        public void SaveJson()
        {
            string path = EditorUtility.SaveFilePanel(name, Application.dataPath, name, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            File.WriteAllText(path, JsonUtility.ToJson(this, true));
        }

        [Serializable]
        public class SceneSetting
        {
            public bool enable = true;
            public string name = null;
        }

        private static void OnApplySetting()
        {
        }

        private string GetOutputFolder()
        {
            return Path.Combine(OutputFolderPath, BuildName);
        }

        public void Reset()
        {
            buildTarget = EditorUserBuildSettings.activeBuildTarget;
            productName = PlayerSettings.productName;
            companyName = PlayerSettings.companyName;
            applicationIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup);

            version = PlayerSettings.bundleVersion;

            androidSettings.Reset();
            iosSettings.Reset();
        }

        public static void BuildAddressableAssets()
        {
#if USE_ADDRESSABLES
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent();
#else
            Debug.LogError($"Addressables are disabled");
#endif
        }

        public void ApplySettings()
        {
            PlayerSettings.SplashScreen.showUnityLogo = showUnitySplashScreen;
            PlayerSettings.SplashScreen.show = showSplashScreen;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup, applicationIdentifier);
            PlayerSettings.productName = productName;
            PlayerSettings.companyName = companyName;

            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.allowDebugging = developmentBuild;

            PlayerSettings.bundleVersion = version;
            if (developmentBuild &&
                BuilderUtil.ExecuteArguments.TryGetValue(BuilderUtil.OPT_DEV_BUILD_NUM, out string buildNumber) &&
                !string.IsNullOrEmpty(buildNumber))
                PlayerSettings.bundleVersion += "." + buildNumber;


            File.WriteAllText(Path.Combine(BuilderUtil.ProjectDir, "BUILD_VERSION"), PlayerSettings.bundleVersion);

            if (defaultIconToOverwrite == null)
            {
                var defaultIconPath =
                    $"{EditorUtility.OpenFilePanelWithFilters("Select Your Default Icon", "Assets", new string[] {"Texture", "png", "Texture", "jpeg", "Texture", "jpg"})}";
                defaultIconToOverwrite = AssetDatabase.LoadAssetAtPath<Texture2D>(Util.ToRelativePath(defaultIconPath));
                if (defaultIconToOverwrite != null)
                {
                    foreach (Builder builder in Util.GetAssets<Builder>())
                    {
                        builder.defaultIconToOverwrite = defaultIconToOverwrite;
                        EditorUtility.SetDirty(builder);
                    }
                }
            }

            string defaultIconFilePath =
                Path.Combine(BuilderUtil.ProjectDir, AssetDatabase.GetAssetPath(defaultIconToOverwrite));

            string selectedIconFilePath = Path.Combine(BuilderUtil.ProjectDir, AssetDatabase.GetAssetPath(icon));

            File.WriteAllBytes(defaultIconFilePath, File.ReadAllBytes(selectedIconFilePath));

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(defaultIconToOverwrite));

            EditorBuildSettingsScene[] buildSettingsScenes = EditorBuildSettings.scenes;

            BootConfig bootConfig = Util.FindScriptableObject<BootConfig>();

            bootConfig.modulesManifest = manifest;
            bootConfig.buildNumber = versionCode;

            EditorUtility.SetDirty(bootConfig);

            BaseModuleManifest manifestAsset = manifest.Value();

            foreach ((ModuleLink m, FieldInfo f) in manifestAsset.GetModuleFields())
            {
                if (m.ConfigLink.HasValue)
                {
                    Util.ApplyModuleConfig(m.ImplementationType, m.ConfigLink.Value());
                }
            }

            for (int i = 0; i < buildSettingsScenes.Length; i++)
            {
                EditorBuildSettingsScene scene = buildSettingsScenes[i];
                SceneSetting setting = scenes.FirstOrDefault(x => x.name == Path.GetFileName(scene.path));
                if (setting != null)
                {
                    scene.enabled = setting.enable;
                }

                buildSettingsScenes[i] = scene;
            }

            EditorBuildSettings.scenes = buildSettingsScenes;
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;


            iosSettings.ApplySettings(this);
            androidSettings.ApplySettings(this);
            OnApplySetting();
            AssetDatabase.SaveAssets();
        }

        public bool BuildPlayer(bool autoRunPlayer)
        {
            if (Directory.Exists(OutputFolderPath) == false)
            {
                string newFolder = EditorUtility.SaveFolderPanel("Select build folder", Application.dataPath,
                    Application.dataPath);
                if (Directory.Exists(newFolder))
                {
                    OutputFolderPath = newFolder;
                    EditorUtility.SetDirty(this);
                }
                else
                {
                    return false;
                }

                return false;
            }

            string buildFilePath = GetOutputFolder();

            BuildOptions opt = developmentBuild
                ? (BuildOptions.Development & BuildOptions.AllowDebugging)
                : BuildOptions.None
                  | (autoRunPlayer ? BuildOptions.AutoRunPlayer : BuildOptions.None);

            string[] scenesToBuild = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            Debug.Log(K_LOG_TYPE + "Scenes to build : " + scenesToBuild.Aggregate((a, b) => a + ", " + b));


            Debug.Log(K_LOG_TYPE + "BuildPlayer is started. Defined symbols : " +
                      PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup));
            BuildReport report = BuildPipeline.BuildPlayer(scenesToBuild, buildFilePath, ActualBuildTarget, opt);

            switch (report.summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log(
                        $"<color=green>Build Succeeded!</color> Build time: {report.summary.totalTime.ToString()}");
                    break;
                case BuildResult.Unknown:
                    break;
                case BuildResult.Failed:
                    break;
                case BuildResult.Cancelled:
                    break;
                default:
                    Debug.LogError("Build failed!");
                    break;
            }

            return true;
        }

        private static void Build()
        {
            BuilderUtil.StartBuild(BuilderUtil.GetBuilderFromExecuteArgument(), true);
        }
    }
}