using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop.OfferSystem
{
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/Offer Triggers/Conditions/Active Offers")]
    public class ActiveOffersCondition : OfferConditionHandler
    {
        [SerializeField] private int maxActiveOffersAmount = 3;

        private IShopManager shop;

        public override bool AreConditionsMet()
        {
            return shop.OfferTriggers.GetActiveOffersAmount() < maxActiveOffersAmount;
        }

        protected override void OnInit(IShopManager shop)
        {
            this.shop = shop;
        }
    }
}
