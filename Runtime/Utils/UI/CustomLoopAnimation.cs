using UnityEngine;

namespace SwiftFramework.Utils.UI
{
    public class CustomLoopAnimation : MonoBehaviour
    {
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float duration = 1f;

        private float time;

        private void Update()
        {
            transform.localScale = Vector3.one * curve.Evaluate(time);
            time += Time.unscaledDeltaTime / duration;
            if(time >= 1)
            {
                time = 0;
            }
        }
    }
}
