using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Runtime.Serialization;
using SwiftFramework.Helpers;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core
{
    [Serializable]
    public class Link : ILink, ISerializationCallbackReceiver
    {

        public static IEnumerable<TLink> PopulateLinkList<TLink, TAsset>(bool promptFolderSelection = true, string defaultFolder = "") where TAsset : Object where TLink : Link, new()
        {
#if UNITY_EDITOR
            string folder = GetFolder(promptFolderSelection, defaultFolder);

            if (string.IsNullOrEmpty(folder))
            {
                yield break;
            }

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}", new [] { folder }))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                TAsset asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TAsset>(path);
                yield return Create<TLink>(folder.Remove(0, GetRootFolder().Length + 1) + "/" + asset.name);
            }
            
#else

            yield break;

#endif
            
        }

        private static string GetFolder(bool promptFolderSelection = true, string defaultFolder = "")
        {
            string folder = GetRootFolder();

            if (promptFolderSelection)
            {
                folder = UnityEditor.EditorUtility.OpenFolderPanel("Choose folder to populate link list",
                    $"Assets/{GetRootFolder()}/{defaultFolder}", $"");
            }

            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            return PathUtils.ToRelativePath(folder);
        }
        
        public static IEnumerable<TLink> PopulatePrefabLinkList<TLink, TAsset>(bool promptFolderSelection = true, string defaultFolder = "") where TLink : Link, new()
        {
#if UNITY_EDITOR

            string folder = GetFolder(promptFolderSelection, defaultFolder);

            if (string.IsNullOrEmpty(folder))
            {
                yield break;
            }
            
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:GameObject", new [] { folder }))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponent<TAsset>() != null)
                {
                    path = path.RemoveExtention();
                    yield return Create<TLink>(path.Substring(folder.Length + 1, path.Length - (folder.Length + 1)));
                }
            }
#else

            yield break;

#endif
        }
        
        private static string GetRootFolder()
        {
#if USE_ADDRESSABLES
            return "Assets/Addressables";
#else
            return "Assets/Resources";
#endif
        }

        private const string GENERATED = "generated_";

        public const string PathPropertyName = nameof(Path);

        public virtual bool HasValue => Path != NULL;

        public bool IsEmpty => string.IsNullOrEmpty(Path) || Path == NULL;

        [NonSerialized] protected int cachedHash = 0;

        [HideInInspector] [SerializeField] protected string Path = NULL;

        public const string NULL = "null";

        public bool IsGenerated()
        {
            return Path.StartsWith(GENERATED);
        }

        public L Copy<L>() where L : Link, new()
        {
            return Create<L>(GetPath());
        }

        public virtual IPromise Preload()
        {
            return Promise.Resolved();
        }

        public override bool Equals(object obj)
        {
            if(obj is Link == false)
            {
                return false;
            }
            return ((Link)obj).GetHashCode() == GetHashCode();
        }
        
        public virtual void Reset()
        {
            cachedHash = 0;
        }

        public override int GetHashCode()
        {
            if (cachedHash == 0)
            {
#if UNITY_EDITOR
                App.OnDomainReloaded += AppOnOnDomainReloaded;
#endif
                cachedHash = Path.GetHashCode();
            }
            return cachedHash;
        }

        private void AppOnOnDomainReloaded()
        {
            Reset();
        }

        public static T Create<T>(string path) where T : Link, new()
        {
            return new T() { Path = path };
        }
        
        public static T Create<T>(string folder, UnityEngine.Object obj) where T : Link, new()
        {
            string root = GetLinkFolder(typeof(T));
            if (string.IsNullOrEmpty(root) == false)
            {
                root += "/";
            }
            if (string.IsNullOrEmpty(folder) == false)
            {
                root += folder + "/";
            }
            return new T() { Path = $"{root}{obj.name}" };
        }

        public static T Generate<T>() where T : Link, new()
        {
            return new T() { Path = $"{GENERATED}{Guid.NewGuid().ToString()}" };
        }

        public static T CreateNull<T>() where T : Link, new()
        {
            return new T() { Path = NULL };
        }

        public virtual string GetPath() { return Path; }

        public virtual void OnBeforeSerialize()
        {

        }

        public static string GetLinkFolder(Type linkType)
        {
            foreach (var a in linkType.GetCustomAttributes<LinkFolderAttribute>())
            {
                return a.folder;
            }
            return "";
        }

        public override string ToString()
        {
            return Path.Replace('/', '-').Replace('\\', '-');
        }

        public string GetName()
        {
            return System.IO.Path.GetFileName(Path);
        }

        public virtual void OnAfterDeserialize()
        {
            Reset();
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            Reset();
        }

        public static bool operator == (Link a, Link b)
        {
            return a?.GetHashCode() == b?.GetHashCode();
        }

        public static bool operator != (Link a, Link b)
        {
            return a?.GetHashCode() != b?.GetHashCode();
        }
    }
}
