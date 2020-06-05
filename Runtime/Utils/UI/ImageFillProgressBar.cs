using UnityEngine;
using UnityEngine.UI;

namespace SwiftFramework.Core.Views
{
    public class ImageFillProgressBar : MonoBehaviour, IProgressBar
    {
        [SerializeField] private Image barImage = null;
        [SerializeField] private GenericText amountText = null;


        public void SetUp(float progressNormalized)
        {
            barImage.fillAmount = progressNormalized;
        }

        public void SetUp(int current, int total)
        {
            SetUp((float)current / total);
            if (amountText.HasValue)
            {
                amountText.Value.Text = $"{current}/{total}";
            }
        }
    }
}
