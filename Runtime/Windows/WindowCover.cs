﻿using Swift.Core.Windows;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Swift.Core.Windows
{
    public class WindowCover : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            GetComponentInParent<Window>()?.HandleCloseButtonClick();
        }
    }
}
