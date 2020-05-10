#if USE_ADDRESSABLES

using UnityEngine.ResourceManagement.AsyncOperations;

namespace SwiftFramework.Core
{
    public static class AddressablesExtentions
    {
        public static IPromise GetPromise(this AsyncOperationHandle handle)
        {
            Promise promise = Promise.Create();

            if (handle.IsDone)
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    promise.Resolve();
                }
                else
                {
                    promise.Reject(handle.OperationException);
                }
            }
            else
            {
                handle.Completed += o =>
                {
                    if (o.Status == AsyncOperationStatus.Succeeded)
                    {
                        promise.Resolve();
                    }
                    else
                    {
                        promise.Reject(handle.OperationException);
                    }
                };
            }

            return promise;
        }

        public static IPromise<T> GetPromise<T>(this AsyncOperationHandle<T> handle)
        {
            Promise<T> promise = Promise<T>.Create();

            if (handle.IsDone)
            {

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    promise.Resolve(handle.Result);
                }
                else
                {
                    promise.Reject(handle.OperationException);
                }
            }
            else
            {
                handle.Completed += o =>
                {
                    if (o.Status == AsyncOperationStatus.Succeeded)
                    {
                        promise.Resolve(o.Result);
                    }
                    else
                    {
                        promise.Reject(handle.OperationException);
                    }
                };
            }

            return promise;
        }

        public static IPromise GetPromiseWithoutResult<T>(this AsyncOperationHandle<T> handle)
        {
            Promise promise = Promise.Create();

            if (handle.IsDone)
            {

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    promise.Resolve();
                }
                else
                {
                    promise.Reject(handle.OperationException);
                }
            }
            else
            {
                handle.Completed += o =>
                {
                    if (o.Status == AsyncOperationStatus.Succeeded)
                    {
                        promise.Resolve();
                    }
                    else
                    {
                        promise.Reject(handle.OperationException);
                    }
                };
            }

            return promise;
        }

    }
}
#endif