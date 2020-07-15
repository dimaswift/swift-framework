using SwiftFramework.Core;
using UnityEngine;

namespace SwiftFramework.Helpers
{
    public static class MobileUtils
    {
        public static IPromise<string> GetAdvertisingId()
        {
            Promise<string> result = Promise<string>.Create();

            if (Application.isEditor)
            {
                result.Resolve(SystemInfo.deviceUniqueIdentifier);
                return result;
            }

            Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) =>
            {
                if (string.IsNullOrEmpty(advertisingId) == false)
                {
                    result.Resolve(advertisingId);
                }
                else
                {
                    Debug.LogError("Cannot get Advertising Identifier: " + error);
                    result.Resolve(SystemInfo.deviceUniqueIdentifier);
                }
            });
            Promise<string> timeout = Promise<string>.Create();
            App.Core.Timer.WaitForUnscaled(5).Done(() => timeout.Resolve(SystemInfo.deviceUniqueIdentifier));
            return Promise<string>.Race(result, timeout);
        }
    }
}