using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.Pooling
{
    public class BehaviourPool<P> : IPool where P : Component
    {
        private readonly Stack<P> instances = new Stack<P>();

        private readonly Func<P> instanceHadnler;

        private List<P> activeObjects = new List<P>();

        public int CurrentCapacity => instances.Count;

        public void Return(IPooled pooledObject)
        {
            instances.Push(pooledObject as P);
        }

        public void Return(P pooledObject)
        {
            instances.Push(pooledObject);
        }


        public BehaviourPool(Func<P> instanceHadnler)
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
            instance.gameObject.SetActive(false);
            instances.Push(instance);
        }

        public T Take<T>() where T : class, IPooled
        {
            return Take() as T;
        }

        public void Reset()
        {
            foreach (var instance in activeObjects)
            {
                instance.gameObject.SetActive(false);
                instances.Push(instance);
            }
            activeObjects.Clear();
        }

        public P Take()
        {
            if (instances.Count == 0)
            {
                AddInstanceToPool();
            }
            P instance = instances.Pop();
            instance.gameObject.SetActive(true);
            activeObjects.Add(instance);
            return instance;
        }

        public void ReturnAll()
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                IPooled pooled = activeObjects[i] as IPooled;
                pooled.ReturnToPool();
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
            while(instances.Count > 0)
            {
                UnityEngine.Object.Destroy(instances.Pop());
            }
            activeObjects.Clear();
        }
    }
}

