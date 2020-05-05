using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Prices/Free")]
    public class FreePrice : BasePrice, IPrice
    {
        public event Action OnAvailabilityChanged = () => { };

        public bool CanAfford(BigNumber amount, float discount = 0)
        {
            return true;
        }

        public string GetPriceString(BigNumber amount, float discount = 0)
        {
            return App.Core.Local.GetText("#free");
        }

        public IPromise<bool> Pay(BigNumber amount, float discount = 0)
        {
            return Promise<bool>.Resolved(true);
        }

        public IPromise<bool> Refund(BigNumber amount, float penalty = 1f)
        {
            return Promise<bool>.Resolved(true);
        }
    }
}
