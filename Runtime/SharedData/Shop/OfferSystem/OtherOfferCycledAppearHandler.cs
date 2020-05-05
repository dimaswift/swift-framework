using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop.OfferSystem
{
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/Offer Triggers/Appear Handlers/After Other Offer Cycled")]
    public class OtherOfferCycledAppearHandler : OfferAppearHandler
    {
        [SerializeField] private OfferTriggerLink specificItem = null;

        protected override void OnInit(IShopManager shop)
        {
            shop.OfferTriggers.OnOfferExpired += OfferTriggers_OnOfferExpired;
        }

        private void OfferTriggers_OnOfferExpired(OfferTriggerLink item)
        {
            if (specificItem.HasValue == false || item == specificItem)
            {
                shouldBeOffered.SetValue(true);
            }
        }
    }

}
