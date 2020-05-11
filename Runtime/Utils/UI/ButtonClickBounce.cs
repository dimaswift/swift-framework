using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwiftFramework.Utils.UI
{
    [RequireComponent(typeof(BounceAnimation))]
    public class ButtonClickBounce : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerExitHandler
    {
        private BounceAnimation bounce;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            bounce = gameObject.GetComponent<BounceAnimation>();
            if(bounce == null)
            {
                bounce = gameObject.AddComponent<BounceAnimation>();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button && button.interactable == false)
            {
                return;
            }
            bounce?.Click();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (button && button.interactable == false)
            {
                return;
            }
            bounce?.Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (button && button.interactable == false)
            {
                return;
            }
            bounce?.Release();
        }
    }
}
