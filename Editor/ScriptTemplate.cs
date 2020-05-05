using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace SwiftFramework.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Script Template")]
    internal class ScriptTemplate : ScriptableObject
    {
        public string folder = null;

        public string scriptBody = null;

        public string[] arguments = { };

        public string fileName = null;

        [ContextMenu("Create")]
        public void Create()
        {
            string script = string.Format(scriptBody, arguments);
           
            string path = Path.Combine(Application.dataPath, folder, string.IsNullOrEmpty(fileName) ? $"{arguments[0]}.cs" : $"{fileName}.cs");
            if (File.Exists(path) && EditorUtility.DisplayDialog("Warning", "Script already exists. Overwrite?", "Yes", "Cancel") == false)
            {
                return;
            }
            File.WriteAllText(path, script);
            AssetDatabase.Refresh();
        }
    }

    [CustomEditor(typeof(ScriptTemplate), true)]
    [CanEditMultipleObjects]
    internal class ScriptTemplateEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ScriptTemplate template = target as ScriptTemplate;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("folder"));

            Undo.RecordObject(template, "Script template");
            template.scriptBody = EditorGUILayout.TextArea(template.scriptBody);

            EditorUtility.SetDirty(template);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("arguments"), true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fileName"));

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Create"))
            {
                template.Create();
            }
        }
    }
}
