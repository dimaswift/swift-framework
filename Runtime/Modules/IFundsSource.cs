using System;

namespace SwiftFramework.Core
{
    [PrewarmAsset]
    public interface IFundsSource : ILinked, IInventoryItem
    {
        IPromise<bool> Withdraw(BigNumber amount);
    }

    [Serializable]
    [LinkFolder(Folders.Configs + "/Funds")]
    public class FundsSourceLink : LinkToScriptable<IFundsSource>
    {

    }
}