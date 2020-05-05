using System;
using UnityEngine;
using System.Reflection;
using System.Runtime.Serialization;

namespace SwiftFramework.Core
{
    public class UserCustomDrawerAttribute : Attribute
    {

    }

    [Serializable]
    public class Link : ILink, ISerializationCallbackReceiver
    {
        private const string GENERATED = "generated_";

        public const string PathPropertyName = nameof(Path);

        public virtual bool HasValue => Path != NULL;

        public bool IsEmpty => string.IsNullOrEmpty(Path) || Path == NULL;

        [NonSerialized] protected int cachedHash = 0;

        [HideInInspector] [SerializeField] protected string Path = NULL;

        public const string NULL = "null";

        public string ResourcesPath => GetPath().RemoveExtention();

        public static string AssetToLinkPath(string assetPath)
        {
            return assetPath.Substring(7, assetPath.Length - 7);
        }

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
            if(cachedHash == 0)
            {
                cachedHash = Path.GetHashCode();
            }
            return cachedHash;
        }

        public static T Create<T>(string path) where T : Link, new()
        {
            return new T() { Path = path };
        }

        public static T Generate<T>() where T : Link, new()
        {
            return new T() { Path = $"{GENERATED}{Guid.NewGuid().ToString()}" };
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
