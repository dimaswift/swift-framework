using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    internal class BuilderEditor : EditorWindow
    {
        private Vector2 scrollPosition;
        private Builder[] targets;
        private SerializedObject serializedObject;
        private const string K_PREFS_KEY_LAST_SELECTED = "ProjectBuilderEditor_LastSelected";
        private static GUIContent contentOpen;
        private static GUIContent contentTitle = new GUIContent();
        private static ReorderableList roSceneList;
        private static ReorderableList roExcludeDirectoriesList;
        private static ReorderableList roBuilderList;
        private static GUIStyle styleCommand;
        private static GUIStyle styleTitle;
        private static string endBasePropertyName = "";
        private static string[] availableScenes;
        private static List<Builder> buildersInProject;

        private static readonly Dictionary<BuildTarget, IBuildTargetSettings> buildTargetSettings =
            typeof(Builder).Assembly
                .GetTypes()
                .Where(x => x.IsPublic && !x.IsInterface && typeof(IBuildTargetSettings).IsAssignableFrom(x))
                .Select(x => Activator.CreateInstance(x) as IBuildTargetSettings)
                .OrderBy(x => x.BuildTarget)
                .ToDictionary(x => x.BuildTarget);

        private static readonly int[] buildTargetValues = buildTargetSettings.Keys.Cast<int>().ToArray();

        private static readonly GUIContent[] buildTargetLabels =
            buildTargetSettings.Keys.Select(x => new GUIContent(x.ToString())).ToArray();

        private static Texture GetBuildTargetIcon(Builder builder)
        {
            return buildTargetSettings.ContainsKey(builder.buildTarget)
                ? buildTargetSettings[builder.buildTarget].Icon
                : EditorGUIUtility.FindTexture("BuildSettings.Editor.Small");
        }
#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Build Helper")]
#endif
        public static void OnOpenFromMenu()
        {
            GetWindow<BuilderEditor>("Project Builder");
        }

        private void Initialize()
        {
            if (styleCommand != null)
                return;

            styleTitle = new GUIStyle("IN BigTitle")
            {
                alignment = TextAnchor.UpperLeft, fontSize = 12, stretchWidth = true, margin = new RectOffset()
            };

            styleCommand = new GUIStyle(EditorStyles.textArea) {stretchWidth = false, fontSize = 9};
            contentOpen = new GUIContent(EditorGUIUtility.FindTexture("project"));

            Builder dummy = CreateInstance<Builder>();
            SerializedProperty sp = new SerializedObject(dummy).GetIterator();
            sp.Next(true);
            while (sp.Next(false))
                endBasePropertyName = sp.name;

            roSceneList = new ReorderableList(new List<Builder.SceneSetting>(), typeof(Builder.SceneSetting));
            roSceneList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = roSceneList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, 16, rect.height - 2),
                    element.FindPropertyRelative("enable"), GUIContent.none);
                EditorGUIEx.TextFieldWithTemplate(new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height - 2),
                    element.FindPropertyRelative("name"), GUIContent.none, availableScenes, false);
            };
            roSceneList.headerHeight = 0;
            roSceneList.elementHeight = 18;

            roExcludeDirectoriesList = new ReorderableList(new List<string>(), typeof(string));
            roExcludeDirectoriesList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = roExcludeDirectoriesList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUIEx.DirectoryPathField(rect, element, GUIContent.none, "Select exclude directory in build.");
            };
            roExcludeDirectoriesList.headerHeight = 0;
            roExcludeDirectoriesList.elementHeight = 18;

            roBuilderList = new ReorderableList(buildersInProject, typeof(Builder))
            {
                onSelectCallback = list => Selection.activeObject = list.list[list.index] as Builder
            };
            roBuilderList.onAddCallback += list =>
            {
                EditorApplication.delayCall += () =>
                {
                    BuilderUtil.CreateBuilderAsset();
                    OnSelectionChanged();
                };
            };
            roBuilderList.onRemoveCallback += list =>
            {
                EditorApplication.delayCall += () =>
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(list.list[list.index] as Builder));
                    AssetDatabase.Refresh();
                    OnSelectionChanged();
                };
            };
            roBuilderList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                Builder b = roBuilderList.list[index] as Builder;
                if (!b)
                    return;

                GUI.DrawTexture(new Rect(rect.x, rect.y + 2, 16, 16), GetBuildTargetIcon(b));
                GUI.Label(new Rect(rect.x + 16, rect.y + 2, rect.width - 16, rect.height - 2),
                    new GUIContent($"{b.name} ({b.productName})"));
            };
            roBuilderList.headerHeight = 0;
            roBuilderList.draggable = false;

            contentTitle = new GUIContent(Util.GetAssets<Texture2D>(typeof(Builder).Name + " Icon").FirstOrDefault());
            DestroyImmediate(dummy);
        }

        private void OnEnable()
        {
            targets = null;

            string path =
                AssetDatabase.GUIDToAssetPath(
                    PlayerPrefs.GetString(K_PREFS_KEY_LAST_SELECTED + EditorUserBuildSettings.activeBuildTarget));
            if (!string.IsNullOrEmpty(path))
            {
                Builder builder = AssetDatabase.LoadAssetAtPath<Builder>(path);
                if (builder)
                {
                    SelectBuilder(new[] {builder});
                }
            }

            if (targets == null)
            {
                if (Selection.objects.OfType<Builder>().Any())
                {
                    SelectBuilder(Selection.objects.OfType<Builder>().ToArray());
                }
                else
                {
                    var builders = Util.GetAssets<Builder>().ToArray();
                    if (builders.Any())
                    {
                        SelectBuilder(builders.Take(1).ToArray());
                    }
                }
            }

            Selection.selectionChanged += OnSelectionChanged;
            minSize = new Vector2(300, 300);
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void SelectBuilder(Builder[] builders)
        {
            availableScenes = EditorBuildSettings.scenes.Select(x => Path.GetFileName(x.path)).ToArray();

            buildersInProject = new List<Builder>(
                Util.GetAssets<Builder>()
            );

            targets = 0 < builders.Length
                ? builders
                : buildersInProject.Take(1).ToArray();

            serializedObject = null;

            contentTitle.text = 0 < targets.Length
                ? targets.Select(x => "  " + x.name).Aggregate((a, b) => a + "\n" + b)
                : "";

            Builder lastSelected =
                targets.FirstOrDefault(x => x.buildTarget == EditorUserBuildSettings.activeBuildTarget);
            if (lastSelected)
            {
                PlayerPrefs.SetString(K_PREFS_KEY_LAST_SELECTED + EditorUserBuildSettings.activeBuildTarget,
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(lastSelected)));
                PlayerPrefs.Save();
            }
        }

        private void OnSelectionChanged()
        {
            var builders = Selection.objects.OfType<Builder>().ToArray();

            if (0 < builders.Length || targets.Any(x => !x))
            {
                SelectBuilder(builders);
                Repaint();
            }
        }

        private void OnGUI()
        {
            Initialize();

            if (targets == null || targets.Length == 0)
            {
                if (GUILayout.Button("Create New ProjectBuilder Asset"))
                    Selection.activeObject = BuilderUtil.CreateBuilderAsset();
                return;
            }

            using (EditorGUILayout.ScrollViewScope svs = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = svs.scrollPosition;

                serializedObject = serializedObject ?? new SerializedObject(targets);
                serializedObject.Update();

                GUILayout.Label(contentTitle, styleTitle);

                DrawCustomProjectBuilder();
                DrawApplicationBuildSettings();
                DrawBuildTargetSettings();
                DrawControlPanel();


                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawCustomProjectBuilder()
        {
            Type type = serializedObject.targetObject.GetType();
            if (type == typeof(Builder))
                return;

            GUI.backgroundColor = Color.green;
            using (new EditorGUIEx.GroupScope(type.Name))
            {
                GUI.backgroundColor = Color.white;

                GUILayout.Space(-20);
                Rect rButton = EditorGUILayout.GetControlRect();
                rButton.x += rButton.width - 50;
                rButton.width = 50;
                if (GUI.Button(rButton, "Edit", EditorStyles.miniButton))
                {
                    InternalEditorUtility.OpenFileAtLineExternal(
                        AssetDatabase.GetAssetPath(
                            MonoScript.FromScriptableObject(serializedObject.targetObject as ScriptableObject)), 1);
                }

                var itr = serializedObject.GetIterator();

                itr.NextVisible(true);
                while (itr.NextVisible(false) && itr.name != endBasePropertyName)
                    ;

                while (itr.NextVisible(false))
                    EditorGUILayout.PropertyField(itr, true);
            }
        }

        private void DrawApplicationBuildSettings()
        {
            var spBuildTarget = serializedObject.FindProperty("buildTarget");
            using (new EditorGUIEx.GroupScope("Application Build Setting"))
            {
                EditorGUILayout.IntPopup(spBuildTarget, buildTargetLabels, buildTargetValues);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("companyName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("productName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("applicationIdentifier"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
                EditorGUILayout.HelpBox(
                    "Default will always be overwritten on the disk by the icon selected above. It should always be assigned as a Default Icon in Player Settings",
                    MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultIconToOverwrite"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("buildAppBundle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showUnitySplashScreen"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showSplashScreen"));
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("developmentBuild"));

                EditorGUILayout.LabelField("Exclude Directories");
                roExcludeDirectoriesList.serializedProperty = serializedObject.FindProperty("excludeDirectories");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(16);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUI.indentLevel--;
                        roExcludeDirectoriesList.DoLayoutList();
                        EditorGUI.indentLevel++;
                    }
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField("Version Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("version"));
                switch ((BuildTarget) spBuildTarget.intValue)
                {
                    case BuildTarget.Android:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("versionCode"),
                            new GUIContent("Version Code"));
                        break;
                    case BuildTarget.iOS:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("versionCode"),
                            new GUIContent("Build Number"));
                        break;
                }

                EditorGUI.indentLevel--;
            }
        }


        private void DrawBuildTargetSettings()
        {
            SerializedProperty spBuildTarget = serializedObject.FindProperty("buildTarget");
            BuildTarget buildTarget = (BuildTarget) spBuildTarget.intValue;
            if (buildTargetSettings.ContainsKey(buildTarget))
                buildTargetSettings[buildTarget].DrawSetting(serializedObject);
        }

        private void DrawControlPanel()
        {
            Builder builder = serializedObject.targetObject as Builder;
            if (builder == null)
            {
                return;
            }

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(
                    new GUIContent(
                        $"{builder.productName} ver.{builder.version} ({builder.versionCode})",
                        GetBuildTargetIcon(builder)), EditorStyles.largeLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Apply Setting", EditorGUIUtility.FindTexture("vcs_check"))))
                    {
                        builder.ApplySettings();
                    }

                    if (GUILayout.Button(
                        new GUIContent("Player Setting", EditorGUIUtility.FindTexture("EditorSettings Icon")),
                        GUILayout.Height(21), GUILayout.Width(110)))
                    {
                        Selection.activeObject = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Build Addressable Assets",
                        EditorGUIUtility.FindTexture("vcs_check"))))
                    {
                        Builder.BuildAddressableAssets();
                    }
                }

                EditorGUI.BeginDisabledGroup(builder.ActualBuildTarget != EditorUserBuildSettings.activeBuildTarget);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(
                        new GUIContent($"Build to '{builder.BuildName}'",
                            EditorGUIUtility.FindTexture("preAudioPlayOff")), "LargeButton"))
                    {
                        EditorApplication.delayCall += () => BuilderUtil.StartBuild(builder, false);
                    }

                    var r = EditorGUILayout.GetControlRect(false, GUILayout.Width(15));
                    if (GUI.Button(new Rect(r.x - 2, r.y + 5, 20, 20), contentOpen, EditorStyles.label))
                        BuilderUtil.RevealOutputInFinder(builder.OutputFolderPath);
                    EditorGUI.EndDisabledGroup();
                }

                if (GUILayout.Button(new GUIContent("Build & Run", EditorGUIUtility.FindTexture("preAudioPlayOn")),
                    "LargeButton"))
                {
                    EditorApplication.delayCall += () => BuilderUtil.StartBuild(builder, true);
                }

                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("Convert to JSON (console log)"))
                {
                    Debug.Log(JsonUtility.ToJson(builder, true));
                }

                GUILayout.Space(10);
                GUILayout.Label("Available Project Builders", EditorStyles.boldLabel);
                roBuilderList.list = buildersInProject;
                roBuilderList.index = buildersInProject.FindIndex(x => x == serializedObject.targetObject);
                roBuilderList.DoLayoutList();
            }
        }
    }
}