using System.Reflection;
using Swift.Core.SharedData;
using UnityEngine;

namespace Swift.Core.Editor
{
    using UnityEditor;
    
    
    [CustomPropertyDrawer(typeof(GameObjectLink))]
    public class GameObjectLinkDrawer : LinkPropertyDrawer<UnityEngine.GameObject>
    {
    }
    
    [CustomPropertyDrawer(typeof(MonoBehaviourLink))]
    public class MonoBehaviourLinkDrawer : LinkPropertyDrawer<UnityEngine.MonoBehaviour>
    {
    }
    
    [CustomPropertyDrawer(typeof(ModuleConfigLink))]
    public class ModuleConfigLinkDrawer : LinkPropertyDrawer<ModuleConfig>
    {
    }
    
    [CustomPropertyDrawer(typeof(AudioClipLink))]
    public class AudioClipLinkDrawer : LinkPropertyDrawer<UnityEngine.AudioClip>
    {
    }
    
    [CustomPropertyDrawer(typeof(SpriteLink))]
    public class SpriteLinkDrawer : LinkPropertyDrawer<UnityEngine.Sprite>
    {
    }
    
    [CustomPropertyDrawer(typeof(Texture2DLink))]
    public class Texture2DLinkDrawer : LinkPropertyDrawer<UnityEngine.Texture2D>
    {
    }
    
    [CustomPropertyDrawer(typeof(MaterialLink))]
    public class MaterialLinkDrawer : LinkPropertyDrawer<UnityEngine.Material>
    {
    }
    
    [CustomPropertyDrawer(typeof(BehaviourModuleLink))]
    public class BehaviourModuleLinkDrawer : LinkPropertyDrawer<BehaviourModule>
    {
    }
    
    [CustomPropertyDrawer(typeof(WindowLink))]
    public class WindowLinkDrawer : LinkPropertyDrawer<IWindow>
    {
    }
    
    [CustomPropertyDrawer(typeof(UIElementLink))]
    public class UIElementLinkDrawer : LinkPropertyDrawer<IUIElement>
    {
    }
    
    [CustomPropertyDrawer(typeof(ViewLink))]
    public class ViewLinkDrawer : LinkPropertyDrawer<IView>
    {
    }
    
    [CustomPropertyDrawer(typeof(GlobalEventLink))]
    public class GlobalEventLinkDrawer : LinkPropertyDrawer<GlobalEvent>
    {
    }
    
    [CustomPropertyDrawer(typeof(GlobalPromiseLink))]
    public class GlobalPromiseLinkDrawer : LinkPropertyDrawer<GlobalPromise>
    {
    }
    
    [CustomPropertyDrawer(typeof(EventArgumentsLink))]
    public class EventArgumentsLinkDrawer : LinkPropertyDrawer<EventArguments>
    {
    }

    [CustomPropertyDrawer(typeof(RewardLink))]
    public class RewardLinkDrawer : LinkPropertyDrawer<IReward>
    {
    }
    
    [CustomPropertyDrawer(typeof(CurveLink))]
    public class CurveLinkDrawer : LinkPropertyDrawer<Curve>
    {
    }
    
    [CustomPropertyDrawer(typeof(ScriptableLink))]
    public class ScriptableLinkDrawer : LinkPropertyDrawer<ScriptableObject>
    {
        protected override bool AllowCreation => fieldInfo != null && fieldInfo.GetCustomAttribute<LinkFilterAttribute>() != null;
    }
}
