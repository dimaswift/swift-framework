using System;
using System.Collections.Generic;
using SwiftFramework.Core.Boosters;

namespace SwiftFramework.Core
{
    public interface IBoosterManager
    {
        BoostersState State { get; }
        BoosterManagerConfig Config { get; }
        event Action<Booster> OnBoosterActivated;
        event Action<Booster> OnBoosterExpired;
        event Action<Booster> OnBoosterDeactivated;
        event Action OnMultipliersUpdated;
        event Action OnInventoryChanged;
        event Action OnBoosterAddedToInventory;
        long GetTotalMultiplier(BoosterTargetLink target);
        long GetTotalMultiplier(BoosterTargetLink target, Link context);
        long GetActiveBoostersAmount(BoosterConfigLink configLink);
        long GetActiveBoostersAmount(BoosterTargetLink target);
        long GetTotalSecondsLeft(BoosterConfigLink configLink);
        long GetTotalSecondsLeft(BoosterTargetLink target);
        long GetExpirationTimeStamp(BoosterConfigLink configLink);
        bool IsExpired(BoosterConfigLink booster);
        bool IsActive(BoosterConfigLink configLink);
        void ActivateBooster(BoosterConfigLink config, int amount = 1, Link context = null);
        bool TryDeactivateBooster(BoosterConfigLink config);
        BoosterConfigLink GenerateBooster(BoosterTemplateLink template);
        void AddBoosterToInventory(BoosterConfigLink config, int amount, Link context = null);
        void ForceExpire(BoosterTargetLink target, long count);
        void ForceExpire(BoosterConfigLink config, Link context = null);
        void CheckExpiredBoosters();
        IEnumerable<Booster> GetActiveBoosters(BoosterType type, BoosterTargetLink target);
        IEnumerable<Booster> GetActiveBoosters(BoosterType type, BoosterOperation operation, BoosterTargetLink target);
        IEnumerable<Booster> GetActiveBoostersAllOfType(BoosterType type);
        IEnumerable<(BoosterConfigLink link, int amount)> GetBoostersInInventory();
        int GetBoosterAmountInInventory(BoosterConfigLink link);
        BoosterConfig TryGetConfig(BoosterConfigLink configLink);
    }
}