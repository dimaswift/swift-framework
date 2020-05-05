using UnityEditor;
using UnityEngine;


namespace SwiftFramework.EditorUtils
{
	/// <summary>
	/// Build target settings interface.
	/// </summary>
	internal interface IBuildTargetSettings
	{
		/// <summary>
		/// Build target.
		/// </summary>
		BuildTarget buildTarget { get;}

		/// <summary>
		/// Icon for build target.
		/// </summary>
		Texture icon { get;}

		/// <summary>
		/// </summary>
		void Reset();

		/// <summary>
		/// On Applies the settings.
		/// </summary>
		void ApplySettings(Builder builder);

		/// <summary>
		/// Draws the setting.
		/// </summary>
		void DrawSetting(SerializedObject serializedObject);
	}
}