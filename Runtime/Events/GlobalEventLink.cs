using System;

namespace SwiftFramework.Core
{
    [Serializable]
    [LinkFolder(Folders.Events)]
    public class GlobalEventLink : LinkTo<GlobalEvent>
    {
        public void Invoke()
        {
            Value.Invoke();
        }

        public void Invoke(EventArguments arguments)
        {
            Value.Invoke(arguments);
        }

        public void AddListener(GlobalEventHandler eventHandler)
        {
            Value.AddListener(eventHandler);
        }

        public bool RemoveListener(GlobalEventHandler eventHandler)
        {
            return Value.RemoveListener(eventHandler);
        }

        public void RemoveAllListeners()
        {
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
