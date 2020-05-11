using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [BuiltInModule]
    [ModuleGroup(ModuleGroups.Core)]
    public interface INetworkManager : IModule
    {
        IPromise<string> Get(string url, int timeoutSeconds = 5);
        IPromise<Texture2D> DownloadImage(string url);
        IPromise<byte[]> DownloadRaw(string url, Action<long> progressBytes = null);
    }
}
