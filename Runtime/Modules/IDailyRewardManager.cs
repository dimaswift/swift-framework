namespace SwiftFramework.Core
{
    public interface IDailyRewardManager : IModule
    {
        IPromise<bool> TryShowRewardWindow();
        void ClaimReward();
    }
}