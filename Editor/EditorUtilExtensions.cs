using System.IO;
using SwiftFramework.Core;
using SwiftFramework.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    public static class EditorUtilExtensions
    {
        public static void MoveToCenter(this EditorWindow window)
        {
            var position = window.position;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            window.position = position;
            window.Show();
        }

        public static string FromRelativeResourcesPathToAbsoluteProjectPath(this string path)
        {
            return string.IsNullOrEmpty(path) ? "Assets/Resources" : $"Assets/Resources/{path}";
        }

        public static string ToRelativeResourcesPath(this string path)
        {
            if (path.Contains("/Resources/") == false)
            {
                return "";
            }

            FileInfo file = new FileInfo(path);
            string result = file.Directory?.Name;

            if (result == "Resources")
            {
                return "";
            }

            DirectoryInfo current = file.Directory?.Parent;
            while (current != null && current.Name != "Resources")
            {
                result = $"{current.Name}/{result}";
                current = current.Parent;
            }

            return result;
        }

        public static T Value<T>(this LinkTo<T> link) where T : Object
        {
#if USE_ADDRESSABLES
            return AddrHelper.GetAsset<T>(link);
#else
            return link.Value;
#endif
        }
    }
}