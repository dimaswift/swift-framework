using System;
using UnityEngine;

namespace Swift.Core.SharedData
{
    [Serializable]
    public class CustomAnimation
    {
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        public float duration = 1f;


        public void Animate(Action<float> handler, Action finishHandler = null)
        {
            App.Core.Timer.Evaluate(duration, t =>
            {
                handler(curve.Evaluate(t));
                if (Mathf.Approximately(t, 1))
                {
                    finishHandler?.Invoke();
                }
            });
        }
    }
}