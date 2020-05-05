using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/ShopItem")]
    public class ShopItem : ScriptableObject
    {
        public virtual IEnumerable<ShopItemLink> GetOverlappingItems()
        {
            yield break;
        }
        public SpriteLink icon;
        public PriceLink price;
        public ShopItemTagLink tag;
        public RewardLink[] rewards;


        public virtual IEnumerable<IReward> GetAllRewards() => rewards.GetValues();
        public virtual string GetPriceString() => price.GetPriceString();
        public virtual void CompletePurchase() { }

    }


    [Serializable]
    [LinkFolder(Folders.Configs + "/Shop/Items")]
    public class ShopItemLink : LinkTo<ShopItem>
    {

    }

}
