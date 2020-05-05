using SwiftFramework.Core.Supervisors;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Boosters
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/SupervisorReward")]
    public class SupervisorReward : ScriptableObject, IReward
    {
        public SupervisorLink supervisor;
        public SpriteLink Icon => supervisor.Value.skin.Value.icon;

        public IPromise AddReward()
        {
            Promise promise = Promise.Create();

            IIdleGameModule idleGame = App.Core.GetModule<IIdleGameModule>();

            idleGame.SharedSupervisors.Acquire(supervisor);

            promise.Resolve();

            return promise;

        }

        public string GetDescription()
        {
            return $"#{supervisor.GetName()}_shop_desc";
        }

        public virtual bool IsAvailable()
        {
            return true;
        }

        public BigNumber GetAmount() => 1;
    }
}
