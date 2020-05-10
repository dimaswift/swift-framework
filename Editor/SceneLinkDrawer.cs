using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(SceneLink))]
    public class SceneLinkPropertyDrawer : PropertyDrawer
    {
        private SceneLinkDrawer drawer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (drawer == null)
            {
                drawer = new SceneLinkDrawer(typeof(SceneAsset), fieldInfo);
            }
            drawer.Draw(position, property, label);
        }
    }


    internal class SceneLinkDrawer : BaseLinkDrawer
    {
        public SceneLinkDrawer(System.Type type, FieldInfo fieldInfo) : base(type, fieldInfo)
        {
        }

        protected override void OnAssetChanged(string previousAssetPath, string newAssetPath)
        {
            List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            editorBuildSettingsScenes.RemoveAll(s => s.path == previousAssetPath);

            if (editorBuildSettingsScenes.FindIndex(s => s.path == newAssetPath) == -1)
            {
                editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(newAssetPath, true));
                EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
            }
        }

        protected override void OnNullSelected(string previousAssetPath)
        {
            List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            editorBuildSettingsScenes.RemoveAll(s => s.path == previousAssetPath);
            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        }

        protected override void Reload()
        {
            assets.Clear();
#if USE_ADDRESSABLES
            assets.AddRange(AddrHelper.GetScenes());
#else
            assets.AddRange(ResourcesAssetHelper.GetScenes());
#endif

        }
    }
}