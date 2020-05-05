using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Utils/Unlocker")]
    public class UnlockerConfig : ScriptableObject
    {
        public PriceLink price;
        public PriceLink skipUnlockPrice;
        public TimeReduction unlockTimeReduction;
        public long unlockDuration;
        public UnlockStatus defaultStatus;

        [Serializable]
        public class TimeReduction
        {
            public long seconds;
            public PriceLink price;
        }
    }

    [Serializable]
    [LinkFolder(Folders.Configs + "/Unlockers")]
    public class UnlockerLink : LinkTo<UnlockerConfig>
    {

    }}
