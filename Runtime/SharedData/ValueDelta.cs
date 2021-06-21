namespace Swift.Core
{

    public struct ValueDelta
    {
        public BigNumber baseValue;
        public BigNumber upgradeValue;
        public float baseFloatValue;
        public float upgradeByFloatValue;
        public bool reachedMaxLevel;
        public SpriteLink icon;

        public BigNumber Delta => upgradeValue.Value - baseValue.Value;

        public float FloatDelta => upgradeByFloatValue - baseFloatValue;

        public bool IsBigNumber => baseValue.Value != 0 || upgradeValue.Value != 0;

        public string Sign
        {
            get
            {
                if (IsBigNumber)
                {
                    if (Delta.Value == 0)
                    {
                        return "";
                    }
                    return Delta > 0 ? "+" : "";
                }


                if (FloatDelta == 0)
                {
                    return "";
                }
                return FloatDelta > 0 ? "+" : "";
            }
        }

        public DeltaType Type
        {
            get
            {
                if (reachedMaxLevel)
                    return DeltaType.Positive;

                if (IsBigNumber)
                {
                    if (Delta.Value == 0)
                    {
                        return DeltaType.Constant;
                    }
                    return Delta > 0 ? DeltaType.Positive : DeltaType.Negative;
                }

                if (FloatDelta == 0)
                {
                    return DeltaType.Constant;
                }
                return FloatDelta > 0 ? DeltaType.Positive : DeltaType.Negative;
            }
        }

        public enum DeltaType
        {
            Positive, Negative, Constant
        }

        public string name;
        public string postFix;
    }
}
