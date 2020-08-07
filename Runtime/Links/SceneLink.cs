using System;
#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif
using UnityEngine.SceneManagement;

namespace SwiftFramework.Core
{
    [FlatHierarchy]
    [Serializable()]
    public class SceneLink : Link
    {
#if USE_ADDRESSABLES
        [NonSerialized] private SceneInstance sceneInstance = default;
#else
        [NonSerialized] private Scene scene = default;
#endif
        public override string ToString()
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(GetPath());
            return string.IsNullOrEmpty(name) ? NULL : name;
        }

        public string GetScenePath()
        {
            return ToString();
        }

        public IPromise Unload()
        {
#if USE_ADDRESSABLES
            return Addressables.UnloadSceneAsync(sceneInstance).GetPromiseWithoutResult();
#else
            return SceneManager.UnloadSceneAsync(scene).GetPromise();
#endif
        }

        public IPromise<bool> Load(LoadSceneMode mode)
        {
            Promise<bool> promise = Promise<bool>.Create();
#if USE_ADDRESSABLES
            Addressables.LoadSceneAsync(Path, mode, true).GetPromise().Then(scene =>
                {
                    sceneInstance = scene;
                    promise.Resolve(true);
                })
                .Catch(e => promise.Resolve(false));
#else

            SceneManager.LoadSceneAsync("Resources/" + Path, mode).GetPromise().Then(() => promise.Resolve(true)).Catch(e => promise.Resolve(false));

#endif
            return promise;
        }

        public override bool HasValue
        {
            get
            {
                if (GetPath() == NULL || string.IsNullOrEmpty(GetPath()))
                {
                    return false;
                }
                return true;
            }
        }
        public override IPromise Preload()
        {
            return Load(LoadSceneMode.Additive).Then();
        }
    }
}