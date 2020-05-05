using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Prices/RewardedAd")]
    public class RewardedAdPrice : BasePrice, IPrice
    {
        public event Action OnAvailabilityChanged = () => { };

        public bool CanAfford(BigNumber amount, float discount = 0)
        {
            IAdsManager adsManager = App.Core.GetModule<IAdsManager>();
            return adsManager != null && adsManager.IsRewardedReady();
        }

        public string GetPriceString(BigNumber amount, float discount = 0)
        {
            return "#watch_video";
        }

        public IPromise<bool> Pay(BigNumber amount, float discount = 0)
        {
            IAdsManager adsManager = App.Core.GetModule<IAdsManager>();

            if (adsManager == null)
            {
                return Promise<bool>.Resolved(false);
            }

            return adsManager.ShowRewardedWithLoadingWindow(name);
        }

        public IPromise<bool> Refund(BigNumber amount, float penalty = 1)
        {
            return Promise<bool>.Resolved(false);
        }
    }
}
