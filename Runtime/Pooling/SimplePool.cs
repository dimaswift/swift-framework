using System;
using System.Collections.Generic;

namespace SwiftFramework.Core.Pooling
{
    public class SimplePool<P> : IPool where P : class, IPooled
    {
        public event Action<P> OnItemReturnedToPool = i => { };

        private readonly Stack<P> instances = new Stack<P>();

        private readonly Func<P> instanceHadnler;

        private readonly List<IPooled> activeObjects = new List<IPooled>();

        public int CurrentCapacity => instances.Count;

        public void Return(IPooled pooledObject)
        {
            P item = pooledObject as P;
            instances.Push(item);
            activeObjects.Remove(item);
            OnItemReturnedToPool(item);
        }

        public SimplePool(Func<P> instanceHadnler)
        {
            this.instanceHadnler = instanceHadnler;
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
            P instance = instanceHadnler();
            instance.Init(this);
            instance.ReturnToPool();
        }

        public T Take<T>() where T : class, IPooled
        {
            if(instances.Count == 0)
            {
                AddInstanceToPool();
            }
            T instance = instances.Pop() as T;
            instance.TakeFromPool();
            activeObjects.Add(instance);
            return instance;
        }

        public P Take()
        {
            return Take<P>();
        }

        public void ReturnAll()
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                activeObjects[i].ReturnToPool();
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

        public void Dispose()
        {
            while (instances.Count > 0)
            {
                instances.Pop().Dispose();
            }
            activeObjects.Clear();
        }

        public void Dispose(IPooled pooled)
        {
            if (activeObjects.Contains(pooled))
            {
                activeObjects.Remove(pooled);
            }
        }
    }
}

