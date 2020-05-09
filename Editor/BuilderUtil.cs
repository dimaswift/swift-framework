using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    internal class BuilderUtil : ScriptableSingleton<BuilderUtil>
    {
        internal const string OPT_BUILDER = "-builder";
        internal const string OPT_CLOUD_BUILDER = "-bvrbuildtarget";
        internal const string OPT_APPEND_SYMBOL = "-appendSymbols";
        internal const string OPT_OVERRIDE = "-override";
        internal const string OPT_DEV_BUILD_NUM = "-devBuildNumber";

        internal static readonly Dictionary<string, string> executeArguments = new Dictionary<string, string>();

        internal static readonly string projectDir = Environment.CurrentDirectory.Replace('\\', '/');

        internal static readonly Type builderType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .FirstOrDefault(x => x.IsSubclassOf(typeof(Builder)))
                                                  ?? typeof(Builder);

        internal static readonly MethodInfo miSetIconForObject = typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);

        internal static Builder currentBuilder { get { return instance.m_CurrentBuilder; } private set { instance.m_CurrentBuilder = value; } }

        [SerializeField] Builder m_CurrentBuilder;

        [SerializeField] bool m_BuildAndRun = false;

        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            string argKey = "";
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.IndexOf('-') == 0)
                {
                    argKey = arg;
                    executeArguments[argKey] = "";
                }
                else if (0 < argKey.Length)
                {
                    executeArguments[argKey] = arg;
                    argKey = "";
                }
            }

            EditorApplication.delayCall += UpdateBuilderAssets;
        }

        private static void UpdateBuilderAssets()
        {
            MonoScript builderScript = Resources.FindObjectsOfTypeAll<MonoScript>()
                .FirstOrDefault(x => x.GetClass() == builderType);

            Texture2D icon = Util.GetAssets<Texture2D>(typeof(Builder).Name + " Icon").FirstOrDefault();

            if (builderType == typeof(Builder))
                return;

            if (icon && builderScript && miSetIconForObject != null)
            {
                miSetIconForObject.Invoke(null, new object[] { builderScript, icon });
                EditorUtility.SetDirty(builderScript);
            }

            foreach (var builder in Util.GetAssets<Builder>())
            {
                var so = new SerializedObject(builder);
                so.Update();
                so.FindProperty("m_Script").objectReferenceValue = builderScript;
                so.ApplyModifiedProperties();
            }

            AssetDatabase.Refresh();
        }


        internal static Builder CreateBuilderAsset()
        {
            if (!Directory.Exists("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");

            // Open save file dialog.
            string filename = AssetDatabase.GenerateUniqueAssetPath(string.Format("Assets/Editor/Default {0}.asset", EditorUserBuildSettings.activeBuildTarget));
            string path = EditorUtility.SaveFilePanelInProject("Create New Builder Asset", Path.GetFileName(filename), "asset", "", "Assets/Editor");
            if (path.Length == 0)
                return null;

            // Create and save a new builder asset.
            Builder builder = ScriptableObject.CreateInstance(builderType) as Builder;
            AssetDatabase.CreateAsset(builder, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = builder;
            return builder;
        }


        internal static Builder GetBuilderFromExecuteArgument()
        {
            string name;
            var args = executeArguments;

            if (args.TryGetValue(OPT_CLOUD_BUILDER, out name))
            {
                name = name.Replace("-", " ");
            }
            else if (!args.TryGetValue(OPT_BUILDER, out name))
            {
                throw new UnityException(Builder.kLogType + "Error : You need to specify the builder as follows. '-builder <builder asset name>'");
            }

            Builder builder = Util.GetAssets<Builder>(name).FirstOrDefault();

            if (!builder)
            {
                throw new UnityException(Builder.kLogType + "Error : The specified builder could not be found. " + name);
            }
            else if (builder.actualBuildTarget != EditorUserBuildSettings.activeBuildTarget)
            {
                throw new UnityException(Builder.kLogType + "Error : The specified builder's build target is not " + EditorUserBuildSettings.activeBuildTarget);
            }
            else
            {
                Debug.Log(Builder.kLogType + "Builder selected : " + builder);
            }


            string json;
            if (args.TryGetValue(OPT_OVERRIDE, out json))
            {
                UnityEngine.Debug.Log(Builder.kLogType + "Override builder with json as following\n" + json);
                JsonUtility.FromJsonOverwrite(json, builder);
            }
            return builder;
        }

        public static void RevealOutputInFinder(string path)
        {
            if (InternalEditorUtility.inBatchMode)
                return;

            var parent = Path.GetDirectoryName(path);
            EditorUtility.RevealInFinder(
                (Directory.Exists(path) || File.Exists(path)) ? path :
                (Directory.Exists(parent) || File.Exists(parent)) ? parent :
                projectDir
            );
        }

        internal static void StartBuild(Builder builder, bool buildAndRun)
        {
            currentBuilder = builder;
            instance.m_BuildAndRun = buildAndRun;
            ResumeBuild(true);
        }


        public static void ResumeBuild(bool compileSuccessfully)
        {
            bool success = false;
            try
            {
                EditorUtility.ClearProgressBar();
                if (compileSuccessfully && currentBuilder)
                {
                    currentBuilder.ApplySettings();
                    success = currentBuilder.BuildPlayer(instance.m_BuildAndRun);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (executeArguments.ContainsKey("-batchmode"))
            {
                EditorApplication.Exit(success ? 0 : 1);
            }
        }

    }
}
