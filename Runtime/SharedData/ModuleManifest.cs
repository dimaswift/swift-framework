using System;
using UnityEngine;

namespace Swift.Core
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
            set
            {
                options.initializeOnLoad = value == ModuleLoadType.OnInitialize;
            }
        }

        public bool CreateOnAppLoad => options.createOnAppLoaded;

        public ModuleState State
        {
            get => options.state;
            set => options.state = value;
        } 

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
            createOnAppLoaded = false,
            state = ModuleState.Enabled
        };
        
        [Serializable]
        public struct Options
        {
            public bool createOnAppLoaded;
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