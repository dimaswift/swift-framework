using System;
using UnityEngine;

namespace SwiftFramework.Core.Boosters
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Boosters/Template")]
    public class BoosterTemplate : ScriptableObject
    {
        public bool stackable;
        public BoosterType type;
        public BoosterTargetLink[] possibleTargets;
        public SpriteLink[] possibleIcons;
        public long[] possibleMultipliers;
        public long[] possibleDurations;
        public long[] possibleCooldownSeconds;
    }


    [Serializable]
    [LinkFolder(Folders.Configs + "/Boosters/Templates")]
    public class BoosterTemplateLink : LinkTo<BoosterTemplate>
    {

    }
}
