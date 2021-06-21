using System;
using UnityEngine;

namespace Swift.Core.Windows
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeWindowAnimation : MonoBehaviour, IAppearAnimationHandler
    {
        [SerializeField] private float speed = 2f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1.2f, 1f, 1f);
        
        private CanvasGroup canvasGroup = null;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void ProcessHiding(float timeNormalized)
        {
            canvasGroup.alpha = 1f - timeNormalized * speed;
            transform.localScale = Vector3.one * scaleCurve.Evaluate(1f - timeNormalized * speed);
        }

        public void ProcessShowing(float timeNormalized)
        {
            canvasGroup.alpha = timeNormalized * speed;
            transform.localScale = Vector3.one * scaleCurve.Evaluate(timeNormalized * speed);
        }
    }
}
