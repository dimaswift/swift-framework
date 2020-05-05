using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public interface IFacebookProfile : IModule
    {
        event Action<FacebookProfile> OnLogin;
        FacebookProfile Profile { get; }
        IPromise<FacebookAuthResult> Login();
        void Logout();
    }

    public class FacebookAuthResult
    {
        public string Error { get; set; }
        public bool Success { get; set; }
        public FacebookProfile Profile { get; set; }
    }

    public class FacebookProfile
    {
        public string AccessToken { get; set; }
        public string Id { get; set; }
        public IPromise<string> Email { get; set;}
        public IPromise<string> Name { get; set;}
        public IPromise<Texture2D> Avatar { get; set; }
    }
}
