using System;
using UnityEngine;

namespace Swift.Core
{
    [Serializable]
    [LinkFolder(Folders.Events)]
    public class GlobalEventLink : LinkTo<GlobalEvent>
    {
        public void Invoke()
        {
            if (IsEmpty)
            {
                return;
            }
            
            if (!Value)
            {
                Debug.LogError($"Cannot invoke event: {GetPath()}");
                return;
            }
            Value.Invoke();
        }

        
        public void Invoke(EventArguments arguments)
        {
            if (IsEmpty)
            {
                return;
            }
            
            if (!Value)
            {
                Debug.LogError($"Cannot invoke event: {GetPath()}");
                return;
            }
            Value.Invoke(arguments);
        }

        public void AddListener(GlobalEventHandler eventHandler)
        {
            if (IsEmpty)
            {
                return;
            }
            
            if (!Value)
            {
                Debug.LogError($"Cannot add listener to event: {GetPath()}");
                return;
            }
            Value.AddListener(eventHandler);
        }

        public bool RemoveListener(GlobalEventHandler eventHandler)
        {
            if (IsEmpty)
            {
                return false;
            }
            
            if (!Value)
            {
                Debug.LogError($"Cannot remove listener to event: {GetPath()}");
                return false;
            }
            return Value.RemoveListener(eventHandler);
        }

        public void RemoveAllListeners()
        {
            if (IsEmpty)
            {
                return;
            }
            
            if (!Value)
            {
                Debug.LogError($"Cannot remove all listeners: {GetPath()}");
                return;
            }
            Value.RemoveAllListeners();
        }
    }

    [Serializable]
    [LinkFolder(Folders.Promises)]
    public class GlobalPromiseLink : LinkTo<GlobalPromise>
    {
        public void Resolve()
        {
            Value.Resolve();
        }

        public void Reject(Exception exception)
        {
            Value.Reject(exception);
        }

        public void ReportProgress(float progress)
        {
            Value.ReportProgress(progress);
        }

        public IPromise Then(Promise promise)
        {
            return Value.Then(promise);
        }

        public IPromise Then(Action onSuccess, Action<Exception> onFail = null)
        {
            return Value.Then(onSuccess, onFail);
        }

        public IPromise Always(Action action)
        {
            return Value.Always(action);
        }

        public void ResetPromise()
        {
            Value.ResetPromise();
        }
    }


    [Serializable]
    public class EventArgumentsLink : LinkTo<EventArguments>
    {
        
    }
}
