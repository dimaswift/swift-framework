namespace SwiftFramework.Core
{
    public interface IAnalyticsManager : IModule
    {
        void LogEvent(string eventName, params (string key, object value)[] args);
        void LogPurchase(string productId, string currencyCode, decimal price, string transactionId);
        void LogFirstPurchase(string productId);
        void LogRewardedVideoStarted(string placementId);
        void LogFirstAdWatched(string placementId);
        void LogRewardedVideoSuccess(string placementId);
        void LogRewardedVideoAttemptToShow(string placementId);
        void LogInterstitialWatched();
        void LogRewardedVideoError(string placementId, string errorType, string errorCode);
        void LogLevelUp(int level);
    }
}
