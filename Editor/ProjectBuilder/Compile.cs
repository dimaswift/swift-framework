using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    internal class Compile : ScriptableSingleton<Compile>
    {
        private static bool isCompiling = false;

        [SerializeField] List<string> onFinishedCompile = new List<string>();

        public static event Action<bool> OnFinishedCompile
        {
            add
            {
                string path = string.Format("{0}.{1}", value.Method.DeclaringType.FullName, value.Method.Name);
                if (instance.onFinishedCompile.Contains(path))
                    Debug.LogError(path + " already be registered.");
                else if (!value.Method.IsStatic)
                    Debug.LogError(path + " is not static method.");
                else if (value.Method.Name.StartsWith("<"))
                    Debug.LogError(path + " is anonymous method.");
                else
                    instance.onFinishedCompile.Add(path);
            }
            remove
            {
                instance.onFinishedCompile.Remove(string.Format("{0}.{1}", value.Method.DeclaringType.FullName, value.Method.Name));
            }
        }

        [InitializeOnLoadMethod]
        static void OnFinishedCompileSuccessfully()
        {
            EditorApplication.delayCall += () =>
            {
                instance.HandleFinishedCompile(true);

                EditorApplication.update += () =>
                {
                    if (isCompiling == EditorApplication.isCompiling)
                        return;

                    isCompiling = EditorApplication.isCompiling;

                    if (!isCompiling)
                        instance.HandleFinishedCompile(false);
                };
            };
        }

        private void HandleFinishedCompile(bool successfully)
        {
            foreach (var methodPath in onFinishedCompile.ToArray())
            {
                try
                {
                    string className = Path.GetFileNameWithoutExtension(methodPath);
                    string methodName = Path.GetExtension(methodPath).TrimStart('.');
                    MethodInfo ret = Type.GetType(className).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    ret.Invoke(null, new object[] { successfully });
                }
                catch (Exception e)
                {
                    Debug.LogError(methodPath + " cannnot call. " + e.Message);
                }
                onFinishedCompile.Remove(methodPath);
            }
        }
    }
}