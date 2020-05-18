using UnityEditor;
using UnityEngine;
namespace SwiftFramework.EditorUtils
{
    [System.Serializable]
    internal class BuildTargetSettings_WebGL : IBuildTargetSettings
    {
        public BuildTarget BuildTarget => BuildTarget.WebGL;

        public Texture Icon => EditorGUIUtility.FindTexture("BuildSettings.WebGL.Small");

        public void Reset() { }

        public void ApplySettings(Builder builder) { }

        public void DrawSetting(SerializedObject serializedObject) { }
    }
}