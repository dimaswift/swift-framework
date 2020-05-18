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
        private const string OPT_BUILDER = "-builder";
        private const string OPT_CLOUD_BUILDER = "-bvrbuildtarget";
        internal const string OPT_APPEND_SYMBOL = "-appendSymbols";
        private const string OPT_OVERRIDE = "-override";
        internal const string OPT_DEV_BUILD_NUM = "-devBuildNumber";

        internal static readonly Dictionary<string, string> ExecuteArguments = new Dictionary<string, string>();

        internal static readonly string ProjectDir = Environment.CurrentDirectory.Replace('\\', '/');

        private static readonly Type builderType = AppDomain.CurrentDomain.GetAssemblies()
                                                       .SelectMany(x => x.GetTypes())
                                                       .FirstOrDefault(x => x.IsSubclassOf(typeof(Builder)))
                                                   ?? typeof(Builder);

        private static readonly MethodInfo miSetIconForObject =
            typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);

        private static Builder CurrentBuilder
        {
            get => instance.currentBuilder;
            set => instance.currentBuilder = value;
        }

        [SerializeField] private Builder currentBuilder;

        [SerializeField] private bool buildAndRun = false;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            string argKey = "";
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.IndexOf('-') == 0)
                {
                    argKey = arg;
                    ExecuteArguments[argKey] = "";
                }
                else if (0 < argKey.Length)
                {
                    ExecuteArguments[argKey] = arg;
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
                miSetIconForObject.Invoke(null, new object[] {builderScript, icon});
                EditorUtility.SetDirty(builderScript);
            }

            foreach (Builder builder in Util.GetAssets<Builder>())
            {
                SerializedObject so = new SerializedObject(builder);
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

            string filename = AssetDatabase.GenerateUniqueAssetPath(
                $"Assets/Editor/Default {EditorUserBuildSettings.activeBuildTarget}.asset");
            string path = EditorUtility.SaveFilePanelInProject("Create New Builder Asset", Path.GetFileName(filename),
                "asset", "", "Assets/Editor");
            if (path.Length == 0)
                return null;

            Builder builder = CreateInstance(builderType) as Builder;
            AssetDatabase.CreateAsset(builder, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = builder;
            return builder;
        }


        internal static Builder GetBuilderFromExecuteArgument()
        {
            var args = ExecuteArguments;

            if (args.TryGetValue(OPT_CLOUD_BUILDER, out var name))
            {
                name = name.Replace("-", " ");
            }
            else if (!args.TryGetValue(OPT_BUILDER, out name))
            {
                throw new UnityException(Builder.K_LOG_TYPE +
                                         "Error : You need to specify the builder as follows. '-builder <builder asset name>'");
            }

            Builder builder = Util.GetAssets<Builder>(name).FirstOrDefault();

            if (!builder)
            {
                throw new UnityException(Builder.K_LOG_TYPE + "Error : The specified builder could not be found. " +
                                         name);
            }

            if (builder != null && builder.ActualBuildTarget != EditorUserBuildSettings.activeBuildTarget)
            {
                throw new UnityException(Builder.K_LOG_TYPE + "Error : The specified builder's build target is not " +
                                         EditorUserBuildSettings.activeBuildTarget);
            }

            Debug.Log(Builder.K_LOG_TYPE + "Builder selected : " + builder);

            if (args.TryGetValue(OPT_OVERRIDE, out var json))
            {
                Debug.Log(Builder.K_LOG_TYPE + "Override builder with json as following\n" + json);
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
                ProjectDir
            );
        }

        internal static void StartBuild(Builder builder, bool buildAndRun)
        {
            CurrentBuilder = builder;
            instance.buildAndRun = buildAndRun;
            ResumeBuild(true);
        }


        private static void ResumeBuild(bool compileSuccessfully)
        {
            bool success = false;
            try
            {
                EditorUtility.ClearProgressBar();
                if (compileSuccessfully && CurrentBuilder)
                {
                    CurrentBuilder.ApplySettings();
                    success = CurrentBuilder.BuildPlayer(instance.buildAndRun);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (ExecuteArguments.ContainsKey("-batchmode"))
            {
                EditorApplication.Exit(success ? 0 : 1);
            }
        }
    }
}