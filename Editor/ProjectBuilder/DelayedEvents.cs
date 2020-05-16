using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    public class DelayedEvents : ScriptableSingleton<DelayedEvents>
    {
        [SerializeField] List<DelayedEvent> delayedEvents = new List<DelayedEvent>();

        private class DelayedEvent
        {
            public string path;
            public string key;
        }

        public static void AddListener(Action action, string key)
        {
            string path = string.Format("{0}.{1}", action.Method.DeclaringType.FullName, action.Method.Name);
            var delayedEvent = new DelayedEvent()
            {
                path = path,
                key = key
            };
            if (instance.delayedEvents.FindIndex(e => e.path == path) != -1)
                Debug.LogError(path + " already be registered.");
            else if (!action.Method.IsStatic)
                Debug.LogError(path + " is not static method.");
            else if (action.Method.Name.StartsWith("<"))
                Debug.LogError(path + " is anonymous method.");
            else
                instance.delayedEvents.Add(delayedEvent);
        }

        public static void Invoke(string key)
        {
            foreach (var e in instance.delayedEvents.ToArray())
            {
                if (e.key != key)
                {
                    return;
                }
                try
                {
                    string className = Path.GetFileNameWithoutExtension(e.path);
                    string methodName = Path.GetExtension(e.path).TrimStart('.');
                    MethodInfo ret = Type.GetType(className).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    ret.Invoke(null, new object[] { });
                }
                catch (Exception exc)
                {
                    Debug.LogError(e.path + " cannnot call. " + exc.Message);
                }
                instance.delayedEvents.RemoveAll(_e => _e.path == e.path);
            }
        }

    }
}