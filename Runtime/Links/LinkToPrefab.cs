using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [Serializable]
    public class LinkToPrefab<T> : Link where T : class
    {
        public bool Loaded => cachedGameObject;

        public override bool HasValue => string.IsNullOrEmpty(Path) == false && Path != NULL;

        [NonSerialized] private T cachedAsset;

        [NonSerialized] private GameObject cachedGameObject;

        [NonSerialized] private Promise<T> loadPromise;

        [NonSerialized] private AsyncOperationHandle<GameObject>? loadHandle;

        public virtual T Value
        {
            get
            {
                if (Loaded)
                {
                    return cachedAsset;
                }

                cachedGameObject = AddrCache.GetAsset<GameObject>(Path);

                cachedAsset = cachedGameObject?.GetComponent<T>();

                Initialize(cachedGameObject);

                return cachedAsset;
            }
        }

        public IPromise<T> Instantiate()
        {
            Promise<T> promise = Promise<T>.Create();
            Load().Then(prefab => 
            {
                var isntance = UnityEngine.Object.Instantiate(cachedGameObject);
                promise.Resolve(isntance.GetComponent<T>());
            })
            .Catch(e => promise.Reject(e));
            return promise;
        }

        private void Initialize(GameObject prefab)
        {
            ILinked linked = prefab.GetComponent<ILinked>();
            if (linked != null)
            {
                linked.SetLink(this);
            }
        }

        public virtual void Load(Action<T> result, Action<Exception> fail = null)
        {
            if (Loaded)
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

            if (HasValue == false)
            {
                loadPromise.Reject(new EntryPointNotFoundException("Link doesn't have any value"));
                return loadPromise;
            }

            if (Loaded || AddrCache.Loaded(Path))
            {
                loadPromise.Resolve(Value);
                return loadPromise;
            }

            loadHandle = Addressables.LoadAssetAsync<GameObject>(Path);

            loadHandle.Value.Completed += a =>
            {
                if (Loaded == false)
                {
                    if (a.Status == AsyncOperationStatus.Succeeded)
                    {
                        cachedGameObject = a.Result;
                        Initialize(cachedGameObject);
                        cachedAsset = cachedGameObject.GetComponent<T>();
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

        public virtual void Release()
        {
            if (Loaded == false || loadHandle.HasValue == false)
            {
                return;
            }

            Addressables.Release(loadHandle.Value);

            loadPromise = null;
            cachedAsset = null;
            cachedGameObject = null;
            loadHandle = null;
        }

        public override IPromise Preload()
        {
            return Load().Then();
        }
    }


}
