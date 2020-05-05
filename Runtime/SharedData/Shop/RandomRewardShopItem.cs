using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop
{
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/Random Booster")]
    public class RandomRewardShopItem : ShopItem
    {
        [SerializeField] private RandomReward[] possibleRewards = { };

        [Serializable]
        private class RandomReward
        {
            [Range(0f, 1f)]
            public float chance = 0;
            public RewardLink link = null;
        }

        [NonSerialized] private RewardLink lastRolledReward = null;

        public override IEnumerable<IReward> GetAllRewards()
        {
            if (lastRolledReward == null)
            {
                Debug.LogError("Don't forget to Roll random reward before buying!");
                yield break;
            }
            yield return lastRolledReward.Value;
        }

        public RewardLink GetLastRolledReward() => lastRolledReward;

        public IEnumerable<RewardLink> GetPossibleRewards()
        {
            foreach (RandomReward reward in possibleRewards)
            {
                yield return reward.link;
            }
        }

        public RewardLink RollReward()
        {
            float rand = UnityEngine.Random.value;
            foreach (RandomReward reward in possibleRewards)
            {
                if (reward.chance <= rand)
                {
                    lastRolledReward = reward.link;
                    return lastRolledReward;
                }
            }
            lastRolledReward = possibleRewards.FirstOrDefaultFast().link;
            return lastRolledReward;
        }
    }
}
