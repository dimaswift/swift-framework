using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData
{
    [Serializable]
    public class AchievementData : IProgressData
    {
        public Step[] steps;
        public int currentStep;
        public string id;

        [Serializable]
        public class Step
        {
            public bool rewardClaimed;
            public int progress;
        }

        public Step GetCurrentStep() => steps[currentStep];

        public string GetId() => id;

        public ProgressItemStatus GetStatus(ProgressItemConfig config)
        {
            AchievementConfig achievementConfig = config as AchievementConfig;

            if (currentStep >= achievementConfig.steps.Length)
            {
                return ProgressItemStatus.Rewarded;
            }

            if (GetCurrentStep().rewardClaimed)
            {
                return ProgressItemStatus.Rewarded;
            }

            if (GetCurrentStep().progress >= achievementConfig.steps[currentStep].targetAmount)
            {
                return ProgressItemStatus.Completed;
            }

            return ProgressItemStatus.Active;
        }

        public (int current, int target) GetProgress(ProgressItemConfig config)
        {
            AchievementConfig achievementConfig = config as AchievementConfig;
            if (currentStep >= steps.Length || currentStep >= achievementConfig.steps.Length)
            {
                int max = achievementConfig.steps[achievementConfig.steps.Length - 1].targetAmount;
                return (max, max);
            }
            return (steps[currentStep].progress, achievementConfig.steps[currentStep].targetAmount);
        }

        public int GetReward(ProgressItemConfig config)
        {
            AchievementConfig achievementConfig = config as AchievementConfig;
            if (currentStep >= steps.Length || currentStep >= achievementConfig.steps.Length)
            {
                return 0;
            }
            return achievementConfig.steps[currentStep].reward;
        }

        public float GetProgressNormalized(ProgressItemConfig config)
        {
            AchievementConfig achievementConfig = config as AchievementConfig;
            if (currentStep >= steps.Length || currentStep >= achievementConfig.steps.Length)
            {
                return 1;
            }
            return Mathf.InverseLerp(achievementConfig.steps[currentStep].startAmount, achievementConfig.steps[currentStep].targetAmount, steps[currentStep].progress);
        }
    }

}
