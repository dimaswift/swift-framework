using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

namespace SwiftFramework.Core
{
    [Serializable]
    public class LinkToScriptable<T> : Link where T : class
    {
        public bool Loaded => loaded;

        public override bool HasValue => string.IsNullOrEmpty(Path) == false && Path != NULL;

        [NonSerialized] private T cachedAsset;

        [NonSerialized] private Promise<T> loadPromise;

        [NonSerialized] private bool loaded;

        [NonSerialized] private AsyncOperationHandle<UnityEngine.ScriptableObject>? loadHandle;

        public virtual T Value
        {
            get
            {
                if (loaded)
                {
                    return cachedAsset;
                }

                if (IsGenerated())
                {
                    cachedAsset = App.Core.Storage.Load<T>(this);
                    Initialize(cachedAsset);
                    loaded = cachedAsset != null;
                    return cachedAsset;
                }

                cachedAsset = AddrCache.GetAsset<UnityEngine.ScriptableObject>(Path) as T;

                Initialize(cachedAsset);

                loaded = cachedAsset != null;

                return cachedAsset;
            }
        }

        private void Initialize(T scriptable)
        {
            if (scriptable == null)
            {
                return;
            }

            ILinked linked = scriptable as ILinked;
            if (linked != null)
            {
                linked.SetLink(this);
            }
        }

        public virtual void Load(Action<T> result, Action<Exception> fail = null)
        {
            if (loaded)
            {
                result(cachedAsset);
                return;
            }
            Load().Then(r => result(r)).Catch(e => fail?.Invoke(e));
        }

        public virtual IPromise<T> Load()
        {
            if (loadPromise != null)
            {
                return loadPromise;
            }

            loadPromise = Promise<T>.Create();

            if (IsGenerated())
            {
                loadPromise.Resolve(Value);
                return loadPromise;
            }

            if (HasValue == false)
            {
                loadPromise.Reject(new EntryPointNotFoundException($"Link doesn't have any value: {GetPath()}"));
                return loadPromise;
            }

            if (loaded || AddrCache.Loaded(Path))
            {
                loadPromise.Resolve(Value);
                return loadPromise;
            }

            loadHandle = Addressables.LoadAssetAsync<UnityEngine.ScriptableObject>(Path);

            loadHandle.Value.Completed += a =>
            {
                if (Loaded == false)
                {
                    if (a.Status == AsyncOperationStatus.Succeeded)
                    {
                        cachedAsset = a.Result as T;
                        Initialize(cachedAsset);
                        loaded = cachedAsset != null;
                        loadPromise.Resolve(cachedAsset);
                    }
                    else
                    {
                        loadPromise.Reject(new EntryPointNotFoundException($"Cannot load value from link: {Path}"));
                    }
                }
            };

            return loadPromise;
        }

        public void Release()
        {
            if (Loaded == false || loadHandle.HasValue == false)
            {
                return;
            }

            Addressables.Release(loadHandle.Value);

            loadPromise = null;
            cachedAsset = null;
            loaded = false;
            loadHandle = null;
        }

        public override IPromise Preload()
        {
            return Load().Then();
        }
    }
}
