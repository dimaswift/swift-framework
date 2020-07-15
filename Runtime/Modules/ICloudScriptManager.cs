using System;

namespace SwiftFramework.Core
{
    public interface ICloudScriptManager : IModule
    {
        IPromise<(T result, CloudScriptResponseCode code)> ExecuteCloudScript<T>(string method, object args, bool logStreamEvent = false, bool executeLatestRevision = false);
    }

    public enum CloudScriptResponseCode
    {
        OK = 200,
        CloudFunctionNotFound = 405,
        ScriptException = 500,
        BadRequest = 400,
        EmptyResponse = 404,
        InvalidJson = 501,
        NotLoggedIn = 403,
        NoInternet = 502
    }
    
    [Serializable]
    public struct CloudScriptResponse
    {
        public int code;
        public string error;
    }
}
