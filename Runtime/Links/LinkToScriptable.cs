#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using System;
using UnityEngine;

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

#if USE_ADDRESSABLES
        [NonSerialized] private AsyncOperationHandle<ScriptableObject>? loadHandle;
#else
        [NonSerialized] private ResourceRequest loadHandle = null;
#endif

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

#if USE_ADDRESSABLES
                cachedAsset = AssetCache.GetAsset<ScriptableObject>(Path) as T;
#else
                cachedAsset = Resources.Load<ScriptableObject>(Path) as T;
#endif

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

        public override void Reset()
        {
            base.Reset();
            loaded = false;
            cachedAsset = null;
            loadHandle = null;
            loadPromise = null;
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

        public IPromise<TScriptable> LoadOrCreate<TScriptable>() where TScriptable : ScriptableObject, T
        {
            Promise<TScriptable> result = Promise<TScriptable>.Create();

            Load().Then(value => result.Resolve(value as TScriptable), e =>
            {
                TScriptable instance = ScriptableObject.CreateInstance<TScriptable>();
                instance.hideFlags = HideFlags.DontSave;
                result.Resolve(instance);
            });
            
            return result;
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

#if USE_ADDRESSABLES
            if (loaded || AssetCache.Loaded(Path))
            {
                loadPromise.Resolve(Value);
                return loadPromise;
            }

            loadHandle = Addressables.LoadAssetAsync<ScriptableObject>(Path);

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
#else

            if (loaded)
            {
                loadPromise.Resolve(Value);
                return loadPromise;
            }

            loadHandle = Resources.LoadAsync<ScriptableObject>(Path);

            loadHandle.completed += a =>
            {
                if (Loaded == false)
                {
                    if (a.isDone)
                    {
                        cachedAsset = loadHandle.asset as T;
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

#endif

            return loadPromise;
        }

        public void Release()
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

        public override IPromise Preload()
        {
            return Load().Then();
        }
    }
}
