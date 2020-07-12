using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [AddrLabel(AddrLabels.Module)]
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/ModuleManifest")]
    public class ModuleManifest : ScriptableObject
    {
        public ModuleLoadType LoadType
        {
            get
            {
                if (options.initializeOnLoad)
                {
                    return ModuleLoadType.OnInitialize;
                }

                return ModuleLoadType.OnDemand;
            }
        }
        public ModuleState State => options.state;

        public ModuleLink Link
        {
            get => module;
            set => module = value;
        }
        
        public Type InterfaceType => module.InterfaceType;
        public Type ImplementationType => module.ImplementationType;

        [SerializeField] private ModuleLink module = null;
        [SerializeField] private Options options = new Options()
        {
            initializeOnLoad = true,
            state = ModuleState.Enabled
        };
        
        [Serializable]
        public struct Options
        {
            public bool initializeOnLoad;
            public ModuleState state;
        }
    }

    [Flags]
    public enum ModuleLoadType
    {
        OnDemand = 1, 
        OnInitialize = 2,
    }
    
    public enum ModuleState
    {
        Enabled, Disabled
    }
}