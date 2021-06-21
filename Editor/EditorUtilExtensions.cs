using System.IO;
using System.Text;
using Swift.Core;
using Swift.Core.Editor;
using Swift.Core.SharedData;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Swift.EditorUtils
{
    public static class EditorUtilExtensions
    {
        public static void MoveToCenter(this EditorWindow window)
        {
            Rect position = window.position;
           
            Rect newRect = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height);
          
            position.center = newRect.center;
            window.position = position;
            window.Show();
        }

        public static AssemblyDefinition GetData(this AssemblyDefinitionAsset asset)
        {
            string text = Encoding.UTF8.GetString(asset.bytes);
            return JsonUtility.FromJson<AssemblyDefinition>(text);
        }
        
        public static void ShowCompileAndPlayModeWarning(this EditorWindow window, out bool canEdit)
        {
            if (EditorApplication.isCompiling)
            {
                GUI.Label(new Rect(0, 0, window.position.width, window.position.height), $"Cannot modify. Compiling scripts...", EditorStyles.centeredGreyMiniLabel);
                canEdit = false;
                return;
            }
            
            if (EditorApplication.isPlaying)
            {
                GUI.Label(new Rect(0, 0, window.position.width, window.position.height), $"Cannot modify in Play Mode", EditorStyles.centeredGreyMiniLabel);
                canEdit = false;
                return;
            }

            canEdit = true;
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