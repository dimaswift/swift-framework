using System;
using UnityEngine;

namespace Swift.Utils.UI
{
    public class CustomLoopAnimation : MonoBehaviour
    {
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float duration = 1f;
        [SerializeField] private float delay = 0f;

        private float time;

        private float waitTimer;
        
        private void OnEnable()
        {
            waitTimer = delay;
        }

        private void Update()
        {
            waitTimer -= Time.unscaledDeltaTime;

            if (waitTimer > 0)
            {
                return;
            }
            
            transform.localScale = Vector3.one * curve.Evaluate(time);
            time += Time.unscaledDeltaTime / duration;
            if(time >= 1)
            {
                time = 0;
            }
        }
    }
}
