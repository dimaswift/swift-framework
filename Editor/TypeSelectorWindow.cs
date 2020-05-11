using SwiftFramework.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    internal class TypeSelectorWindow : EditorWindow
    {
        [SerializeField] private int currentType = 0;
        private IEnumerable<Type> types;

        private Promise<Type> promise;

        public static IPromise<Type> Open(IEnumerable<Type> types, string title)
        {
            var win = GetWindow<TypeSelectorWindow>(true, title, true);

            win.MoveToCenter();

            win.types = types;

            win.promise = Promise<Type>.Create();

            return win.promise;
        }

        public void OnGUI()
        {
            if(promise == null)
            {
                Close();
                return;
            }

            Undo.RecordObject(this, "Type Selector");

            EditorGUILayout.LabelField("Select type");

            List<(string, Type)> names = new List<(string, Type)>();

            foreach (var type in types)
            {
                names.Add((type.Name, type));
            }

            GUILayout.Space(18);

            currentType = EditorGUILayout.Popup(currentType, names.ConvertAll(c => c.Item1).ToArray());

            GUILayout.Space(18);

            if (GUILayout.Button("Choose"))
            {
                promise.Resolve(names[currentType].Item2);
                Close();
                return;
            }

            EditorUtility.SetDirty(this);
        }
    }
}
