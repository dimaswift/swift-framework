using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Swift.Utils.UI
{
    public abstract class ElementFor<T> : Element, IPointerClickHandler
    {
        public event Action<ElementFor<T>> OnClick = v => { };

        public bool Interactable { get; set; } = true;

        public T Value { get; private set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(Interactable == false)
            {
                return;
            }
            OnClicked();
            OnClick(this);
        }

        public void SetUp(T value)
        {
            Value = value;
            OnSetUp(value);
        }

        protected abstract void OnClicked();

        protected abstract void OnSetUp(T value);
    }
}
