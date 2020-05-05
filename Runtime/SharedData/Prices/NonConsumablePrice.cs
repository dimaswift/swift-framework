using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Prices/IAP/NonConsumable")]
    public class NonConsumablePrice : IAPPrice
    {
        [RegisterProduct(IAPProductType.NonConsumable)]
        [SerializeField] private string productId = null;

        public override string ProductId => productId;
    }
}
