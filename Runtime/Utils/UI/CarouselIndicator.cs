using System;
using System.Collections.Generic;
using SwiftFramework.Core;
using UnityEngine;

namespace SwiftFramework.Utils.UI
{
    public class CarouselIndicator : ElementSet
    {
        public bool MoveToCurrentPage { get; set; }
        
        [SerializeField] private RectTransform activeDot = null;
        [SerializeField] private float dotMoveSpeed = 25;
        
        private readonly List<RectTransform> dots = new List<RectTransform>();

        private int currentPageIndex;
        private int pagesAmount;
        private Action<int> onDotClick;
        
        public void SetCurrentPageIndex(int index)
        {
            currentPageIndex = index;
        }

        public void MoveToNextDot(int direction, float normalizedPosition)
        {
            if ((currentPageIndex == 0 && direction < 0) || (currentPageIndex >= pagesAmount - 1 && direction > 0))
            {
                return;   
            }
            RectTransform nextDot = dots[currentPageIndex + direction];
            RectTransform currentDot = dots[currentPageIndex];
            activeDot.position = Vector3.Lerp(currentDot.position, nextDot.position, normalizedPosition);
        }
        
        public void Prepare(int pagesAmount, int currentPageIndex, Action<int> onDotClick)
        {
            this.onDotClick = onDotClick;
            this.pagesAmount = pagesAmount;
            
            SetCurrentPageIndex(currentPageIndex);

            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, pagesAmount);
            
            IEnumerable<int> GetPages()
            {
                for (int i = 0; i < pagesAmount; i++)
                {
                    yield return i;
                }
            }
            
            dots.Clear();

            if (pagesAmount > 1)
            {
                SetUp<NumericElement, int>(GetPages(), OnDotClick);
            }
            else
            {
                Clear();
            }

            if (pagesAmount > 1)
            {
                foreach (NumericElement activeElement in GetActiveElements<NumericElement>())
                {
                    dots.Add(activeElement.RectTransform);
                }
            
                App.Core.Timer.WaitForNextFrame().Done(() =>
                {
                    activeDot.position = dots[currentPageIndex].position;
                    activeDot.SetAsLastSibling();
                });
            }
            
            activeDot.gameObject.SetActive(pagesAmount > 1);
        }

        private void OnDotClick(ElementFor<int> element)
        {
            onDotClick?.Invoke(element.Value);
        }

        private void Update()
        {
            if (MoveToCurrentPage == false)
            {
                return;
            }
            if (currentPageIndex < dots.Count)
            {
                Vector3 targetPos = dots[currentPageIndex].position;
                if (Vector3.SqrMagnitude(targetPos - activeDot.position) > float.Epsilon)
                {
                    activeDot.position = Vector3.Lerp(activeDot.position, targetPos, Time.unscaledDeltaTime * dotMoveSpeed);   
                }
            }
        }
    }
}