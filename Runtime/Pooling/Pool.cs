using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.Pooling
{
    public class Pool : IPool
    {
        public int CurrentCapacity => poolStack.Count;

        private readonly Func<IPooled> createInstanceHandler;

        private Stack<IPooled> poolStack = new Stack<IPooled>();

        private List<IPooled> activeItems = new List<IPooled>();

        private static readonly List<Pool> pools = new List<Pool>();

        private Pool(Func<IPooled> createInstanceHandler)
        {
            this.createInstanceHandler = createInstanceHandler;
        }

        public static Pool Create(Func<IPooled> createInstanceHandler)
        {
            Pool pool = new Pool(createInstanceHandler);

            pools.Add(pool);

            return pool;
        }

        public static void DisposeAllPools()
        {
            foreach (var pool in pools)
            {
                try
                {
                    pool.Dispose();
                }
                catch(Exception e)
                {
                    Debug.LogError($"{e.Message}"); 
                }
            }
            pools.Clear();
        }

        public void Dispose()
        {
            foreach (var item in poolStack)
            {
                try
                {
                    item.Dispose();
                }
                catch
                {
                    continue;
                }
            }
            poolStack.Clear();
        }

        public static int PoolsCount => pools.Count;

      

        public static Pool GetPool(int i)
        {
            return pools[i];
        }

        public void WarmUp(int capacity)
        {
            int activeCount = activeItems.Count;

            for (int i = 0; i < capacity; i++)
            {
                AddInstanceToPool();
                activeItems.Add(null);
            }

            activeItems.RemoveRange(activeCount, capacity);
        }

        private void AddInstanceToPool()
        {
            IPooled item = createInstanceHandler();
            item.Init(this);
            item.ReturnToPool();
        }

        public IEnumerable<T> GetActiveItems<T>() where T : IPooled
        {
            for (int i = 0; i < activeItems.Count; i++)
            {
                yield return (T)activeItems[i];
            }
        }

        public void ForEachActiveItem(Action<IPooled> handler)
        {
            for (int i = 0; i < activeItems.Count; i++)
            {
                handler(activeItems[i]);
            }
        }

        public IPooled Take()
        {
            if (poolStack.Count == 0)
            {
                AddInstanceToPool();
            }
            IPooled item = poolStack.Pop();
           
            item.TakeFromPool();
            activeItems.Add(item);
            return item;
        }

        public T Take<T>() where T : class, IPooled
        {
            return Take() as T;
        }

        public void Return(IPooled pooledObject)
        {
            poolStack.Push(pooledObject);
            activeItems.Remove(pooledObject);
        }

        public void ReturnAll()
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                activeItems[i].ReturnToPool();
            }
        }

        public IPromise WarmUpAsync(int capacity)
        {
            return App.Core.Timer.WaitForAll(WarmUpRoutine(capacity));
        }

        private IEnumerable<Action> WarmUpRoutine(int capacity)
        {
            for (int i = 0; i < capacity; i++)
            {
                yield return AddInstanceToPool;
            }
        }
    }
}

