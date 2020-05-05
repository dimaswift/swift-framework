using System;

namespace SwiftFramework.Core
{
    public interface IFacebookCloudAuthentication : IModule
    { 
        FacebookProfile Profile { get; }
        IPromise<FacebookProfile> LinkAccount();
        IPromise UnlinkAccount();
        IPromise<CloudActionResult> Login();
        bool IsLinked();
        event Action OnLogin;
    }
}
