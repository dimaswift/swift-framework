using UnityEngine;
namespace SwiftFramework.Core.Supervisors
{
    [PrewarmAsset]
    public class SupervisorSkin : ScriptableObject
    {
        public SpriteLink icon;
        public ViewLink view;
    }

    [System.Serializable]
    [LinkFolder(Folders.Configs + "/Supervisors/Skins")]
    public class SupervisorSkinLink : LinkTo<SupervisorSkin>
    {

    }
}
