using System.Collections;
using UnityEngine;
using SwiftFramework.Core;
using SwiftFramework.Core.SharedData;
using UnityEngine.UI;

namespace SwiftFramework.Utils.UI
{
    public class BounceAnimation : MonoBehaviour
    {
        [SerializeField] private float animationDuration = .15f;
        [SerializeField] private float scaleDownPercent = .9f;
        [SerializeField] private AudioClipLink clickSound = Link.Create<AudioClipLink>("Sounds/button_click");

        private Coroutine currentAnimation;
        private bool clicked;
        private Vector3 startScale;
        private ISoundManager soundManager;

        private void Awake()
        {
            App.ExecuteOnLoad(() => soundManager = App.Core.GetModule<ISoundManager>());
            startScale = transform.localScale;
            if (startScale.sqrMagnitude == 0)
            {
                startScale = Vector3.one;
            }
        }

        public void Click()
        {
            if (clickSound != null && clickSound.HasValue)
            {
                soundManager.PlayOnce(clickSound, SoundType.SFX);
            }
        }

        public void Press()
        {
            App.Core.Coroutine.Begin(PressAnimationRoutine(), ref currentAnimation);
            
            clicked = true;
        }
        
        public void Release()
        {
            if (clicked == false)
            {
                return;
            }

            App.Core.Coroutine.Begin(BounceAnimationRoutine(), ref currentAnimation);
            
            clicked = false;
        }

        private IEnumerator BounceAnimationRoutine()
        {
            float t = 0f;
            while (t < 1)
            {
                transform.localScale = Vector3.Lerp(startScale * scaleDownPercent, startScale, t);
                t += Time.unscaledDeltaTime / animationDuration;
                yield return null;
            }
            transform.localScale = startScale;
        }

        private IEnumerator PressAnimationRoutine()
        {
            float t = 0f;
            while (t < 1)
            {
                transform.localScale = Vector3.Lerp(startScale, startScale * scaleDownPercent, t);
                t += Time.unscaledDeltaTime / animationDuration;
                yield return null;
            }
        }

    }
}
