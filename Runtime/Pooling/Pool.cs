using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.Pooling
{
    public class Pool : IPool
    {
        public int CurrentCapacity => poolStack.Count;

        private readonly Func<IPooled> createInstanceHandler;

        private readonly Stack<IPooled> poolStack = new Stack<IPooled>();

        private readonly HashSet<IPooled> activeItems = new HashSet<IPooled>();
        
        private readonly List<IPooled> buffer = new List<IPooled>();

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
                catch (Exception e)
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

        public void Dispose(IPooled pooled)
        {
            if (activeItems.Contains(pooled))
            {
                activeItems.Remove(pooled);
            }
        }

        public static int PoolsCount => pools.Count;

      

        public static Pool GetPool(int i)
        {
            return pools[i];
        }

        public void WarmUp(int capacity)
        {
            for (int i = 0; i < capacity; i++)
            {
                AddInstanceToPool();
            }
        }

        private void AddInstanceToPool()
        {
            IPooled item = createInstanceHandler();
            item.Init(this);
            activeItems.Add(item);
            item.ReturnToPool();
        }

        public IEnumerable<T> GetActiveItems<T>() where T : IPooled
        {
            foreach (IPooled activeItem in activeItems)
            {
                yield return (T) activeItem;
            }
        }

        public void ForEachActiveItem(Action<IPooled> handler)
        {
            foreach (IPooled activeItem in activeItems)
            {
                handler(activeItem);
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
            buffer.Clear();
            buffer.AddRange(activeItems);
            foreach (IPooled pooled in buffer)
            {
                Return(pooled);
            }
            buffer.Clear();
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

