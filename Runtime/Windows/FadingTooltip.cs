using System;
using System.Collections;
using SwiftFramework.Core.Views;
using UnityEngine;

namespace SwiftFramework.Core.Windows
{
    public class FadingTooltip : View, ITooltip
    {
        [SerializeField] private float moveUpSpeed = 10;
        [SerializeField] private float damping = 10;
        [SerializeField] private GenericText messageText = new GenericText();
        [SerializeField] private AnimationCurve fadingCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        public void Show(Vector2 screenPoint, RectTransform parent, string message, float duration, Color color)
        {
            messageText.Text = message;
    
            Transform myTransform = transform;
            myTransform.SetParent(parent);
            myTransform.localScale = Vector3.one;
            messageText.Value.Color = color;
            transform.position = screenPoint;
            StartCoroutine(FadingRoutine(duration));
        }

        private IEnumerator FadingRoutine(float duration)
        {
            float speed = moveUpSpeed;
            float timer = duration;
            while (timer > 0)
            {
                timer -= Time.unscaledDeltaTime;
                transform.Translate(Vector3.up * speed);
                speed = Mathf.Max(0, speed - Time.unscaledDeltaTime * damping);
                Color color = messageText.Value.Color;
                color.a = fadingCurve.Evaluate(timer / duration);
                messageText.Value.Color = color;
                yield return null;
            }
            ReturnToPool();
        }
    }
    
    
    public interface ITooltip : IView
    {
        void Show(Vector2 screenPoint, RectTransform parent, string message, float duration, Color color);
    }

    [Serializable]
    public class GenericTooltip : InterfaceComponentField<ITooltip> {}
}