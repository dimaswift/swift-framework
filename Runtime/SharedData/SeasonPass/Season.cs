using SwiftFramework.Core.SharedData.Shop;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.SeasonPass
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Season Pass/Season")]
    public class Season : ScriptableObject
    {
        public string nameKey;
        public string descKey;
        public SpriteLink buttonIcon;
        public UnixTimestamp startTime;
        public UnixTimestamp endTime;
        public ShopItemLink premiumPassOffer;
        [LinkFilter(typeof(ISeasonPassView))] public ViewLink view;
        public List<Milestone> milestones;
    }


    [Serializable]
    [LinkFolder(Folders.Configs + "/Seasons")]
    public class SeasonLink : LinkToScriptable<Season>
    {

    }

    [Serializable]
    public class Milestone
    {
        public int Index { get; set; }
        public int targetPoints;
        public List<RewardLink> freeRewards;
        public List<RewardLink> premiumRewards;
    }
}
