using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public interface ICloudAuthentication : IModule
    {
        event Action<string> OnAppRedirectionReceived;
        event Action OnLogin;
        event Action OnLoginAttempt;
        event Action OnLogout;
        event Action OnUserOpenedWebInterface;
        event Action<ICloudProfile> OnProfileChanged;
        bool LoggedIn { get; }
        IStatefulEvent<bool> Connected { get; }

        IPromise<SetDisplayNameResult> SetDisplayName(string name);
        ICloudProfile Profile { get; }
        IPromise<CloudActionResult> Login();
        IPromise<CloudActionResult> LoginWithDeviceId();
        IPromise UploadData(string key, string data);
        IPromise<string> GetData(string key);
        IPromise<T> DownloadSaveData<T>() where T : ICloudSave;
        IPromise<ICloudSave> DownloadSaveData(Type type);
        IPromise UploadSaveData<T>(T data);
        IPromise<T> GetSaveData<T>(Func<T> defaultSaveData) where T : ICloudSave;
        void CheckLogin<T>(Action callback, Promise<T> promiseToReject);
        void Logout();
        void ProcessExternalLogin(object payload, Promise<CloudActionResult> promise);
        void NotifyAboutDisconnect();

        void OpenWebInterface();
    }

    public interface ICloudProfile
    {
        string DisplayName { get; }
        FacebookProfile FacebookProfile { get; }
        IPromise<Texture2D> Avatar { get; }
        void UnlinkFacebookProfile();
        bool LinkedToFacebook();
        void LinkFacebookProfile(FacebookProfile facebookProfile);
    }

    public enum CloudActionResult
    {
        Success = 0, AccountNotFound = 1, ServiceUnavailable = 2, UnknownError = 3, Timeout = 4, NewAccountCreated = 5
    }

    public enum SetDisplayNameResult
    {
        Success = 0, InvalidName = 1, UnknownError = 2, Timeout = 3
    }
}
