using System;

namespace SwiftFramework.Core
{
    public interface IIdleGameModule : IModule
    {
        T GetState<T>() where T : class;
        long FirstSessionTimestamp { get; }
        event Action OnSeasonEventFinished;
        event Action<ILink> OnZoneUnlocked;
        event Action OnZoneChanged;
        event Action OnGameCreated;
        event Action<ILink> OnOfflineRevenueClaimed;
        IFundsSource SoftFunds { get; }
        IFundsSource HardFunds { get; }
        FracturedBigNumber ProductionSpeed { get; }
        IBoosterManager GetCurrentBoosterManager();
        IPromise SetActiveZone(ILink zoneLink);
        IPromise ReloadActiveZone();
        bool IsZoneUnlocked(ILink zoneLink);
        bool TrySetSeasonEventActive();
        (BigNumber amount, FundsSourceLink funds, long duration) GetOfflineRevenue(ILink zoneLink, bool ignoreActiveZone);
        (BigNumber amount, FundsSourceLink funds) GetWorldOfflineRevenue(ILink worldLink);
        long GetZoneRevenueMultiplier(ILink zoneLink);
        void ClaimOfflineRevenue(ILink zoneLink, long multiplier);
        void ClaimWorldOfflineRevenue(ILink worldLink, long multiplier);
        ILink GetActiveWorld();
        ILink GetActiveZone();
        IPromise<bool> UnlockZone(ILink zoneLink);
        T GetGameController<T>() where T : class;
        ISharedSupervisorsManager SharedSupervisors { get; }
        IPromise<long> TryShowCurrentZoneOfflineRevenue();
        bool IsSeasonEventZoneActive();
        bool IsPrestigeAvailableForCurrentZone();
        IStageManager StageManager { get; }
        IPromise OpenSeasonPass();
        IPromise OpenMap();
        IPromise OpenGameStage();
    }
}
