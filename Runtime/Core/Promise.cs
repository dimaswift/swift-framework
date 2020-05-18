using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftFramework.Core
{
    public interface IBasePromise
    {
        int Id { get; }
        PromiseState CurrentState { get; }
    }

    public interface IPromise : IBasePromise
    {
        void Done(Action action);
        void Progress(Action<float> action);
        void Catch(Action<Exception> action);
        void Always(Action action);
        IPromise Then(Action onSuccess = null, Action<Exception> onError = null);
        IPromise Channel(Promise otherPromise);
    }

    public interface IPromise<T> : IBasePromise
    {
        void Done(Action<T> action);
        void Progress(Action<float> action);
        void Catch(Action<Exception> action);
        void Always(Action<T> action);
        IPromise Then(Action<T> onSuccess = null, Action<Exception> onError = null);
        IPromise<T> Channel(Promise<T> otherPromise);
    }

    public enum PromiseState
    {
        Pending,
        Resolved,
        Rejected
    }

    internal static class PromiseLock
    {
        public  static  readonly  object Locker = new object();
    }

    public class Promise<T> : IPromise<T>, IDisposable
    {
        public PromiseState CurrentState => state;
        public int Id { get; private set; }
        private readonly List<Action<T>> doneActions = new List<Action<T>>(ACTIONS_CAPACITY);
        private readonly List<Action<float>> progressActions = new List<Action<float>>(ACTIONS_CAPACITY);
        private readonly List<Action<Exception>> failActions = new List<Action<Exception>>(ACTIONS_CAPACITY);

        private PromiseState state = PromiseState.Pending;

        private static readonly Stack<Promise<T>> pool = new Stack<Promise<T>>(POOL_CAPACITY);
        private const int POOL_CAPACITY = 8;
        private const int ACTIONS_CAPACITY = 2;
        private Exception exception;
        private T result;
        private float progress;

        private Promise()
        {
            Id = Promise.autoId++;
        }

        ~Promise()
        {
            lock (PromiseLock.Locker)
            {
                Dispose();
                Id = Promise.autoId++;
                pool.Push(this);
            }
        }

        public static Promise<T> Create()
        {
            lock (PromiseLock.Locker)
            {
                if (pool.Count > 0)
                {
                    return pool.Pop();
                }

                return new Promise<T>();
            }
        }

        public static IPromise<T> Resolved(T result)
        {
            Promise<T> promise = Create();
            promise.Resolve(result);
            return promise;
        }

        public static IPromise<T> Rejected(Exception exception)
        {
            Promise<T> promise = Create();
            promise.Reject(exception);
            return promise;
        }

        public static IPromise<T> Race(params IPromise<T>[] promises)
        {
            return Race((IEnumerable<IPromise<T>>) promises);
        }

        public static IPromise<T> Race(IEnumerable<IPromise<T>> promises)
        {
            Promise<T> promise = Create();

            foreach (IPromise<T> p in promises)
            {
                p.Then(r =>
                    {
                        if (promise.CurrentState == PromiseState.Pending)
                        {
                            promise.Resolve(r);
                        }
                    })
                    .Catch(e =>
                    {
                        if (promise.CurrentState == PromiseState.Pending)
                        {
                            promise.Reject(e);
                        }
                    });
            }

            return promise;
        }

        public void Dispose()
        {
            progress = 0;
            result = default;
            doneActions.Clear();
            progressActions.Clear();
            failActions.Clear();
            state = PromiseState.Pending;
            exception = null;
        }

        public void ReportProgress(float progress)
        {
            if (state != PromiseState.Pending)
            {
                Debug.LogError($"Trying to report progress on promise with state {state}");
                return;
            }

            this.progress = progress;

            for (int i = 0; i < progressActions.Count; i++)
            {
                progressActions[i]?.Invoke(progress);
            }
        }

        public void Progress(Action<float> action)
        {
            switch (state)
            {
                case PromiseState.Pending:
                    progressActions.Add(action);
                    break;
                case PromiseState.Resolved:
                    action(progress);
                    break;
            }
        }

        public void Resolve(T result, bool waitForMainThread = false)
        {
            if (waitForMainThread && Dispatcher.IsOnMainThread == false)
            {
                Dispatcher.RunOnMainThread(() => Resolve(result));
                return;
            }

            if (state != PromiseState.Pending)
            {
                Debug.LogError($"Trying to resolve promise with state: {state}");
                return;
            }

            this.result = result;

            state = PromiseState.Resolved;

            for (int i = 0; i < doneActions.Count; i++)
            {
                doneActions[i]?.Invoke(result);
            }
        }

        public IPromise Then(Action<T> onSuccess = null, Action<Exception> onError = null)
        {
            Promise promise = Promise.Create();

            Progress(p => { promise.ReportProgress(p); });

            Done(r =>
            {
                onSuccess?.Invoke(r);
                promise.Resolve();
            });

            Catch(e =>
            {
                onError?.Invoke(e);
                promise.Reject(e);
            });

            return promise;
        }

        public void Reject(Exception exception, bool waitForMainThread = false)
        {
            if (waitForMainThread && Dispatcher.IsOnMainThread == false)
            {
                Dispatcher.RunOnMainThread(() => Reject(exception));
                return;
            }

            if (state != PromiseState.Pending)
            {
                Debug.LogError($"Trying to reject promise with state: {state}");
                return;
            }

            Debug.LogException(exception);

            this.exception = exception;

            state = PromiseState.Rejected;

            for (int i = 0; i < failActions.Count; i++)
            {
                failActions[i]?.Invoke(exception);
            }
        }

        public void Done(Action<T> action)
        {
            switch (state)
            {
                case PromiseState.Pending:
                    doneActions.Add(action);
                    break;
                case PromiseState.Resolved:
                    action(result);
                    break;
                case PromiseState.Rejected:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public void Catch(Action<Exception> action)
        {
            switch (state)
            {
                case PromiseState.Pending:
                    failActions.Add(action);
                    break;
                case PromiseState.Rejected:
                    action(exception);
                    break;
                case PromiseState.Resolved:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Always(Action<T> action)
        {
            switch (state)
            {
                case PromiseState.Pending:
                    failActions.Add(e => action(default));
                    doneActions.Add(action);
                    break;
                case PromiseState.Rejected:
                case PromiseState.Resolved:
                    action(result);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IPromise<T> Channel(Promise<T> otherPromise)
        {
            Progress(otherPromise.ReportProgress);

            Done(r => { otherPromise.Resolve(r); });

            Catch(e => { otherPromise.Reject(e); });

            return this;
        }
    }


    public class Promise : IPromise, IDisposable
    {
        public PromiseState CurrentState => state;
        public int Id { get; private set; }
        private readonly List<Action> doneActions = new List<Action>(ACTIONS_CAPACITY);
        private readonly List<Action<float>> progressActions = new List<Action<float>>(ACTIONS_CAPACITY);
        private readonly List<Action<Exception>> failActions = new List<Action<Exception>>(ACTIONS_CAPACITY);
        private PromiseState state = PromiseState.Pending;

        private static readonly Stack<Promise> pool = new Stack<Promise>();
        private const int POOL_CAPACITY = 8;
        private const int ACTIONS_CAPACITY = 2;
        private Exception exception;
        private float progress;
        internal static int autoId;
        private static readonly object locker = new object();

        private static readonly Promise resolvedPromise = Create();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            for (int i = 0; i < POOL_CAPACITY; i++)
            {
                pool.Push(new Promise());
            }
        }

        private Promise()
        {
            Id = autoId++;
        }

        ~Promise()
        {
            lock (locker)
            {
                Dispose();
                Id = autoId++;
                pool.Push(this);
            }
        }

        public static Promise Create()
        {
            lock (locker)
            {
                try
                {
                    return pool.Count > 0 ? pool.Pop() : new Promise();
                }
                catch
                {
                    return new Promise();
                }
            }
        }

        public static IPromise Resolved()
        {
            if (resolvedPromise.state != PromiseState.Resolved)
            {
                resolvedPromise.Resolve();
            }

            return resolvedPromise;
        }

        public static IPromise Rejected(Exception exception)
        {
            Promise promise = Create();
            promise.Reject(exception);
            return promise;
        }

        public void Dispose()
        {
            progress = 0;
            doneActions.Clear();
            failActions.Clear();
            progressActions.Clear();
            state = PromiseState.Pending;
            exception = null;
        }

        public IPromise Then(Action onSuccess = null, Action<Exception> onError = null)
        {
            Promise promise = Create();

            Progress(p => { promise.ReportProgress(p); });

            Done(() =>
            {
                onSuccess?.Invoke();
                promise.Resolve();
            });

            Catch(e =>
            {
                onError?.Invoke(e);
                promise.Reject(e);
            });

            return promise;
        }

        public void ReportProgress(float progress)
        {
            if (state != PromiseState.Pending)
            {
                Debug.LogError($"Trying to report progress on promise with state: {state}");
                return;
            }

            for (int i = 0; i < progressActions.Count; i++)
            {
                progressActions[i]?.Invoke(progress);
            }
        }

        public void Progress(Action<float> action)
        {
            switch (state)
            {
                case PromiseState.Pending:
                    progressActions.Add(action);
                    break;
                case PromiseState.Resolved:
                    action(progress);
                    break;
            }
        }

        public void ResolveOnMainThread()
        {
            Dispatcher.RunOnMainThread(Resolve);
        }

        public void Resolve()
        {
            if (state != PromiseState.Pending)
            {
                Debug.LogError($"Trying to resolve promise with state: {state}");
                return;
            }

            state = PromiseState.Resolved;

            for (int i = 0; i < doneActions.Count; i++)
            {
                doneActions[i]?.Invoke();
            }
        }

        public static IPromise Race<T>(params IPromise<T>[] promises)
        {
            return Race((IEnumerable<IPromise<T>>) promises);
        }

        public static IPromise Race<T>(IEnumerable<IPromise<T>> promises)
        {
            Promise promise = Create();

            foreach (IPromise<T> p in promises)
            {
                p.Then(r =>
                    {
                        if (promise.CurrentState == PromiseState.Pending)
                        {
                            promise.Resolve();
                        }
                    })
                    .Catch(e =>
                    {
                        if (promise.CurrentState == PromiseState.Pending)
                        {
                            promise.Reject(e);
                        }
                    });
            }

            return promise;
        }

        public static IPromise Race(params IPromise[] promises)
        {
            return Race((IEnumerable<IPromise>) promises);
        }

        public static IPromise Race(IEnumerable<IPromise> promises)
        {
            Promise promise = Create();

            foreach (IPromise p in promises)
            {
                p.Then(() =>
                    {
                        if (promise.CurrentState == PromiseState.Pending)
                        {
                            promise.Resolve();
                        }
                    })
                    .Catch(e =>
                    {
                        if (promise.CurrentState == PromiseState.Pending)
                        {
                            promise.Reject(e);
                        }
                    });
            }

            return promise;
        }

        public static IPromise All(params IPromise[] promises)
        {
            return All((IEnumerable<IPromise>) promises);
        }

        public static IPromise All<T>(params IPromise<T>[] promises)
        {
            return All((IEnumerable<IPromise<T>>) promises);
        }

        private static readonly List<IPromise> buffer = new List<IPromise>();
        
        public static IPromise All(IEnumerable<IPromise> promises, Action<float> progress = null)
        {
            buffer.Clear();
            buffer.AddRange(promises);
            
            int total = buffer.Count();

            if (total == 0)
            {
                return Resolved();
            }

            int current = 0;

            Promise promise = Create();

            foreach (IPromise p in buffer)
            {
                p.Then(() =>
                    {
                        current++;
                        progress?.Invoke(current / (float) total);
                        if (current >= total)
                        {
                            if (promise.state == PromiseState.Pending)
                            {
                                promise.Resolve();
                            }
                        }
                    })
                    .Catch(e =>
                    {
                        if (promise.state == PromiseState.Pending)
                        {
                            promise.Reject(e);
                        }
                    });
            }

            return promise;
        }

        public static IPromise All<T>(IEnumerable<IPromise<T>> promises, Action<float> progress = null)
        {
            IPromise<T>[] array = promises.ToArray();
            int total = array.Length;

            if (total == 0)
            {
                return Resolved();
            }

            int current = 0;

            Promise promise = Create();

            foreach (IPromise<T> p in array)
            {
                p.Then(r =>
                    {
                        current++;
                        progress?.Invoke(current / (float) total);

                        if (current == total)
                        {
                            if (promise.state == PromiseState.Pending)
                            {
                                promise.Resolve();
                            }
                        }
                    })
                    .Catch(e =>
                    {
                        if (promise.state == PromiseState.Pending)
                        {
                            promise.Reject(e);
                        }
                    });
            }

            return promise;
        }

        public void Reject(Exception exception, bool waitForMainThread = false)
        {
            if (state != PromiseState.Pending)
            {
                Debug.LogError($"Trying to reject promise with state: {state}");
                return;
            }

            if (waitForMainThread && Dispatcher.IsOnMainThread == false)
            {
                Dispatcher.RunOnMainThread(() => Reject(exception));
                return;
            }

            this.exception = exception;

            state = PromiseState.Rejected;

            for (int i = 0; i < failActions.Count; i++)
            {
                failActions[i]?.Invoke(exception);
            }
        }

        public void Done(Action action)
        {
            switch (state)
            {
                case PromiseState.Pending:
                    doneActions.Add(action);
                    break;
                case PromiseState.Resolved:
                    action();
                    break;
            }
        }

        public void Catch(Action<Exception> action)
        {
            switch (state)
            {
                case PromiseState.Pending:
                    failActions.Add(action);
                    break;
                case PromiseState.Rejected:
                    action(exception);
                    break;
                case PromiseState.Resolved:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Always(Action action)
        {
            switch (state)
            {
                case PromiseState.Pending:
                    doneActions.Add(action);
                    failActions.Add(e => action());
                    break;
                case PromiseState.Rejected:
                case PromiseState.Resolved:
                    action();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IPromise Channel(Promise otherPromise)
        {
            Progress(otherPromise.ReportProgress);

            Done(otherPromise.Resolve);

            Catch(e => { otherPromise.Reject(e); });

            return this;
        }
    }
}