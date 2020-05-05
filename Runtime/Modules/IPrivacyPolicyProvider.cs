namespace SwiftFramework.Core
{
    public interface IPrivacyPolicyProvider : IModule
    {
        bool IsInCompliance { get; }
        IPromise<bool> Comply();
    }
}
