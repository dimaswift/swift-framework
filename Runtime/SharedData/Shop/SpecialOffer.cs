using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop
{
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/Special Offer")]
    public class SpecialOffer : ShopItem, ISpecialOfferTag
    {
        [LinkFilter(typeof(ISpecialOfferTag))] public WindowLink window;
    }

    public interface ISpecialOfferTag
    {

    }
}
