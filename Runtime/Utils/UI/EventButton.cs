using SwiftFramework.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SwiftFramework.Utils.UI
{
    public class EventButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private GlobalEventLink click = null;
        [SerializeField] private GlobalEventLink up = null;
        [SerializeField] private GlobalEventLink down = null;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (click.IsEmpty)
            {
                return;
            }
            click.Value.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (down.IsEmpty)
            {
                return;
            }
            down.Value.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (up.IsEmpty)
            {
                return;
            }
            up.Value.Invoke();
        }
    }
}