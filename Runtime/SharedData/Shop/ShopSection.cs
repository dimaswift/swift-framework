using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/Section")]
    public class ShopSection : ScriptableObject
    {
        public List<ShopItemLink> items;
    }

    [Serializable]
    [LinkFolder(Folders.Configs + "/Shop/Sections")]
    public class ShopSectionLink : LinkTo<ShopSection>
    {

    }
}
