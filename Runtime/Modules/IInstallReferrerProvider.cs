using System;

namespace SwiftFramework.Core
{
    public interface IInstallReferrerProvider : IModule
    {
        IPromise<(string referrer, DateTime clickDate, DateTime installDate)> GetReferrer();
    }
}
