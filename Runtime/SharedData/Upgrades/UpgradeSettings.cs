using System;

namespace SwiftFramework.Core.SharedData.Upgrades
{
    [Serializable]
    public class UpgradeSettings
    {
        public string upgrableName;
        public LevelBoost[] boostLevels;
        public float upgradeCostMultiplier;
        public SpriteLink icon;
        public FundsSourceLink fundsSource;
        public bool canUpgradeForAd;
        public long adUpgradesAmount;
        public BigNumber baseUpgradeCost = 10;
        public UpgradeIconsLink upgradeIcons;

        [Serializable]
        public struct LevelBoost
        {
            public long level;
            public RewardLink reward;
        }
    }
}
