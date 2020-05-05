using SwiftFramework.Core.Boosters;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Boosters
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/BoosterReward")]
    public class BoosterReward : ScriptableObject, IReward
    {
        public BoosterConfigLink booster;
        public BigNumber amount;
        public SpriteLink Icon => booster.Value.icon;

        public IPromise AddReward()
        {
            Promise promise = Promise.Create();

            IIdleGameModule idleGame = App.Core.GetModule<IIdleGameModule>();

            idleGame.GetCurrentBoosterManager().AddBoosterToInventory(booster, (int)amount.Value);

            promise.Resolve();

            return promise; 
          
        }

        public string GetDescription()
        {
            return $"#{booster.GetName()}_shop_desc";
        }

        public virtual bool IsAvailable()
        {
            return true;
        }

        public BigNumber GetAmount() => amount;
    }
}
