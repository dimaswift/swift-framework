using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Prices/IAP/Subscription")]
    public class SubscriptionPrice : IAPPrice
    {
        [RegisterProduct(IAPProductType.Subscription)]
        [SerializeField] private string productId = null;

        public override string ProductId => productId;
    }
}
