using SwiftFramework.Core.SharedData.Shop;
using SwiftFramework.Core.SharedData.Shop.OfferSystem;
using System;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface IShopManager : IModule
    {
        IPromise<TransactionResult> Buy(ShopItemLink item);
        bool IsPurchased(ShopItemLink item);
        void ClaimSubscriptionBonus(ShopItemLink item);
        bool CanClaimSubscriptionBonus(ShopItemLink item);
        bool IsSubscribed(ShopItemLink item);
        long GetSecondsTillNextSubsriptionBonus();
        int GetSubscriptionBonusesAmount(ShopItemLink item);
        event Action<ShopItemLink, TransactionResult> OnTransactionCompleted;
        IEnumerable<OfferTriggerLink> GetOfferTriggers();
        IOfferTriggerController OfferTriggers { get; }
    }

    public enum TransactionResult
    {
        Success, InsufficientFunds, Error
    }
}
