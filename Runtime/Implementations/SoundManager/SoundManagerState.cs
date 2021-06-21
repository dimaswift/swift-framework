using System.Collections.Generic;
using Swift.Core;

namespace Swift.Sound
{
    [System.Serializable]
    internal class SoundManagerState
    {
        public List<SoundTypeState> statesByType = new List<SoundTypeState>();

        public bool enableVibration = true;
        
        [System.Serializable]
        public class SoundTypeState
        {
            public SoundType type;
            public bool muted;
        }
    }
}
