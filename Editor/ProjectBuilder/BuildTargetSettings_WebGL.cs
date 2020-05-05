using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;


namespace SwiftFramework.EditorUtils
{
	[System.Serializable]
	internal class BuildTargetSettings_WebGL : IBuildTargetSettings
	{
		public BuildTarget buildTarget{get{ return BuildTarget.WebGL;}}

		public Texture icon{get{ return EditorGUIUtility.FindTexture("BuildSettings.WebGL.Small");}}

		public void Reset(){}

		public void ApplySettings(Builder builder){}

		public void DrawSetting(SerializedObject serializedObject){}
	}
}