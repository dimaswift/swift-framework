using SwiftFramework.Core.SharedData.Shop;
using SwiftFramework.Core.SharedData.Shop.OfferSystem;
using System;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface IOfferTriggerController
    {
        event Action<(OfferTriggerLink trigger, long expireTimestamp)> OnOfferTriggered;
        event Action<OfferTriggerLink> OnOfferExpired;
        int GetActiveOffersAmount();
        IEnumerable<(ShopItemLink offer, long expireTimestamp)> GetActiveOffers();
        bool TryGetOfferState(OfferTriggerLink offerTriggerLink, out long expireTimestamp);
    }
}