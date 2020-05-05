using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SwiftFramework.Core
{
    [Serializable]
    public class LinkTo<T> : Link where T : UnityEngine.Object
    {
        public bool Loaded => loaded;

        public override bool HasValue => string.IsNullOrEmpty(Path) == false && Path != NULL;

        [NonSerialized] private T cachedAsset;

        [NonSerialized] private Promise<T> loadPromise;

        [NonSerialized] private bool loaded;

        [NonSerialized] private AsyncOperationHandle<T>? loadHandle;

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

                cachedAsset = AddrCache.GetAsset<T>(Path);

                Initialize(cachedAsset);

                loaded = cachedAsset != null;

                return cachedAsset;
            }
        }

        public virtual T1 GetAs<T1>() where T1 : T
        {
            return Value as T1;
        }

        private void Initialize(T asset)
        {
            ILinked linked = asset as ILinked;

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
            Load().Then(r => result(r)).Catch(e => { fail?.Invoke(e); UnityEngine.Debug.LogException(e); });
        }

        public virtual IPromise<T> Load()
        {
            if (loadPromise != null)
            {
                return loadPromise;
            }

            loadPromise = Promise<T>.Create();

            if (HasValue == false)
            {
                loadPromise.Reject(new EntryPointNotFoundException("Link doesn't have any value"));
                return loadPromise;
            }

            if (loaded || AddrCache.Loaded(Path))
            {
                loadPromise.Resolve(Value);
                return loadPromise;
            }
             
            if (IsGenerated())
            {
                cachedAsset = App.Core.Storage.Load<T>(this);
                Initialize(cachedAsset);
                loaded = cachedAsset != null;
                loadPromise.Resolve(cachedAsset);
                return loadPromise;
            }

            loadHandle = Addressables.LoadAssetAsync<T>(Path);

            loadHandle.Value.Completed += a =>
            {
                if (Loaded == false)
                {
                    if (a.Status == AsyncOperationStatus.Succeeded)
                    {
                        cachedAsset = a.Result;
                        Initialize(cachedAsset);
                        loaded = cachedAsset != null;
                        loadPromise.Resolve(a.Result);
                    }
                    else
                    {
                        var e = new EntryPointNotFoundException($"Cannot load value from link: {Path}");
                        UnityEngine.Debug.LogException(e);
                        loadPromise.Reject(e);
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
