using UnityEngine;

namespace SwiftFramework.Core.Windows
{
    public class FadeWindowAnimation : MonoBehaviour, IAppearAnimationHandler
    {
        [SerializeField] private float speed = 1f;
        [SerializeField] private CanvasGroup canvasGroup = null;

        public void ProcessHiding(float timeNormalized)
        {
            canvasGroup.alpha = 1f - timeNormalized * speed;
        }

        public void ProcessShowing(float timeNormalized)
        {
            canvasGroup.alpha = timeNormalized * speed;
        }
    }
}
