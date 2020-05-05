using SwiftFramework.Core.SharedData.Shop;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.Boosters
{
    [CreateAssetMenu(menuName = "SwiftFramework/Boosters/BoosterManagerConfig")]
    public class BoosterManagerConfig : ScriptableObject
    {
        public List<ShopItemLink> boostersForAds;
        public List<BoosterConfigLink> chipBoosters;
    }

    [Serializable]
    [LinkFolder(Folders.Configs)]
    public class BoosterManagerConfigLink : LinkTo<BoosterManagerConfig>
    {

    }
}
