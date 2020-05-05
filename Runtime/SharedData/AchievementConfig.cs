using System;

namespace SwiftFramework.Core.SharedData
{
    [Serializable]
    public class AchievementConfig : ProgressItemConfig
    {
        public StepConfig[] steps = new StepConfig[] { new StepConfig() };
        public bool allowOvershoot;

        [Serializable]
        public class StepConfig
        {
            public int reward;
            public int startAmount;
            public int targetAmount;
        }

        public AchievementData CreateAchievement()
        {
            return new AchievementData()
            {
                id = id,
                currentStep = 0,
                steps = new AchievementData.Step[]
                {
                    new AchievementData.Step()
                    {
                        progress = steps[0].startAmount,
                        rewardClaimed = false
                    }
                }
            };
        }
    }
}
