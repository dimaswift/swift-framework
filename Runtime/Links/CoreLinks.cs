using UnityEngine;
using System;

namespace SwiftFramework.Core
{
    [Serializable]
    public class GameObjectLink : LinkTo<GameObject>
    {

    }
    
    [Serializable]
    public class MonoBehaviourLink : LinkToPrefab<MonoBehaviour>
    {

    }


    [FlatHierarchy]
    [Serializable()]
    public class ModuleConfigLink : LinkTo<ModuleConfig>
    {

    }

    [Serializable]
    [LinkFolder(Folders.Sounds)]
    public class AudioClipLink : LinkTo<AudioClip>
    {

    }

    [Serializable()]
    [LinkFolder(Folders.Sprites)]
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
    [LinkFolder(Folders.Views)]
    public class ViewLink : LinkToPrefab<IView>
    {
        public static readonly ViewLink Empty = CreateNull<ViewLink>();
    }

    [Serializable]
    public class ScriptableLink : LinkToScriptable<ScriptableObject>
    {
        
    }
}
