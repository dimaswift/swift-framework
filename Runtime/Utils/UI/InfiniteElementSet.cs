using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Swift.Utils.UI
{
    public class InfiniteElementSet : ElementSet
    {
        [SerializeField] private float spacing = 0;
        [SerializeField] private ScrollRect scrollRect = null;

        private readonly List<object> valuesCache = new List<object>();

        private float elementHeight;
        private int currentPivot;
        private int visibleElementCount;

        private Element[] currentElementsList;
        private Element[] elementsCache;

        public void UpdateElements<E, T>() where E : ElementFor<T> where T : class
        {
            if (scrollRect == null)
            {
                return;
            }

            if (valuesCache.Count < 1)
            {
                return;
            }

            int pivot = Mathf.Clamp(Mathf.CeilToInt((scrollRect.content.anchoredPosition.y - elementHeight) / elementHeight), 0, valuesCache.Count - visibleElementCount);

            while (pivot > currentPivot)
            {
                if (pivot + visibleElementCount - 1 >= valuesCache.Count)
                {
                    currentPivot = pivot;
                    break;
                }

                currentPivot += (int)Mathf.Sign(pivot - currentPivot);

                Element[] current = currentElementsList;

                Array.Copy(currentElementsList, 1, elementsCache, 0, currentElementsList.Length - 1);

                E elementToSwap = currentElementsList[0] as E;

                elementsCache[elementsCache.Length - 1] = elementToSwap;

                T value = (T)valuesCache[currentPivot + visibleElementCount - 1];

                elementToSwap.SetUp(value);

                currentElementsList = elementsCache;

                elementsCache = current;

                float y = elementToSwap.RectTransform.anchoredPosition.y - elementHeight * visibleElementCount;

                elementToSwap.RectTransform.anchoredPosition = new Vector2(elementToSwap.RectTransform.anchoredPosition.x, y);
            }

            while (pivot < currentPivot)
            {
                if (pivot < 0)
                {
                    currentPivot = 0;
                    break;
                }

                currentPivot += (int)Mathf.Sign(pivot - currentPivot);

                Element[] current = currentElementsList;

                Array.Copy(currentElementsList, 0, elementsCache, 1, currentElementsList.Length - 1);

                E elementToSwap = currentElementsList[visibleElementCount - 1] as E;

                elementsCache[0] = elementToSwap;

                T value = (T)valuesCache[currentPivot];

                elementToSwap.SetUp(value);

                currentElementsList = elementsCache;

                elementsCache = current;

                float y = elementToSwap.RectTransform.anchoredPosition.y + elementHeight * visibleElementCount;

                elementToSwap.RectTransform.anchoredPosition = new Vector2(elementToSwap.RectTransform.anchoredPosition.x, y);
            }

        }

        public override void SetUp<E, T>(IEnumerable<T> values, Action<T, E> initMethod)
        {
            elementHeight = element.RectTransform.rect.height + spacing;
            visibleElementCount = Mathf.CeilToInt(scrollRect.viewport.rect.height / elementHeight) + 1;
            float contentHeight = elementHeight * values.Count();
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, contentHeight);
            currentPivot = 0;
            valuesCache.Clear();
            Clear();
            currentElementsList = new Element[visibleElementCount];
            elementsCache = new Element[visibleElementCount];
            int i = 0;
            foreach (T value in values)
            {
                if (i < visibleElementCount)
                {
                    E e;
                    if (i < elements.Count)
                    {
                        e = elements[i].GetComponent<E>();
                    }
                    else
                    {
                        e = Instantiate(element, scrollRect.content).GetComponent<E>();
                        elements.Add(e);
                    }
                    currentElementsList[i] = e;
                    initMethod(value, e);
                    e.RectTransform.anchoredPosition = new Vector2(e.RectTransform.anchoredPosition.x, i * -elementHeight);
                    e.gameObject.SetActive(true);
                }
                valuesCache.Add(value);
                i++;
            }
        }
    }
}
