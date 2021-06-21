using System;
using UnityEngine;

namespace Swift.Core.Views
{
    public class SimpleProgressBar : MonoBehaviour, IProgressBar
    {
        [SerializeField] private Transform bar = null;
        [SerializeField] private GenericText progressText = null;

        public void SetUp(float progressNormalized)
        {
            bar.localScale = new Vector3(progressNormalized, 1, 1);
        }

        public void SetUp(int current, int total)
        {
            SetUp((float)current / total);
            if (progressText.HasValue)
            {
                progressText.Value.Text = $"{current}/{total}";
            }
        }
    }
}
