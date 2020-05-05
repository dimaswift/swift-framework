using System;

namespace SwiftFramework.Core
{
    public interface IInAppPurchaseManager : IModule
    {
        IStatefulEvent<bool> PurchasingInitialized { get; }
        IPromise<bool> Buy(string productId);
        IPromise<bool> RestorePurchases();
        string GetPriceString(string productId);
        string GetCurrencyCode(string productId);
        decimal GetPrice(string productId);
        bool IsSubscribed(string productId);
        SubscriptionData GetSubscriptionData(string productId);
        bool IsAlreadyPurchased(string productId);

        event Action<string> OnItemPurchased;
    }

    public enum IAPProductType
    {
        Consumable = 0, NonConsumable = 1, Subscription = 2
    }

    [Serializable]
    public struct SubscriptionData
    {
        public string productId;
        public bool isSubscribed;
        public bool isFreeTrial;
        public DateTime purchaseDate;
        public DateTime expirationDate;
        public TimeSpan remainingTime;
        public bool isCanceled;
    }

    public class RegisterProductAttribute : Attribute
    {
        public IAPProductType type;

        public RegisterProductAttribute(IAPProductType type)
        {
            this.type = type;
        }
    }
}
