using System.Collections;
using SwiftFramework.Core;
using SwiftFramework.Core.SharedData;
using UnityEngine;

namespace SwiftFramework.Utils.UI
{
    public class ScaleAnimation : MonoBehaviour, IGenericAnimation
    {
        [SerializeField] private bool unscaledTime = true;
        [SerializeField] private CustomAnimation customAnimation = new CustomAnimation();
        [SerializeField] private Vector3 startScale = new Vector3(1, 1, 1);
        
        
        public IPromise Animate()
        {
            Promise promise = Promise.Create();
            if (gameObject.activeSelf == false)
            {
                gameObject.SetActive(true);
            }
            StartCoroutine(AnimationRoutine(promise));
            return promise;
        }

        private IEnumerator AnimationRoutine(Promise promise)
        {
            float timer = 0f;
            while (timer < 1f)
            {
                transform.localScale = startScale * customAnimation.curve.Evaluate(timer);
                timer += unscaledTime ? Time.unscaledDeltaTime / customAnimation.duration : Time.deltaTime / customAnimation.duration;
                yield return null;
            }
            promise.Resolve();
        }
    }
}