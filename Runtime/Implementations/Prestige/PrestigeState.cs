using System;

namespace SwiftFramework.Core.Prestige
{
    [Serializable]
    public class PrestigeState : IDeepCopy<PrestigeState>
    {
        public int level;
        public bool notifiedAboutAvailablePresitge;

        public PrestigeState() { }

        public PrestigeState DeepCopy()
        {
            return new PrestigeState()
            {
                level = level,
                notifiedAboutAvailablePresitge = notifiedAboutAvailablePresitge,
            };
        }
    }
}
