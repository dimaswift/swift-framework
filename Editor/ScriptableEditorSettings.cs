using SwiftFramework.EditorUtils;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    internal class ScriptableEditorSettings<T> : ScriptableObject where T : ScriptableObject
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
                        if (System.IO.Directory.Exists(folder) == false)
                        {
                            System.IO.Directory.CreateDirectory(folder);
                        }
                        string path = $"{folder}/{typeof(T).Name}.asset";
                        instance = CreateInstance<T>();
                        instance.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.HideInInspector;
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
