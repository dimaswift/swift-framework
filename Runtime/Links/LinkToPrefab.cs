#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using System;
using UnityEngine;

namespace Swift.Core
{

    [Serializable]
    public class LinkToPrefab<T> : Link where T : class
    {
        public bool Loaded => cachedGameObject;

        public override bool HasValue => string.IsNullOrEmpty(Path) == false && Path != NULL;

        [NonSerialized] private T cachedAsset;

        [NonSerialized] private GameObject cachedGameObject;

        [NonSerialized] private Promise<T> loadPromise;

#if USE_ADDRESSABLES
        [NonSerialized] private AsyncOperationHandle<GameObject>? loadHandle;
#else
        [NonSerialized] private ResourceRequest loadHandle = null;
#endif

        public virtual T Value
        {
            get
            {
                if (Loaded)
                {
                    return cachedAsset;
                }

#if USE_ADDRESSABLES
                cachedGameObject = AssetCache.GetAsset<GameObject>(Path);
#else
                cachedGameObject = Resources.Load<GameObject>(Path);
#endif

                cachedAsset = cachedGameObject?.GetComponent<T>();

                Initialize(cachedGameObject);
                
#if UNITY_EDITOR

                App.OnDomainReloaded += () => cachedGameObject = null;
                
#endif

                return cachedAsset;
            }
        }

        public IPromise<T> Instantiate()
        {
            Promise<T> promise = Promise<T>.Create();
            Load().Then(prefab => 
            {
                GameObject instance = UnityEngine.Object.Instantiate(cachedGameObject);
                promise.Resolve(instance.GetComponent<T>());
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
            Load().Then(result).Catch(e => fail?.Invoke(e));
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

#if USE_ADDRESSABLES
            if (Loaded || AssetCache.Loaded(Path))
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
#else
            if (Loaded)
            {
                loadPromise.Resolve(Value);
                return loadPromise;
            }

            loadHandle = Resources.LoadAsync<GameObject>(Path);

            loadHandle.completed += a =>
            {
                if (Loaded == false)
                {
                    if (loadHandle.isDone && loadHandle.asset)
                    {
                        cachedGameObject = loadHandle.asset as GameObject;
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

#endif

            return loadPromise;
        }

        public virtual void Release()
        {
#if USE_ADDRESSABLES

            if (Loaded == false || loadHandle.HasValue == false)
            {
                return;
            }

            Addressables.Release(loadHandle.Value);
#else
            if (Loaded == false || loadHandle == null || loadHandle.asset == null)
            {
                return;
            }

            Resources.UnloadAsset(loadHandle.asset);

#endif
            Reset();

        }

        public override void Reset()
        {
            base.Reset();
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
