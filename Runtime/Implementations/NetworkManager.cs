using SwiftFramework.Core;
using UnityEngine.Networking;
using System;
using UnityEngine;
using System.Collections;

namespace SwiftFramework.Core
{
    [DefaultModule]
    [DisallowCustomModuleBehaviours]
    internal class NetworkManager : BehaviourModule, INetworkManager
    {
        public IPromise<Texture2D> DownloadImage(string url)
        {
            Promise<Texture2D> result = Promise<Texture2D>.Create();

            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

            var operation = request.SendWebRequest();

            operation.completed += response =>
            {
                if (request.isHttpError || request.isNetworkError)
                {
                    result.Reject(new Exception(request.error));
                }
                else
                {
                    DownloadHandlerTexture handler = request.downloadHandler as DownloadHandlerTexture;
                    if (handler == null || handler.texture == null)
                    {
                        result.Reject(new Exception("Empty response"));
                    }
                    else
                    {
                        result.Resolve(handler.texture);
                    }
                }
            };

            return result;
        }

        public IPromise<byte[]> DownloadRaw(string url, Action<long> progressBytes = null)
        {
            Promise<byte[]> result = Promise<byte[]>.Create();

            UnityWebRequest request = UnityWebRequest.Get(url);

            var operation = request.SendWebRequest();

            StartCoroutine(ProgressRoutine(result, operation, progressBytes));

            operation.completed += response =>
            {
                if (request.isHttpError || request.isNetworkError)
                {
                    result.Reject(new Exception(request.error));
                }
                else
                {
                    if (request.downloadHandler == null || request.downloadHandler.data == null)
                    {
                        result.Reject(new Exception("Empty response"));
                    }
                    else
                    {
                        result.Resolve(request.downloadHandler.data);
                    }
                }
            };

            return result;
        }

        private IEnumerator ProgressRoutine<T>(Promise<T> promise, UnityWebRequestAsyncOperation operation, Action<long> progressBytes)
        {
            float progress = 0;
            ulong lastByteProgress = 0;
            while(operation.isDone == false)
            {
                if(progressBytes != null && operation.webRequest.downloadedBytes != lastByteProgress)
                {
                    lastByteProgress = operation.webRequest.downloadedBytes;
                    progressBytes((long) lastByteProgress);
                }
                if(promise.CurrentState == PromiseState.Pending && operation.progress != progress)
                {
                    promise.ReportProgress(operation.progress);
                    progress = operation.progress;
                }
                yield return null;
            }
        }

        public IPromise<string> Get(string url, int timeoutSeconds = 5)
        {
            Promise<string> result = Promise<string>.Create();

            UnityWebRequest request = UnityWebRequest.Get(url);
 
            request.timeout = timeoutSeconds;

            var operation = request.SendWebRequest();

            operation.completed += response => 
            {
                if (request.isHttpError || request.isNetworkError)
                {
                    result.Reject(new Exception(request.error));
                }
                else
                {
                    if (string.IsNullOrEmpty(request.downloadHandler.text))
                    {
                        result.Reject(new Exception("Empty response"));
                    }
                    else
                    {
                        result.Resolve(request.downloadHandler.text);
                    }
                }
            };

            return result;
        }
    }

}
