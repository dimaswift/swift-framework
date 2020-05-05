using System;
using UnityEngine;

namespace SwiftFramework.Core.Supervisors
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Supervisors/Supervisor")]
    public class Supervisor : ScriptableObject
    {
        public int level;
        public string nameKey;
        public PriceLink price;
        public AbilityLink ability;
        public SupervisorSkinLink skin;
        public bool isShared;
        public SupervisorFamilyLink family;
        public PerkLink perk;
    }

    [Serializable]
    [LinkFolder(Folders.Configs + "/Supervisors")]
    public class SupervisorLink : LinkToScriptable<Supervisor>
    {

    }
}
