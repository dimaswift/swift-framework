using UnityEngine;

namespace Swift.Core
{
    public interface ICloudStorage : IModule
    {
        IPromise<string> UploadFile(string sourcePath, string destinationPath, string contentType);
        IPromise DownloadConfig(ScriptableObject configToOverride);
        IPromise UploadConfig(ScriptableObject config);
        IPromise<byte[]> DownloadRaw(string path);
    }


    public static class ContentType
    {
        public const string Bundle = "application/x-gzip";
        public const string Texture = "image/jpeg";
        public const string Png = "image/png";
        public const string Text = "text/plain";
        public const string Json = "application/json";
    }
}
