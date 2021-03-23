using System;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public class Signal<T>
    {
        private readonly List<Action<T>> listeners = new List<Action<T>>();

        private bool fired;
        
        private T lastResult = default;

        public void Fire(T result)
        {
            fired = true;
            lastResult = result;
            foreach (var listener in listeners)
            {
                listener(result);
            }
        }
        
        public void Reset()
        {
            fired = false;
            lastResult = default;
        }

        public void Subscribe(Action<T> action)
        {
            if (fired)
            {
                action(lastResult);
                return;
            }

            if (listeners.Contains(action) == false)
            {
                listeners.Add(action);
            }
        }
        
        public void Unsubscribe(Action<T> action)
        {
            listeners.Remove(action);
        }

        public static Signal<T> PreFired(T result)
        {
            return new Signal<T>()
            {
                fired = true,
                lastResult = result
            };
        }
    }
    
    
}