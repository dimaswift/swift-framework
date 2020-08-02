using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SwiftFramework.Utils.UI
{
    public class CarouselElementSet : ElementSet, IDragHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            Debug.LogError(eventData.delta.x);
        }
    }
}