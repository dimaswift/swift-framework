using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop.OfferSystem
{
    [PrewarmAsset]
    public abstract class OfferTriggerListener : ScriptableObject 
    {
        [NonSerialized] protected bool initialized;

        public void Init(IShopManager shop)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            OnInit(shop);
        }

        protected abstract void OnInit(IShopManager shop);

        public virtual void OnOfferTriggered(OfferTrigger trigger) { }
        public virtual void OnOfferPurchased(OfferTrigger trigger) { }
    }
}
