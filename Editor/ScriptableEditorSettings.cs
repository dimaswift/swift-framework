using System.IO;
using System.Linq;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    public class ScriptableEditorSettings<T> : ScriptableObject where T : ScriptableEditorSettings<T>
    {
        private static T instance;
        protected virtual bool AutoCreate => true;
        
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = AssetDatabase.LoadAssetAtPath<T>(Path);
                    if (instance == null)
                    {
                        if (Directory.Exists(Util.EditorFolder) == false)
                        {
                            Directory.CreateDirectory(Util.EditorFolder);
                        }
                        instance = CreateInstance<T>();
                        if (instance.AutoCreate)
                        {
                            return Create(instance);
                        }
                        DestroyImmediate(instance);
                        return null;
                    }
                }

                return instance;
            }
        }

        private static  string Path => $"{Util.EditorFolder}/{typeof(T).Name}.asset";
        
        public static T Create(T existingInstance = null)
        {
            instance = existingInstance == null ? CreateInstance<T>() : existingInstance;
  
            instance.hideFlags = HideFlags.NotEditable;
            instance.name = typeof(T).Name;
            AssetDatabase.CreateAsset(instance, Path);
            AssetDatabase.Refresh();

            return instance;
        }
        
        
        
    }
}