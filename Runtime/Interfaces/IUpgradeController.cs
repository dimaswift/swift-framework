using SwiftFramework.Core.SharedData.Upgrades;
using System;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface IUpgradeController
    {
        string UpgradableName { get; }

        IFundsSource Funds { get; }

        IStatefulEvent<bool> UpgradeAvailable { get; }

        event Action OnLeveledUp;

        bool CanAffordLevelUp(long count);

        bool LevelUp(long count);

        bool LevelUpForAd();

        IUpgradableState GetUpgradedCopy(long count);

        IEnumerable<ValueDelta> GetUpgradedValues(long count);

        BigNumber GetUpgradePrice(long count = 1);

        RewardLink GetBoostReward();

        long GetMaxPossibleUpgradeLevels();

        long GetNextBoostLevel();

        long AdjustCountToLimit(long count);

        UpgradeSettings UpgradeSettings { get; }

        float GetBoostProgress(long offset = 1);

        bool IsMaxLevel();

        long GetMaxLevel();

        void CheckUpgradeAvailability();

        long GetUpgradesAmountForAd();

        float GetDiscount();

        bool CanUpgradeForAd();

    }
}
