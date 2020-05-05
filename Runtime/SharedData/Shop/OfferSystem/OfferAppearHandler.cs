namespace SwiftFramework.Core.SharedData.Shop.OfferSystem
{
    public abstract class OfferAppearHandler : OfferTriggerListener
    {
        public IStatefulEvent<bool> ShouldBeOffered => shouldBeOffered;

        protected readonly StatefulEvent<bool> shouldBeOffered = new StatefulEvent<bool>();

        public override void OnOfferTriggered(OfferTrigger trigger) { }
        public override void OnOfferPurchased(OfferTrigger trigger) { }
    }

    [System.Serializable]
    [FlatHierarchy]
    public class OfferAppearHandlerLink : LinkToScriptable<OfferAppearHandler>
    {

    }
}
