using System;

namespace SwiftFramework.Core
{
    public interface IReward
    {
        bool IsAvailable();
        string GetDescription();
        BigNumber GetAmount();
        IPromise AddReward();
        SpriteLink Icon { get; }
        ViewLink ViewLink { get; }
    }

    [Serializable]
    [LinkFolder(Folders.Configs + "/Rewards")]
    public class RewardLink : LinkToScriptable<IReward>
    {
        public IPromise AddReward()
        {
            Promise promise = Promise.Create();
            Load().Then(r => r.AddReward().Channel(promise)).Catch(e => promise.Reject(e));
            return promise; 
        }
    }
}
