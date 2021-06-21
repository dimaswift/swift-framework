#if USE_ADDRESSABLES
using System.Collections.Generic;
using Swift.EditorUtils;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Swift.Core.Editor
{
    internal class LinkValidator : ScriptableSingleton<LinkValidator>
    {
        private static readonly List<FieldData<Link>> links = new List<FieldData<Link>>();
        public List<Object> invalidAssets = new List<Object>();
#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Links/Get Last Validation Results")]
#endif
        public static void GetResults()
        {
            Selection.activeObject = instance;
        }
#if SWIFT_FRAMEWORK_INSTALLED
        [MenuItem("SwiftFramework/Links/Validate")]
#endif
        public static void Validate()
        {
            links.Clear();
            links.AddRange(FieldReferenceFinder.FindAllFields<Link>());
            ShowProgress(0, links.Count);
            instance.invalidAssets.Clear();
            int index = 0;
            foreach (var data in links)
            {
                if(data.property != null && data.property.FindPropertyRelative("Path") != null)
                {
                    var path = data.property.FindPropertyRelative("Path").stringValue;
                    if(path == Link.NULL)
                    {
                        continue;
                    }
                    var entry = AddrHelper.FindEntry(path);
                    if (entry == null)
                    {
                        if (instance.invalidAssets.Contains(data.asset) == false)
                        {
                            instance.invalidAssets.Add(data.asset);
                        }
                        Debug.LogWarning($"Addressable entry not found! Address {path}, asset: { AssetDatabase.GetAssetPath(data.asset) }");
                        continue;
                    }
                    if (AssetDatabase.LoadAssetAtPath(entry.AssetPath, typeof(Object)) == null)
                    {
                        if (instance.invalidAssets.Contains(data.asset) == false)
                        {
                            instance.invalidAssets.Add(data.asset);
                        }
                  
                        Debug.LogWarning($"Addressable asset not found: {path}, asset: { AssetDatabase.GetAssetPath(data.asset) }");
                    }
                }
                ShowProgress(index++, links.Count);
            }
           
            
            EditorUtility.ClearProgressBar();
            EditorUtility.SetDirty(instance);
            Selection.activeObject = instance;
            
        }

        private static void OnLoaded(AsyncOperationHandle operation, FieldData<Link> data)
        {
           
            if (operation.Status == AsyncOperationStatus.Failed)
            {
                instance.invalidAssets.Add(data.asset);
            }
        }

        private static void ShowProgress(int current, int total)
        {
            EditorUtility.DisplayProgressBar("Validating links...", $"Objects checked {current / total}", (float)current / total);
        }
    }

}
#endif