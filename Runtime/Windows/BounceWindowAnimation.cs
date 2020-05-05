using SwiftFramework.Core.SharedData;
using UnityEngine;

namespace SwiftFramework.Core.Windows
{
    public class BounceWindowAnimation : MonoBehaviour, IAppearAnimationHandler
    {
        [SerializeField] private float speed = 1f;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private CurveLink showCurve = null;
        [SerializeField] private CurveLink hideCurve = null;

        public void ProcessHiding(float timeNormalized)
        {
            canvasGroup.alpha = 1f - timeNormalized * speed;
            transform.localScale = Vector3.one * hideCurve.Evaluate(Mathf.Min(1, timeNormalized * speed));
        }

        public void ProcessShowing(float timeNormalized)
        {
            transform.localScale = Vector3.one * showCurve.Evaluate(Mathf.Min(1, timeNormalized * speed));
            canvasGroup.alpha = timeNormalized * speed;
        }
    }
}
