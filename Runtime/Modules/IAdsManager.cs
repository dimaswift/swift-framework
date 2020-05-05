using System;

namespace SwiftFramework.Core
{
    public interface IAdsManager : IModule
    {
        IStatefulEvent<bool> BannerShown { get; }
        float GetTimeSinceAdClosed();
        bool IsShowingRewardedAd();
        event Action<string> OnRewardedShowStarted;
        event Action OnRewardedAdLoaded;
        event Action<string> OnRewardedSuccessfully;
        event Action<string> OnRewardedClosed;
        event Action<string> OnRewardedAttemptToShow;
        event Action<RewardedAdErrorArgs> OnRewardedError;
        bool IsRewardedReady();
        void SetBannerShown(bool shown);
        IPromise<bool> ShowRewardedWithLoadingWindow(string placementId);
        IPromise<RewardedShowResult> ShowRewarded(string placementId);
        bool ShowInterstitial(string placementId);
        bool IsInterstitialReady();
        void CancelRewardedShow();
        bool TryShowInterstitial();
        void ResetInterstitialCounter();
        bool IsBannerEnabled { get; }
    }

    public enum RewardedShowResult
    {
        Success = 0, NotReady = 1, Canceled = 2, Error = 3
    }

    public enum AdBannerPosition
    {
        Top, Bottom
    }

    public struct RewardedAdErrorArgs
    {
        public string placementId;
        public string errorType;
        public string errorMessage;
    }
}
