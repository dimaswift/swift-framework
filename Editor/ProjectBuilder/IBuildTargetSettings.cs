using UnityEditor;
using UnityEngine;

namespace Swift.EditorUtils
{
    internal interface IBuildTargetSettings
    {
        BuildTarget BuildTarget { get; }

        Texture Icon { get; }

        void DrawSetting(SerializedObject serializedObject);
    }
}