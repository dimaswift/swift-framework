using SwiftFramework.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    [DefaultModule]
    [DisallowCustomModuleBehaviours]
    internal class CoroutineTimer : BehaviourModule, ITimer
    {
        private readonly Dictionary<int, Coroutine> activeCoroutines = new Dictionary<int, Coroutine>(64);

        private readonly Queue<Action> actionQueue = new Queue<Action>();

        public event Action OnUpdate = () => { };

        private IPromise StartNew(IEnumerator coroutine, IPromise promise)
        {
            EnqueueForMainThread(() => 
            {
                Coroutine c = StartCoroutine(coroutine);
                activeCoroutines.Add(promise.Id, c);
            });
             
            return promise;
        }

        private void Update()
        {
            OnUpdate();
            lock (actionQueue)
            {
                while (actionQueue.Count > 0)
                {
                    actionQueue.Dequeue()?.Invoke();
                }
            }
        }

        private void EnqueueForMainThread(Action action)
        {
            lock (actionQueue)
            {
                actionQueue.Enqueue(action);
            }
        }

        public bool Cancel(IPromise promise)
        {
            if(promise == null)
            {
                return false;
            }
            if (activeCoroutines.TryGetValue(promise.Id, out Coroutine coroutine))
            {
                EnqueueForMainThread(() => StopCoroutine(coroutine));
                activeCoroutines.Remove(promise.Id);
                return true;
            }
            return false;
        }

        public IPromise WaitFor(float seconds)
        {
            Promise promise = Promise.Create();
            return StartNew(WaitForCoroutine(seconds, promise), promise);
        }

        public IPromise WaitForAll(IEnumerable<Action> actions)
        {
            Promise promise = Promise.Create();
            return StartNew(WaitForAllCoroutine(actions, promise), promise);
        }

        private IEnumerator WaitForCoroutine(float seconds, Promise promise)
        {
            yield return new WaitForSeconds(seconds);
            promise.Resolve();
        }

        private IEnumerator WaitForAllCoroutine(IEnumerable<Action> actions, Promise promise)
        {
            int count = actions.CountFast();
            int i = 0;
            foreach (Action action in actions)
            {
                action();
                i++;
                promise.ReportProgress((float) count / i);
                yield return null;
            }
            promise.Resolve();
        }

        public IPromise WaitForMainThread()
        {
            Promise promise = Promise.Create();
            EnqueueForMainThread(() => promise.Resolve());
            return promise;
        }

        public IPromise WaitForNextFrame()
        {
            Promise promise = Promise.Create();
            return StartNew(WaitForNextFrameCoroutine(promise), promise);
        }

        private IEnumerator WaitForNextFrameCoroutine(Promise promise)
        {
            yield return new WaitForEndOfFrame();
            promise.Resolve();
        }

        public IPromise WaitForUnscaled(float seconds)
        {
            Promise promise = Promise.Create();
            return StartNew(WaitForUnscaledCoroutine(seconds, promise), promise);
        }

        private IEnumerator WaitForUnscaledCoroutine(float seconds, Promise promise)
        {
            yield return new WaitForSecondsRealtime(seconds);
            promise.Resolve();
        }

        public IPromise WaitUntil(Func<bool> condition)
        {
            Promise promise = Promise.Create();
            return StartNew(WaitUntilCoroutine(condition, promise), promise);
        }

        private IEnumerator WaitUntilCoroutine(Func<bool> condition, Promise promise)
        {
            while(condition() == false)
            {
                yield return null;
            }
            promise.Resolve();
        }

        public IPromise Evaluate(float duration, Action<float> callback)
        {
            Promise promise = Promise.Create();
            return StartNew(EvaluateCoroutine(duration, callback, promise), promise);
        }

        private IEnumerator EvaluateCoroutine(float duration, Action<float> callback, Promise promise)
        {
            float time = 0;
            while (time <= 1)
            {
                callback(time);
                time += Time.deltaTime / duration;
                yield return null;
            }
            callback(1);
            promise.Resolve();
        }

    }
}
