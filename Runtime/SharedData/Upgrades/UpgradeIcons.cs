using UnityEngine;

namespace SwiftFramework.Core.SharedData.Upgrades
{
    [PrewarmAsset]
    public abstract class UpgradeIcons : ScriptableObject
    {

    }

    [System.Serializable]
    public class UpgradeIconsLink : LinkToScriptable<UpgradeIcons> { }
}
