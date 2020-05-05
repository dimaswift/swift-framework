using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop.OfferSystem
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/Offer Trigger")]
    public class OfferTrigger : ScriptableObject
    {
        [LinkTypeFilter(typeof(SpecialOffer))] public ShopItemLink offer;
        public UnixDuration minTimeSinceFirstSession = 0;

        public OfferAppearHandlerLink appearHandler;
        [Header("Custom appear conditions")]
        public OfferConditionHandlerLink[] conditions;


        [Header("Limits")]
        public bool limited;
        public UnixDuration duration;

        [Header("Lifetime")]
        public bool repeat;
        public UnixDuration repeatAfterTime;
        public bool retireAfterPurchase;
        public bool showOfferWindowOnTrigger;

        public IEnumerable<OfferTriggerListener> GetListeners()
        {
            if (appearHandler.HasValue)
            {
                yield return appearHandler.Value;
            }

            foreach (OfferConditionHandlerLink conditionLink in conditions)
            {
                yield return conditionLink.Value;
            }
        }
    }

    [System.Serializable]
    public class OfferTriggerLink : LinkToScriptable<OfferTrigger>
    {

    }
}
