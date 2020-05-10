using UnityEditor;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
	internal interface IBuildTargetSettings
	{
		BuildTarget buildTarget { get;}

		Texture icon { get;}

		void Reset();

		void ApplySettings(Builder builder);

		void DrawSetting(SerializedObject serializedObject);
	}
}