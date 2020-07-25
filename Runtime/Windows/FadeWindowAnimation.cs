using System;
using UnityEngine;

namespace SwiftFramework.Core.Windows
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeWindowAnimation : MonoBehaviour, IAppearAnimationHandler
    {
        [SerializeField] private float speed = 2f;
        
        private CanvasGroup canvasGroup = null;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

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
