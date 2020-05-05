using System.IO;
using UnityEngine;

namespace SwiftFramework.Core
{
    public static class FileHelper
    {
        public static string GetStreamingAssetsPath(string fileName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine("jar:file://" + Application.dataPath + "/!assets/", fileName);
#elif !UNITY_IOS && !UNITY_EDITOR
            return Application.dataPath + "/Raw/" + fileName;
#else
            return Path.Combine(Application.dataPath, "StreamingAssets", fileName);
#endif
        }

    }
}
