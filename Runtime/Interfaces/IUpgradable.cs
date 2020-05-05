namespace SwiftFramework.Core
{
    public interface IUpgradable
    {
        IUpgradeController UpgradeController { get; }
        IUpgradableState UpgradeState { get; }
    }
}
