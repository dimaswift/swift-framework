using UnityEditor;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    internal interface IBuildTargetSettings
    {
        BuildTarget BuildTarget { get; }

        Texture Icon { get; }

        void DrawSetting(SerializedObject serializedObject);
    }
}