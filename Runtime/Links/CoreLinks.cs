using UnityEngine;
using System;
using UnityEngine.SceneManagement;

#if USE_ADDRESSABLES

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

#endif

namespace SwiftFramework.Core
{
    [Serializable]
    public class GameObjectLink : LinkTo<GameObject>
    {

    }

    [FlatHierarchy]
    [Serializable()]
    public class ModuleConfigLink : LinkTo<ModuleConfig>
    {

    }

    [Serializable]
    [LinkFolder(Folders.Addressables + "/Sounds")]
    public class AudioClipLink : LinkTo<AudioClip>
    {

    }

    [Serializable()]
    [LinkFolder(Folders.Addressables + "/Sprites")]
    public class SpriteLink : LinkTo<Sprite>
    {

    }

    [Serializable()]
    public class Texture2DLink : LinkTo<Texture2D>
    {

    }

    [Serializable()]
    public class MaterialLink : LinkTo<Material>
    {

    }

    [FlatHierarchy]
    [LinkFolder(Folders.Configs)]
    [Serializable()]
    public class ModuleManifestLink : LinkTo<BaseModuleManifest>
    {

    }

    [FlatHierarchy]
    [Serializable()]
    public class BehaviourModuleLink : LinkToPrefab<BehaviourModule>
    {

    }

    [FlatHierarchy]
    [Serializable()]
    public class WindowLink : LinkToPrefab<IWindow>
    {

    }

    [FlatHierarchy]
    [Serializable()]
    public class UIElementLink : LinkToPrefab<IUIElement>
    {

    }

    [Serializable()]
    [LinkFolder(Folders.Addressables + "/" + Folders.Views)]
    public class ViewLink : LinkToPrefab<IView>
    {

    }

    [FlatHierarchy]
    [Serializable()]
    public class SceneLink : Link
    {
#if USE_ADDRESSABLES
        private SceneInstance sceneInstance;
#else
        private Scene scene;
#endif
        public override string ToString()
        {
            return System.IO.Path.GetFileNameWithoutExtension(GetPath());
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

            SceneManager.LoadSceneAsync(Path, mode).GetPromise().Then(() => promise.Resolve(true)).Catch(e => promise.Resolve(false));

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
