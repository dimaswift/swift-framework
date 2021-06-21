using UnityEditor;
using UnityEngine;
namespace Swift.EditorUtils
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