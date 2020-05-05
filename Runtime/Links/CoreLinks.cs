using UnityEngine;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

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
    public class GlobalConfigLink : LinkTo<GlobalConfig>
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
        private SceneInstance sceneInstance;

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
            return Addressables.UnloadSceneAsync(sceneInstance).GetPromiseWithoutResult();
        }

        public IPromise<bool> Load(UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Promise<bool> promise = Promise<bool>.Create();

            Addressables.LoadSceneAsync(Path, mode, true).GetPromise().Then(scene =>
            {
                sceneInstance = scene;
                promise.Resolve(true);
            })
            .Catch(e => promise.Resolve(false));

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
            return Load(UnityEngine.SceneManagement.LoadSceneMode.Additive).Then();
        }
    }
}
