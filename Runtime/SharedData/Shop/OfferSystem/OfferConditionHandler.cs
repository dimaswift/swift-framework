namespace SwiftFramework.Core.SharedData.Shop.OfferSystem
{
    [PrewarmAsset]
    public abstract class OfferConditionHandler : OfferTriggerListener
    {
        public abstract bool AreConditionsMet();
    }

    [System.Serializable]
    [FlatHierarchy]
    public class OfferConditionHandlerLink : LinkToScriptable<OfferConditionHandler> { }
}
