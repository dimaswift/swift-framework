using SwiftFramework.Core;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Prices/IAP/Consumable")]
    public class ConsumablePrice : IAPPrice
    {
        [RegisterProduct(IAPProductType.Consumable)]
        [SerializeField] private string productId = null;

        public override string ProductId => productId;
    }
}
