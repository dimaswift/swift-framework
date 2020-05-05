using System;
using UnityEngine;

namespace SwiftFramework.Core.Boosters
{
    [AddrLabel(AddrLabels.Booster)]
    [CreateAssetMenu(menuName = "SwiftFramework/Boosters/Config")]
    [PrewarmAsset]
    public class BoosterConfig : ScriptableObject
    {
        public BoosterType type;
        public BoosterOperation operation;
        public BoosterTargetLink target;
        public SpriteLink icon;
        public long multiplier;
        public long durationSeconds;
        public long cooldownSeconds;
        public int maxActiveBoostersAmount;
        public string descriptionKey;
        public bool activateInstantly;
        public string OverviewDescKey => $"{descriptionKey}_overview";
        public string mergingTag;
    }

    [Serializable]
    [LinkFolder(Folders.Configs + "/Boosters")]
    public class BoosterConfigLink : LinkTo<BoosterConfig>
    {

    }
}
