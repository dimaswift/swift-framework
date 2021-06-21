using System;

namespace Swift.Core
{
    public class FracturedBigNumber
    {
        public IStatefulEvent<BigNumber> Value => value;
        public IStatefulEvent<float> FloatValue => floatValue;


        public event Action OnValueChanged = () => { };

        private readonly StatefulEvent<BigNumber> value = new StatefulEvent<BigNumber>();
        private readonly StatefulEvent<float> floatValue = new StatefulEvent<float>();

        public const string PRODUCTION_SPEED = "production_speed";
        private readonly string localizationKey;

        public FracturedBigNumber(string localizationKey)
        {
            this.localizationKey = localizationKey;
        }

        public override string ToString()
        {
            if (value.Value < 1000)
            {
                return App.Core.Local.GetText(localizationKey, floatValue.Value.ToString("0.00"));
            }

            return App.Core.Local.GetText(localizationKey, value);
        }

        public void Set(BigNumber value, float floatValue)
        {
            this.value.SetValue(value);
            this.floatValue.SetValue(floatValue);
            OnValueChanged();
        }
    }
}
