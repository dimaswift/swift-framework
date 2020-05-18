using System.IO;
using System.Linq;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    public class ScriptableEditorSettings<T> : ScriptableObject where T : ScriptableObject
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = AssetDatabase.FindAssets("t:" + typeof(T).Name)
                        .Select(x => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(x), typeof(T)) as T)
                        .FirstOrDefaultFast();

                    if (instance == null)
                    {
                        string folder = Util.EditorFolder;
                        if (Directory.Exists(folder) == false)
                        {
                            Directory.CreateDirectory(folder);
                        }

                        string path = $"{folder}/{typeof(T).Name}.asset";
                        instance = CreateInstance<T>();
                        instance.hideFlags = HideFlags.NotEditable;
                        instance.name = typeof(T).Name;
                        AssetDatabase.CreateAsset(instance, path);
                        AssetDatabase.Refresh();
                    }
                }

                return instance;
            }
        }
    }
}