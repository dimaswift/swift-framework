using System.Reflection;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    using UnityEditor;
    
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.GameObjectLink))]
    public class GameObjectLinkDrawer : LinkPropertyDrawer<UnityEngine.GameObject>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.MonoBehaviourLink))]
    public class MonoBehaviourLinkDrawer : LinkPropertyDrawer<UnityEngine.MonoBehaviour>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.ModuleConfigLink))]
    public class ModuleConfigLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.ModuleConfig>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.AudioClipLink))]
    public class AudioClipLinkDrawer : LinkPropertyDrawer<UnityEngine.AudioClip>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.SpriteLink))]
    public class SpriteLinkDrawer : LinkPropertyDrawer<UnityEngine.Sprite>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.Texture2DLink))]
    public class Texture2DLinkDrawer : LinkPropertyDrawer<UnityEngine.Texture2D>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.MaterialLink))]
    public class MaterialLinkDrawer : LinkPropertyDrawer<UnityEngine.Material>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.BehaviourModuleLink))]
    public class BehaviourModuleLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.BehaviourModule>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.WindowLink))]
    public class WindowLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.IWindow>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.UIElementLink))]
    public class UIElementLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.IUIElement>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.ViewLink))]
    public class ViewLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.IView>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.GlobalEventLink))]
    public class GlobalEventLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.GlobalEvent>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.GlobalPromiseLink))]
    public class GlobalPromiseLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.GlobalPromise>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.EventArgumentsLink))]
    public class EventArgumentsLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.EventArguments>
    {
    }

    [CustomPropertyDrawer(typeof(SwiftFramework.Core.RewardLink))]
    public class RewardLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.IReward>
    {
    }
    
    [CustomPropertyDrawer(typeof(SwiftFramework.Core.SharedData.CurveLink))]
    public class CurveLinkDrawer : LinkPropertyDrawer<SwiftFramework.Core.SharedData.Curve>
    {
    }
    
    [CustomPropertyDrawer(typeof(ScriptableLink))]
    public class ScriptableLinkDrawer : LinkPropertyDrawer<ScriptableObject>
    {
        protected override bool AllowCreation => fieldInfo != null && fieldInfo.GetCustomAttribute<LinkFilterAttribute>() != null;
    }
}
