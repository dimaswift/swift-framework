using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [CreateAssetMenu(menuName = "SwiftFramework/Promises/GlobalPromise", fileName = "GlobalPromise")]
    public class GlobalPromise : ScriptableObject
    {
        public PromiseState State => globalPromise.CurrentState;

        [NonSerialized] private readonly Promise globalPromise = Promise.Create();

        public void Resolve()
        {
            if (globalPromise.CurrentState != PromiseState.Pending)
            {
                return;
            }
            globalPromise.Resolve();
        }

        public void Reject(Exception exception)
        {
            if (globalPromise.CurrentState != PromiseState.Pending)
            {
                return;
            }
            globalPromise.Reject(exception);
        }

        public void ReportProgress(float progress)
        {
            if (globalPromise.CurrentState != PromiseState.Pending)
            {
                return;
            }
            globalPromise.ReportProgress(progress);
        }

        public IPromise Then(Promise promise)
        {
            globalPromise.Progress(p =>
            {
                promise.ReportProgress(p);
            });

            globalPromise.Done(() =>
            {
                promise.Resolve();
            });

            globalPromise.Catch(e =>
            {
                promise.Reject(e);
            });

            return promise;
        }

        public IPromise Then(Action onSuccess, Action<Exception> onFail = null)
        {
            globalPromise.Done(() =>
            {
                onSuccess();
            });

            globalPromise.Catch(e =>
            {
                onFail?.Invoke(e);
            });

            return globalPromise;
        }

        public IPromise Always(Action action)
        {
            globalPromise.Always(() => action());
            return globalPromise;
        }

        public void ResetPromise()
        {
            globalPromise.Dispose();
        }
    }


}
