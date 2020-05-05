using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [PrewarmAsset]
    public abstract class IAPPrice : BasePrice, IPrice
    {
        public abstract string ProductId { get; }

        public event Action OnAvailabilityChanged = () => { };

        public bool CanAfford(BigNumber amount, float discount = 0)
        {
            IInAppPurchaseManager iap = App.Core.GetModule<IInAppPurchaseManager>();
            if (iap == null)
            {
                return false;
            }
            return true;
        }

        public string GetPriceString(BigNumber amount, float discount = 0)
        {
            IInAppPurchaseManager iap = App.Core.GetModule<IInAppPurchaseManager>();
            if (iap == null)
            {
                return "#not_available";
            }
            return iap.GetPriceString(ProductId);
        }

        public IPromise<bool> Pay(BigNumber amount, float discount = 0)
        {
            IInAppPurchaseManager iap = App.Core.GetModule<IInAppPurchaseManager>();

            Promise<bool> promise = Promise<bool>.Create();

            if (iap == null)
            {
                Debug.LogError($"IInAppPurchaseManager not found. Cannot pay with IAPPrice");
                promise.Resolve(false);
                return promise;
            }

            return iap.Buy(ProductId);
        }

        public IPromise<bool> Refund(BigNumber amount, float penalty = 1)
        {
            Debug.LogError($"IAP refunds are not implemented yet"); 
            return Promise<bool>.Resolved(false);
        }
    }
}
