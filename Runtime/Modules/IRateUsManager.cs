namespace SwiftFramework.Core
{
    public interface IRateUsManager : IModule
    {
        IPromise<bool> TryToRateUs();
    }
}