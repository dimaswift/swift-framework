using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [PrewarmAsset]
    public class Perk : ScriptableObject
    {
        public string descriptionKey;
        public SpriteLink icon;
        public float multiplier;
    }

    [Serializable]
    [FlatHierarchy]
    public class PerkLink : LinkToScriptable<Perk>
    {

    }
}
