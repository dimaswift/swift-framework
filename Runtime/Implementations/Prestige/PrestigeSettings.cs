using System;

namespace SwiftFramework.Core.Prestige
{
    [Serializable]
    public class PrestigeSettings
    {
        public PresigeLevel[] levels;
        public ViewLink view;
        public PriceLink basePrice;

        [Serializable]
        public class PresigeLevel
        {
            public PriceLink price;
            public float multiplier;
        }
    }
}
