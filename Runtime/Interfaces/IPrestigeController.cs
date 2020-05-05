using System;

namespace SwiftFramework.Core
{
    public interface IPrestigeController
    {
        (float multiplier, PriceLink price, int level) GetNextPrestige();
        (float multiplier, PriceLink price, int level) GetCurrentPrestige();

        int GetMaxLevel();
        IPromise<bool> UpgradePrestige();
        IStatefulEvent<bool> IsAvailable { get; }
        event Action OnPrestigeChanged;
        event Action OnBecameAvailableForTheFirstTime;
        bool ShouldBeShown();
    }
}