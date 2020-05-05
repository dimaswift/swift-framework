using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop.OfferSystem
{
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/Offer Triggers/Appear Handlers/Instant")]
    public class InstantAppearHandler : OfferAppearHandler
    {
        protected override void OnInit(IShopManager shop)
        {
            shouldBeOffered.SetValue(true);
        }
    }

}
