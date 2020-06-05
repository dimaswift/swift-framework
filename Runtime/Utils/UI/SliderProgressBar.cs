using UnityEngine;
using UnityEngine.UI;

namespace SwiftFramework.Core.Views
{
    public class SliderProgressBar : MonoBehaviour, IProgressBar
    {
        [SerializeField] private Slider slider = null;

        public void SetUp(float progressNormalized)
        {
            slider.normalizedValue = progressNormalized;
        }

        public void SetUp(int current, int total)
        {
            SetUp((float)current / total);
        }
    }
}
