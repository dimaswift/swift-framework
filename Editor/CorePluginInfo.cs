using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/Internal/Core Plugin Info")]
    public class CorePluginInfo : PluginInfo
    {
        [NonSerialized] private SerializedObject serializedObject = null;

        [SerializeField] private bool useAddressables = true;

        [SerializeField] private bool show;

        public override void DrawCustomGUI(Action repaintHandler, PluginData data)
        {
            show = EditorGUILayout.BeginFoldoutHeaderGroup(show, "Options");

            EditorGUI.indentLevel++;

            if (show == false)
            {
                return;
            }

            if (serializedObject == null)
            {
                serializedObject = new SerializedObject(this);
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(useAddressables)));
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Separator();


            EditorGUI.indentLevel--;
        }

        public override IEnumerable<PackageDependency> GetPackages()
        {
            foreach (PackageDependency packageDependency in packageDependencies)
            {
                if (useAddressables == false && packageDependency.packageName == "com.unity.addressables")
                {
                    continue;
                }
                yield return packageDependency;
            }
        }

        public override IEnumerable<(string symbolName, string symbolDesc)> GetSymbols()
        {
            foreach (var symbol in base.GetSymbols())
            {
                if (useAddressables == false && symbol.symbolName == "USE_ADDRESSABLES")
                {
                    continue;
                }
                yield return symbol;
            }
        }


        public override bool CanInstall()
        {
            return true;
        }

        public override bool CanRemove()
        {
            return true;
        }
    }
}