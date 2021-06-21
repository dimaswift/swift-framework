using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Swift.Utils.UI
{
    public class SimpleToggle : Element, IPointerClickHandler
    {
        public bool IsOn => isOn;
        public ToggleEvent OnValueChanged => onValueChanged;

        [SerializeField] private bool isOn = false;
        [SerializeField] private ToggleEvent onValueChanged = null;

        [SerializeField] private GameObject enabledStateObject = null;
        [SerializeField] private GameObject disabledStateObject = null;

        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }
        
        public void SetValue(bool isOn)
        {
            this.isOn = isOn;
            SetGraphics();
        }

        public void Toggle()
        {
            isOn = !isOn;
            onValueChanged.Invoke(isOn);
            SetGraphics();
        }

        private void Awake()
        {
            SetGraphics();
        }

        private void SetGraphics()
        {
            enabledStateObject.SetActive(isOn);
            disabledStateObject.SetActive(!isOn);
        }

        [System.Serializable]
        public class ToggleEvent : UnityEvent<bool>
        {

        }
    }

}
