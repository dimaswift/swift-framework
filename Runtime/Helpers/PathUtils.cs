using System.IO;
using UnityEngine;

namespace SwiftFramework.Helpers
{
    public static class PathUtils
    {
        public static string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return "";
            }

            if (Path.GetFullPath(absolutePath).StartsWith(Path.GetFullPath(Application.dataPath)))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }

            return absolutePath;
        }
    }
}