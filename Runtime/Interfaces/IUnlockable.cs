namespace SwiftFramework.Core
{
    public enum UnlockStatus { Unlocked, IsUnlocking, Locked, JustUnlocked }

    public interface IUnlocker
    {
        PriceLink UnlockPrice { get; }
        PriceLink SkipUnlockPrice { get; }
        PriceLink ReduceUnlockTimePrice { get; }

        IStatefulEvent<UnlockStatus> UnlockStatus { get; }
        IPromise<bool> Unlock();
        IPromise<bool> SkipUnlockWaitTime();
        IPromise<bool> ReduceUnlockWaitTime();
        void CompleteUnlock();
        long GetTimeTillUnlock();
        float GetTimeReductionDiscount();
        float GetSkipUnlockDiscount();
        float GetUnlockDiscount();
        float UnlockTimeTotal { get; }
        long UnlockTimeToReduce { get; }
    }

    public interface IUnlockable
    {
        IUnlocker Unlocker { get; }
    }
}
