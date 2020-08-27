using SwiftFramework.Core;
using UnityEngine;

namespace SwiftFramework.Utils.UI
{
    public class BounceAppearAnimation : MonoBehaviour, IAppearAnimationHandler
    {
        [SerializeField] private AnimationCurve showCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private AnimationCurve hideCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private CanvasGroup canvas = null;
        public void ProcessShowing(float timeNormalized)
        {
            if (canvas)
            {
                canvas.alpha = timeNormalized;
            }
            transform.localScale = Vector3.one * showCurve.Evaluate(timeNormalized);
        }

        public void ProcessHiding(float timeNormalized)
        {
            if (canvas)
            {
                canvas.alpha = 1f - timeNormalized;
            }
            transform.localScale = Vector3.one * hideCurve.Evaluate(timeNormalized);
        }
    }
}