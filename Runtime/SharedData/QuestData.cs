using SwiftFramework.Core;
using System;

namespace SwiftFramework.Core.SharedData
{
    [Serializable]
    public class QuestData : IProgressData
    {
        public long unlockTimestamp;
        public string id;
        public bool rewardClaimed;
        public int progress;
        public int completedTimesAmount;

        public string GetId() => id;

        public (int current, int target) GetProgress(ProgressItemConfig config)
        {
            QuestConfig questConfig = config as QuestConfig;
            return (progress, questConfig.targetAmount);
        }

        public float GetProgressNormalized(ProgressItemConfig config)
        {
            var p = GetProgress(config);
            return (float)p.current / p.target;
        }

        public int GetReward(ProgressItemConfig config)
        {
            QuestConfig questConfig = config as QuestConfig;
            return questConfig.reward;
        }

        public ProgressItemStatus GetStatus(ProgressItemConfig config)
        {
            QuestConfig questConfig = config as QuestConfig;

            if (rewardClaimed)
            {
                if(questConfig.repeatable == false)
                {
                    return ProgressItemStatus.Rewarded;
                }

                rewardClaimed = false;

                return ProgressItemStatus.Active;
            }


            if (unlockTimestamp != 0 && unlockTimestamp - App.Core.Clock.Now.Value >= 0)
            {
                return ProgressItemStatus.OnCooldown;
            }

            if (progress >= questConfig.targetAmount)
            {
                return ProgressItemStatus.Completed;
            }
             
            return ProgressItemStatus.Active;
        }
    }
}
