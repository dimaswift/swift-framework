using SwiftFramework.Core;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Funds
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Shop/FundsReward")]
    public class FundsReward : ScriptableObject, IReward
    {
        public FundsSourceLink source;
        public BigNumber amount;

        public SpriteLink Icon => source.Value.Icon;

        protected virtual BigNumber GetRewardAmount()
        {
            return amount;
        }

        public IPromise AddReward()
        {
            return source.Load().Then(a => a.Add(GetRewardAmount()));
        }

        public string GetDescription()
        {
            return GetRewardAmount().ToString();
        }

        public virtual bool IsAvailable()
        {
            return true;
        }

        public BigNumber GetAmount()
        {
            return GetRewardAmount();
        }
    }

}
