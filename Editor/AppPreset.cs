using System.Collections.Generic;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CreateAssetMenu(menuName = "SwiftFramework/Editor/App Preset")]
    public class AppPreset : ScriptableObject
    {
        [SerializeField] private List<ModuleLink> modules = new List<ModuleLink>();

        [ContextMenu("Apply")]
        public void Apply()
        {
            foreach (ModuleLink moduleLink in modules)
            {
                ModuleInstaller.Install(moduleLink);
            }
            AssetsUtil.TriggerPostprocessEvent();
        }
        
        [ContextMenu("Save")]
        public void Save()
        {
            Undo.RecordObject(this, "App Preset");
            modules.Clear();
            foreach (ModuleManifest moduleManifest in Util.GetAssets<ModuleManifest>())
            {
                if (moduleManifest.State == ModuleState.Enabled)
                {
                    modules.Add(moduleManifest.Link.DeepCopy());
                }
            }
            EditorUtility.SetDirty(this);
        }
    }
}