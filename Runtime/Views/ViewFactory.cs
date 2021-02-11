using SwiftFramework.Core;
using SwiftFramework.Core.Pooling;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    [DefaultModule]
    [DisallowCustomModuleBehaviours]
    internal class ViewFactory : BehaviourModule, IViewFactory
    {
        private const int CAPACITY = 128; 

        private readonly Dictionary<int, Pool> pools = new Dictionary<int, Pool>(CAPACITY);
        

        public IPromise<T> CreateAsync<T>(ViewLink link) where T : class, IView
        {
            Promise<T> promise = Promise<T>.Create();

            CreateAsync<T>(link, r => promise.Resolve(r), e => promise.Reject(e));

            return promise;
        }

        public void CreateAsync<T>(ViewLink link, Action<T> onLoad, Action<Exception> error = null) where T : class, IView
        {
            if (pools.TryGetValue(link.GetHashCode(), out Pool pool))
            {
                onLoad(pool.Take<T>());
                return;
            }

            link.Load(prefab =>
            {
                if (pools.TryGetValue(link.GetHashCode(), out Pool p))
                {
                    onLoad(p.Take<T>());
                    return;
                }

                if (prefab == null)
                {
                    Exception e = new NullReferenceException($"Cannot load view of type {typeof(T).Name}: {link}");
                    Debug.LogException(e);
                    error?.Invoke(e);
                    return;
                }

                pool = Pool.Create(() => Create<T>(prefab.GetRoot()));
                pools.Add(link.GetHashCode(), pool);
                onLoad(pool.Take<T>());
            }, error);
        }

        private T TakeFromPool<T>(ViewLink link) where T : class, IView
        {
            return pools[link.GetHashCode()].Take<T>();
        }

        public T Create<T>(ViewLink link) where T : class, IView
        {
            if (IsLoaded(link))
            {
                return pools[link.GetHashCode()].Take<T>();
            }
            if (AssetCache.Loaded(link.GetPath()))
            {
                Pool pool = Pool.Create(() => Create<T>(link.Value.GetRoot()));
                pools.Add(link.GetHashCode(), pool);
                return pool.Take<T>();
            }
            Debug.LogError($"Cannot create view {link} synchronously. Not loaded yet. Use CreateAsync or WarmUp"); 
            return null;
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            for (int i = 0; i < Pool.PoolsCount; i++)
            {
                Pool.GetPool(i).ForEachActiveItem(view => ((IView)view).Process(delta));
            }
        }

        private T Create<T>(GameObject source) where T : IView
        {
            GameObject instanceObject = Instantiate(source, transform);
            instanceObject.name = source.name;
            T instance = instanceObject.GetComponent<T>();
            return instance;
        }

        public T GetOrCreateView<T>(ViewLink link) where T : class, IView
        {
            if (pools.TryGetValue(link.GetHashCode(), out Pool pool))
            {
                foreach (var item in pool.GetActiveItems<T>())
                {
                    return item;
                }
            }
            return Create<T>(link);
        }

        public void ReturnEverythingToPool()
        {
            for (int i = 0; i < Pool.PoolsCount; i++)
            {
                Pool.GetPool(i).ReturnAll();
            }
        }

        public T FindView<T>(ViewLink link) where T : class, IView
        {
            if (pools.TryGetValue(link.GetHashCode(), out Pool pool))
            {
                foreach (var item in pool.GetActiveItems<T>())
                {
                    return item;
                }
            }

            return default;
        }

        public bool IsLoaded(ViewLink link)
        {
            return pools.ContainsKey(link.GetHashCode());
        }

        public IPromise WarmUp<T>(ViewLink link, int capacity) where T : class, IView
        {
            Promise promise = Promise.Create();

            if (pools.TryGetValue(link.GetHashCode(), out Pool pool))
            {
                promise.Resolve();
                return promise;
            }

            link.Load(prefab =>
            {
                if (pools.TryGetValue(link.GetHashCode(), out Pool p))
                {
                    promise.Resolve();
                    return;
                }

                pool = Pool.Create(() => Create<T>(prefab.GetRoot()));
                pools.Add(link.GetHashCode(), pool);
                pool.WarmUp(capacity);
                promise.Resolve();
            },
            e => promise.Reject(e));

            return promise;
        }
    }

}
