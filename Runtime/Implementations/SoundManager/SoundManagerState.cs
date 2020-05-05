using SwiftFramework.Core;
using System.Collections.Generic;

namespace SwiftFramework.Sound
{
    [System.Serializable]
    internal class SoundManagerState
    {
        public List<SoundTypeState> statesByType = new List<SoundTypeState>();

        [System.Serializable]
        public class SoundTypeState
        {
            public SoundType type;
            public bool muted;
        }
    }
}
