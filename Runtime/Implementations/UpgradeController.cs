using SwiftFramework.Core;
using SwiftFramework.Core.SharedData.Upgrades;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{

    public abstract class UpgradeController<T, S> : IUpgradeController where T : IUpgradableState, IDeepCopy<T> where S : UpgradeSettings
    {
        public string UpgradableName => Settings.upgrableName;

        public event Action OnLeveledUp = () => { };

        public IStatefulEvent<bool> UpgradeAvailable => upgradeAvailabe;

        protected readonly StatefulEvent<bool> upgradeAvailabe = new StatefulEvent<bool>();

        public S Settings { get; }

        public UpgradeSettings UpgradeSettings => Settings;

        public IFundsSource Funds => Settings.fundsSource.Value;

        private readonly Func<float> discountHandler;

        protected readonly T state;

        public UpgradeController(S upgradeSettings, T state, Func<float> discountHandler)
        {
            this.state = state;
            this.discountHandler = discountHandler;
            Settings = upgradeSettings;
            Funds.Amount.OnValueChanged += Amount_OnValueChanged;
        }

        private void Amount_OnValueChanged(BigNumber value)
        {
            CheckUpgradeAvailability();
        }

        public void CheckUpgradeAvailability()
        {
            bool canLevelUp;

            if (!IsMaxLevel())
            {
                canLevelUp = CanAffordLevelUp();
            }
            else
            {
                canLevelUp = false;
            }

            upgradeAvailabe.SetValue(canLevelUp);
        }

        public RewardLink GetBoostReward()
        {
            RewardLink reward = null;
            foreach (var boost in UpgradeSettings.boostLevels)
            {
                if (state.Level < boost.level - 1)
                {
                    reward = boost.reward;
                    break;
                }
            }

            return reward;
        }

        public float GetBoostProgress(long count = 1)
        {
            long nextBoostLevel = GetNextBoostLevel();
            long previousBoostLevel = GetPreviousBoostLevel();

            return 1f - ((float)nextBoostLevel - (state.Level + count)) / (nextBoostLevel - previousBoostLevel);
        }

        protected abstract void OnLevelUp(T upgradable, long count, bool isCopy = false);

        public T GetUpgradedCopy(T upgradable, long count)
        {
            T upgradedCopy = upgradable.DeepCopy();
            OnLevelUp(upgradedCopy, count, true);
            return upgradedCopy;
        }

        protected abstract IEnumerable<ValueDelta> GetUpgradedValues(T upgradable, T upgradedCopy, long count);

        IUpgradableState IUpgradeController.GetUpgradedCopy(long count)
        {
            return GetUpgradedCopy((T)state, count);
        }

        IEnumerable<ValueDelta> IUpgradeController.GetUpgradedValues(long count)
        {
            foreach (var item in GetUpgradedValues(state, GetUpgradedCopy(state, count), count))
            {
                yield return item;
            }
        }

        public bool LevelUp(long count)
        {
            count = AdjustCountToLimit(count);
           
            if (Settings.fundsSource.Value.Take(GetUpgradePrice(count)))
            {
                ApplyLevelUp(count);
                return true;
            }
            return false;
        }

        public bool LevelUpForAd()
        {
            long count = AdjustCountToLimit(GetUpgradesAmountForAd());
            ApplyLevelUp(count);
            return true;
        }

        private void ApplyLevelUp(long count)
        {
            OnLevelUp(state, count);
            CheckUpgradeAvailability();
            OnLeveledUp();
        }

        public bool CanAffordLevelUp(long count = 1)
        {
            if (state.Level >= GetMaxLevel())
            {
                return false;
            }

            BigNumber cost = GetUpgradePrice(count);

            return cost <= Settings.fundsSource.Value.Amount.Value;
        }

        public virtual BigNumber GetUpgradePrice(long count = 1)
        {
            BigNumber cost = 0;

            count = AdjustCountToLimit(count);

            for (long i = 0; i < count; i++)
            {
                cost += GetCertainLevelCost(state, i);
            }

            return cost.Value - cost.Value.MultiplyByFloat(discountHandler());
        }

        protected virtual BigNumber GetBaseUpgradeCost() => Settings.baseUpgradeCost;

        protected virtual BigNumber GetCertainLevelCost(IUpgradableState upgradable, long count)
        {
            BigNumber multiplier = UpgradeSettings.upgradeCostMultiplier.BigNumberPow(upgradable.Level + count + 1);

            if (multiplier > 1000)
            {
                return GetBaseUpgradeCost().Value * multiplier.Value;
            }
            else
            {
                return GetBaseUpgradeCost().Value.MultiplyByFloat(Mathf.Pow(UpgradeSettings.upgradeCostMultiplier, upgradable.Level + count + 1));
            }
        }

        public long GetMaxPossibleUpgradeLevels()
        {
            long result = 0;
            BigNumber totalCredits = Settings.fundsSource.Value.Amount.Value;
            BigNumber cost = 0;
            long maxLevel = GetMaxLevel();

            while (maxLevel > 0 && maxLevel > state.Level + result)
            {
                totalCredits -= cost.Value;
                cost = GetCertainLevelCost(state, result);
                if (cost.Value == 0)
                {
                    break;
                }
                if (totalCredits - cost.Value <= 0)
                {
                    break;
                }
               
                result++;
            }

            if (result == 0)
                result = 1;

            return result;
        }

        public long AdjustCountToLimit(long count)
        {
            long maxLevel = GetMaxLevel();

            if (state.Level + count >= maxLevel)
            {
                count = maxLevel - state.Level;
            }
   
            return count;
        }

        public long GetNextBoostLevel()
        {
            long level = state.Level + 1;
            for (int i = 0; i < UpgradeSettings.boostLevels.Length; i++)
            {
                if (UpgradeSettings.boostLevels[i].level > level)
                {
                    return UpgradeSettings.boostLevels[i].level;
                }
            }
            return 0;
        }

        public long GetPreviousBoostLevel()
        {
            long level = state.Level + 1;
            for (int i = UpgradeSettings.boostLevels.Length - 1; i >= 0; i--)
            {
                if (UpgradeSettings.boostLevels[i].level <= level)
                {
                    return UpgradeSettings.boostLevels[i].level;
                }
            }
            return 0;
        }

        public bool IsMaxLevel()
        {
            return state.Level >= GetMaxLevel();
        }

        public long GetMaxLevel()
        {
            if (Settings.boostLevels.Length == 0 || Settings.boostLevels[Settings.boostLevels.Length - 1].level == 0)
            {
                return 1;
            }
            return Settings.boostLevels[Settings.boostLevels.Length - 1].level - 1;
        }

        public long GetUpgradesAmountForAd()
        {
            return Settings.adUpgradesAmount;
        }

        public float GetDiscount()
        {
            return discountHandler();
        }

        public bool CanUpgradeForAd()
        {
            return Settings.canUpgradeForAd;
        }
    }
}
