using SwiftFramework.Core;
using SwiftFramework.Core.Boosters;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop
{
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/Item with bonus booster")]
    public class ShopItemWithBonusBooster : ShopItem
    {
        public BoosterConfigLink bonusBooster;

        public override IEnumerable<IReward> GetAllRewards()
        {
            foreach (IReward reward in base.GetAllRewards())
            {
                yield return reward;
            }
        }
    }

}
