using System;
using System.Collections.Generic;
using SwiftFramework.Core;
using SwiftFramework.Core.Windows;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SwiftFramework.Utils.UI
{
    public class CarouselElementSet : ElementSet, IDragHandler, IEndDragHandler, IBeginDragHandler, ISelectableSet
    {
        public event Action<int> OnSelectionChanged = i => { };
        
        [SerializeField] private float dragSpeedMultiplier = 1;
        [SerializeField] private float drag = 10;
        [SerializeField] private float swipeThreshold = 5;
        [SerializeField] private CarouselIndicator indicator = null;
        
        [SerializeField] [Range(0.2f, 1f)] private float maxSwipeThresholdPercent = .4f;

        private bool dragging;

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                selectedIndex = value;
                if (indicator != null)
                {
                    indicator.SetCurrentPageIndex(selectedIndex);
                }

                OnSelectionChanged(value);
            }
        }

        private int selectedIndex;
        
        private int elementsCount;
        
        private float viewportWidth;

        private IRootCanvas canvas;
        
        public override void SetUp<E, T>(IEnumerable<T> objects, Action<T, E> initMethod)
        {
            base.SetUp(objects, initMethod);
            elementsCount = ActiveElementsCount;
            
            if (indicator != null)
            {
                indicator.Prepare(elementsCount, selectedIndex, OnIndicatorDotClick);
            }
            
            RectTransform rect = GetComponent<RectTransform>();
            Rect parentRect = rect.parent.GetComponent<RectTransform>().rect;
            
            canvas = GetComponentInParent<IRootCanvas>();
            
            viewportWidth = parentRect.width;
            
            rect.pivot = new Vector2(0, rect.pivot.y);
            
            Vector3 pos = transform.localPosition;
            pos.x = GetTargetPosition();
            transform.localPosition = pos;
            
            foreach (E activeElement in GetActiveElements<E>())
            {
                activeElement.RectTransform.sizeDelta = new Vector2(viewportWidth, activeElement.RectTransform.sizeDelta.y);
            }

            if (selectedIndex >= elementsCount)
            {
                selectedIndex = 0;
            }
        }

        private void OnIndicatorDotClick(int index)
        {
            SelectedIndex = index;
        }

        private void Update()
        {
            if (indicator != null)
            {
                indicator.MoveToCurrentPage = dragging == false;
            }
            
            if (dragging == false)
            {
                float targetPosX = GetTargetPosition();
                
                Vector3 pos = transform.localPosition;
                
                if (Mathf.Abs(pos.x - targetPosX) > float.Epsilon)
                {
                    pos.x = Mathf.Lerp(pos.x, targetPosX, Time.unscaledDeltaTime * drag);
                    transform.localPosition = pos;
                }
            }
            else
            {
                if (indicator != null)
                {
                    float targetPosX = GetTargetPosition();
                    Vector3 pos = transform.localPosition;
                    int dir = (int) Mathf.Sign(targetPosX - pos.x);
                    indicator.MoveToNextDot(dir, Mathf.InverseLerp(targetPosX, GetTargetPosition(dir), pos.x));
                }
            }
        }

        private float GetTargetPosition(int offset = 0)
        {
            return -((SelectedIndex + offset) * viewportWidth) - (viewportWidth / 2);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                return;
            }
            
            Vector3 pos = transform.localPosition;

            float multiplier = dragSpeedMultiplier;

            float worldDelta = eventData.delta.x / canvas.Canvas.transform.localScale.x /
                                canvas.ScreenToWorldSpaceRatio;

            if (SelectedIndex == 0 || SelectedIndex == elementsCount - 1)
            {
                float offset = pos.x - GetTargetPosition();

                if (SelectedIndex == 0 && offset > 0 || SelectedIndex == elementsCount - 1 && offset < 0)
                {
                    if (multiplier > 0)
                    {
                        multiplier -= (Mathf.Abs(offset / viewportWidth)) * 5;
                    }
                }
            }

            pos.x += worldDelta * multiplier;
            
            transform.localPosition = pos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            
            Vector3 pos = transform.localPosition;
            
            float offset = pos.x - GetTargetPosition();

            int swipeDirection = 0;

            float swipeMagnitude = Mathf.Abs(eventData.delta.x) / canvas.Canvas.transform.localScale.x /
                                   canvas.ScreenToWorldSpaceRatio;

            if (Mathf.Abs(offset) / viewportWidth >= maxSwipeThresholdPercent)
            {
                swipeDirection = (int)Mathf.Sign(offset);
            }
            else if (swipeMagnitude >= swipeThreshold)
            {
                swipeDirection = (int)Mathf.Sign(eventData.delta.x);
            }

            if (swipeDirection != 0)
            {
                if (swipeDirection > 0 && SelectedIndex > 0)
                {
                    SelectedIndex--;
                }
                else if (swipeDirection < 0 && SelectedIndex < elementsCount - 1)
                {
                    SelectedIndex++;
                }

                SelectedIndex = Mathf.Clamp(SelectedIndex, 0, elementsCount - 1);
            }
            else
            {
                MoveToClosestElement();
            }
        }

        private void MoveToClosestElement()
        {
            int elementIndex = 0;
            float minDistance = float.MaxValue;
            
            foreach (Element activeElement in GetActiveElements<Element>())
            {
                Vector3 pos = activeElement.RectTransform.position;
                float distance = Mathf.Abs(pos.x - transform.parent.position.x);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    SelectedIndex = elementIndex;
                }
                elementIndex++;
            }

        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
        }
    }
}