namespace SwiftFramework.Core
{
    public interface IWalletManager : IModule
    {
        IPromise Refresh();
        void ForceRefresh(int amount);
        IStatefulEvent<int> Credits { get; }
    }
}
