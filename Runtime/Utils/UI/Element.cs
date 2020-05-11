using System;
using UnityEngine;

namespace SwiftFramework.Utils.UI
{
    public class Element : MonoBehaviour
    {
        [NonSerialized] private RectTransform rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                if(rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }
                return rectTransform;
            }
        }

        public virtual void Clear()
        {

        }

        public virtual void Init()
        {

        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
