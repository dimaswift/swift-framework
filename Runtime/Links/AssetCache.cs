#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
#endif
using System;
using System.Collections.Generic;

using System.Reflection;
using SwiftFramework.Core;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.IO;

namespace SwiftFramework.Core
{
    public static class AssetCache
    {
        private static readonly Dictionary<string, UnityEngine.Object> preloadedAssets = new Dictionary<string, UnityEngine.Object>();

#if USE_ADDRESSABLES
        private static readonly Dictionary<string, AsyncOperationHandle> preloadedOperations = new Dictionary<string, AsyncOperationHandle>();
#else
        private static readonly Dictionary<string, ResourceRequest> preloadedOperations = new Dictionary<string, ResourceRequest>();
#endif

        private static readonly Dictionary<string, (Promise<IEnumerable<UnityEngine.Object>> promise, List<string> addresses)> loadedLabels =
            new Dictionary<string, (Promise<IEnumerable<UnityEngine.Object>> promise, List<string> addresses)>();

        private static IPromise initPromise;

        public static bool Loaded(string address)
        {
            return preloadedAssets.ContainsKey(address);
        }

        public static T GetAsset<T>(string address) where T : UnityEngine.Object
        {
            if (preloadedAssets.TryGetValue(address, out UnityEngine.Object result))
            {
                return result as T;
            }
            if (string.IsNullOrEmpty(address) || address == Link.NULL)
            {
                return null;
            }
            Debug.LogError($"Cannot get preloaded asset of type '{typeof(T).Name}' at address '{address}'. Before accessing synchronous link property 'Value', you need to call async method AssetCache.Preload(addressable label) or mark asset type with [PrewardAsset]");
            return null;
        }


        public static T GetSingletonAsset<T>() where T : UnityEngine.Object
        {
            if (TryGetSingletonAddress(typeof(T), out string addr))
            {
                Debug.LogError($"Cannot load asset of type {typeof(T).Name} as a singleton. [AddrSingleton] attribute is missing!");
                return null;
            }
            return GetAsset<T>(addr);
        }

        public static T GetPrefab<T>()
        {
            foreach (var asset in preloadedAssets)
            {
                GameObject go = asset.Value as GameObject;
                if (go != null)
                {
                    T component = go.GetComponent<T>();
                    if (component != null)
                    {
                        return component;
                    }
                }
            }
            return default;
        }

        public static IPromise<T> LoadSignletonPrefab<T>()
        {
            if (TryGetSingletonAddress(typeof(T), out string address) == false)
            {
                return Promise<T>.Rejected(new InvalidOperationException($"Cannot load prefab of type {typeof(T).Name} as a singleton. [AddrSingleton] attribute is missing!"));
            }
            return LoadPrefab<T>(address);
        }

        public static bool TryGetSingletonAddress(Type type, out string address)
        {
            address = null;
            AddrSingletonAttribute attr = type.GetCustomAttribute<AddrSingletonAttribute>();
            if (attr == null)
            {
                return false;
            }
            address = string.IsNullOrEmpty(attr.folder) ? type.Name : $"{attr.folder}/{type.Name}";
            return true;
        }

        public static bool IsPrewarmed<T>()
        {
            if (TryGetSingletonAddress(typeof(T), out string address) == false)
            {
                Debug.LogError($"Asset of type {typeof(T).Name} is not a singleton. [AddrSingleton] attribute is missing!");
                return false;
            }
            return preloadedAssets.ContainsKey(address);
        }

        public static IPromise<T> LoadSingletonAsset<T>() where T : UnityEngine.Object
        {
            if (TryGetSingletonAddress(typeof(T), out string address) == false)
            {
                return Promise<T>.Rejected(new InvalidOperationException($"Cannot load asset of type {typeof(T).Name} as a singleton. [AddrSingleton] attribute is missing!"));
            }

            if (preloadedAssets.TryGetValue(address, out UnityEngine.Object asset))
            {
                return Promise<T>.Resolved(asset as T);
            }

#if USE_ADDRESSABLES
            return Addressables.LoadAssetAsync<T>(address).GetPromise();
#else
            Promise<T> promise = Promise<T>.Create();

            Resources.LoadAsync<T>(address).GetPromise<T>().Then(a =>
            {
                if (a == null)
                {
                    promise.Reject(new KeyNotFoundException($"Cannot load asset of type {typeof(T).Name} inside 'Resources/{address}'"));
                    return;
                }
                preloadedAssets.Add(address, a);
                promise.Resolve(a);
            })
           .Catch(e => promise.Reject(e));

            return promise;
#endif

        }

        public static IPromise<T> LoadPrefab<T>(string address)
        {
            Promise<T> promise = Promise<T>.Create();

            bool TryGetPreloaded()
            {
                if (preloadedAssets.TryGetValue(address, out UnityEngine.Object asset))
                {
                    if (asset != null)
                    {
                        GameObject go = asset as GameObject;

                        if (go != null && go.GetComponent<T>() != null)
                        {
                            promise.Resolve(go.GetComponent<T>());
                            return true;
                        }
                    }
                    preloadedAssets.Remove(address);
                    return false;
                }
                return false;
            }

#if USE_ADDRESSABLES
            Addressables.LoadAssetAsync<UnityEngine.GameObject>(address).GetPromise().Then(g =>
            {
                if (TryGetPreloaded())
                {
                    return;
                }
                T comp = g.GetComponent<T>();
                if (comp != null)
                {
                    preloadedAssets.Add(address, g);
                    promise.Resolve(comp);
                }
                else
                {
                    promise.Reject(new InvalidOperationException());
                }
            })
            .Catch(e => promise.Reject(e));
#else
            Resources.LoadAsync<GameObject>(address).GetPromise<GameObject>().Then(g =>
            {
                if (g == null)
                {
                    promise.Reject(new KeyNotFoundException($"Cannot load GameObject inside 'Resources/{address}'"));
                    return;
                }
                if (TryGetPreloaded())
                {
                    return;
                }
                T comp = g.GetComponent<T>();
                if (comp != null)
                {
                    preloadedAssets.Add(address, g);
                    promise.Resolve(comp);
                }
                else
                {
                    promise.Reject(new InvalidOperationException());
                }
            })
            .Catch(e => promise.Reject(e));
#endif

            return promise;
        }

        public static IEnumerable<T> GetAssets<T>() where T : UnityEngine.Object
        {
            foreach (var asset in preloadedAssets)
            {
                if (asset.Value is T)
                {
                    yield return asset.Value as T;
                }
            }
        }

        public static IEnumerable<T> GetPrefabs<T>()
        {
            foreach (var asset in preloadedAssets)
            {
                GameObject go = asset.Value as GameObject;
                if (go != null)
                {
                    T component = go.GetComponent<T>();
                    if (component != null)
                    {
                        yield return component;
                    }
                }
            }
        }

        public static bool TryGetAsset<T>(string address, out T asset) where T : UnityEngine.Object
        {
            if (preloadedAssets.TryGetValue(address, out UnityEngine.Object result))
            {
                asset = result as T;
                return asset;
            }
            asset = null;
            return false;
        }

        public static void Dispose()
        {
            List<string> operationsToDispose = new List<string>();
            foreach (var o in preloadedOperations)
            {
                operationsToDispose.Add(o.Key);
            }

            foreach (var item in operationsToDispose)
            {
                try
                {
                    ReleaseAll(item);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

#if USE_ADDRESSABLES
            Addressables.ClearResourceLocators();
#else
            Resources.UnloadUnusedAssets();
#endif

            preloadedOperations.Clear();
            preloadedAssets.Clear();
            loadedLabels.Clear();
            initPromise = null;
        }

        public static bool ReleaseAll(string label)
        {

#if USE_ADDRESSABLES
            if (preloadedOperations.TryGetValue(label, out AsyncOperationHandle asyncOperation) == false)
            {
                return false;
            }

            preloadedOperations.Remove(label);
            Addressables.Release(asyncOperation);
#else
            if (preloadedOperations.TryGetValue(label, out ResourceRequest asyncOperation) == false || !asyncOperation.asset)
            {
                return false;
            }

            preloadedOperations.Remove(label);
            Resources.UnloadAsset(asyncOperation.asset);
#endif

            foreach (var asset in loadedLabels[label].addresses)
            {
                if (preloadedAssets.ContainsKey(asset))
                {
                    preloadedAssets.Remove(asset);
                }
            }

            loadedLabels.Remove(label);

            return true;
        }

        public static IEnumerable<(string link, UnityEngine.Object)> GetPrewarmedAssets()
        {
            foreach (var item in preloadedAssets)
            {
                yield return (item.Key, item.Value);
            }
        }

        public static IPromise<(IList<object> assets, long size)> CheckForUpdates()
        {
            Promise<(IList<object> assets, long size)> promise = Promise<(IList<object> assets, long size)>.Create();
#if USE_ADDRESSABLES

            Addressables.CheckForCatalogUpdates().GetPromise().Always(assets =>
            {
                if (assets == null || assets.Count == 0)
                {
                    Debug.Log($"No content to update");
                    promise.Resolve((new List<object>(), 0));
                    return;
                }
                Addressables.UpdateCatalogs().GetPromise().Done(catalogs =>
                {
                    foreach (var c in catalogs)
                    {
                        Debug.Log($"Received catalog to update: {c.LocatorId}");
                    }
                    if (catalogs.Count > 0)
                    {
                        IList<object> assetsToDownload = catalogs.FirstOrDefaultFast().Keys.ToList();
                        Addressables.GetDownloadSizeAsync(assetsToDownload).GetPromise().Done(totalSize =>
                        {
                            promise.Resolve((assetsToDownload, totalSize));
                        });
                    }
                    else
                    {
                        Debug.Log($"Empty catalog received");
                        promise.Resolve((new List<object>(), 0));
                    }
                });
            });
#else
            promise.Reject(onlySupportedOnAddressablesException);
#endif

            return promise;
        }

        private static readonly NotSupportedException onlySupportedOnAddressablesException = new NotSupportedException("Operation only supported with Addressables enabled");

        public static IPromise DownloadUpdatesIfNeeded(FileDownloadHandler downloadCallback)
        {
            Promise promise = Promise.Create();

            CheckForUpdates().Done(result =>
            {
                if (result.size == 0)
                {
                    promise.Resolve();
                }
                else
                {
                    DownloadUpdates(result.assets, result.size, downloadCallback).Channel(promise);
                }
            });
            return promise;
        }

        public static IPromise DownloadUpdates(IList<object> assetsToDownload, long totalSize, FileDownloadHandler downloadCallback)
        {
            Promise promise = Promise.Create();

#if USE_ADDRESSABLES
            var handle = Addressables.DownloadDependenciesAsync(assetsToDownload, Addressables.MergeMode.Union);

            App.Core.Coroutine.Begin(ReportDownloadProgress(handle, downloadCallback, totalSize));

            handle.GetPromise().Always(() =>
            {
                promise.Resolve();
            });
#else
            promise.Reject(onlySupportedOnAddressablesException);
#endif

            return promise;
        }
#if USE_ADDRESSABLES
        private static IEnumerator ReportDownloadProgress(AsyncOperationHandle handle, FileDownloadHandler downloadHandler, long totalSize)
        {
            while (handle.IsDone == false)
            {
                if (handle.IsValid() == false)
                {
                    yield break;
                }
                downloadHandler((long)(totalSize * handle.PercentComplete), totalSize);
                yield return null;
            }
            downloadHandler(totalSize, totalSize);
        }

#endif


        public static IPromise<long> GetDownloadSize()
        {
            Promise<long> promise = Promise<long>.Create();

#if USE_ADDRESSABLES
            if (Addressables.ResourceLocators.FirstOrDefaultFast().Locate(AddrLabels.Remote, typeof(UnityEngine.Object), out IList<IResourceLocation> locations))
            {
                AsyncOperationHandle<long> handle = new AsyncOperationHandle<long>();

                try
                {
                    handle = Addressables.GetDownloadSizeAsync(locations);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error trying to get download size");
                    Debug.LogException(e);
                    promise.Resolve(0);
                    return promise;
                }

                handle.GetPromise().Always(v => promise.Resolve(v));
            }
            else
            {
                promise.Resolve(0);
            }
#else

            promise.Reject(onlySupportedOnAddressablesException);
#endif

            return promise;
        }

        public static IPromise<IEnumerable<UnityEngine.Object>> PreloadAll(string label)
        {
            (Promise<IEnumerable<UnityEngine.Object>> promise, List<string> assets) alreadyLoadingResult;

            if (loadedLabels.TryGetValue(label, out alreadyLoadingResult))
            {
                return alreadyLoadingResult.promise;
            }

            if (initPromise == null)
            {
#if USE_ADDRESSABLES
                initPromise = Addressables.InitializeAsync().GetPromiseWithoutResult();
#else
                initPromise = Promise.Resolved();
#endif
            }

            Promise<IEnumerable<UnityEngine.Object>> promise = Promise<IEnumerable<UnityEngine.Object>>.Create();


            List<string> addresses = new List<string>();

            loadedLabels.Add(label, (promise, addresses));

            initPromise.Done(() =>
            {
#if USE_ADDRESSABLES
                try
                {
                    Addressables.ResourceLocators.FirstOrDefaultFast().Locate(label, typeof(UnityEngine.Object), out IList<IResourceLocation> locations);

                    Addressables.LoadAssetsAsync<UnityEngine.Object>(locations, null).Completed += o =>
                    {
                        if (o.Status == AsyncOperationStatus.Succeeded)
                        {
                            for (int i = 0; i < locations.Count; i++)
                            {
                                string key = locations[i].PrimaryKey;
                                if (preloadedAssets.ContainsKey(key) == false)
                                {
                                    addresses.Add(key);
                                    preloadedAssets.Add(key, o.Result[i]);
                                }
                            }
                            preloadedOperations.Add(label, o);
                            promise.Resolve(o.Result);
                        }
                        else
                        {
                            promise.Reject(o.OperationException);
                        }
                    };
                }
                catch (Exception e)
                {
                    promise.Reject(e);
                }
#else
                promise.Resolve(new List<UnityEngine.Object>());

#endif
            });

            return promise;
        }
    }
}
