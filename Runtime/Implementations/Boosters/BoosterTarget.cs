using System;
using UnityEngine;

namespace SwiftFramework.Core.Boosters
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Boosters/Target")]
    public class BoosterTarget : ScriptableObject
    {

    }

    [Serializable]
    [LinkFolder(Folders.Configs + "/Boosters/Targets")]
    public class BoosterTargetLink : LinkTo<BoosterTarget>
    { 

    }
}
