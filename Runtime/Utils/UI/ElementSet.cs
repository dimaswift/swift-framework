using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Swift.Utils.UI
{
    public class ElementSet : MonoBehaviour
    {
        [SerializeField] protected Element element = null;

        private void OnValidate()
        {
            if(element == null && transform.childCount > 0)
            {
                element = transform.GetChild(0).GetComponent<Element>();
            }
        }

        protected readonly List<Element> elements = new List<Element>();

        public int ActiveElementsCount => elements.Count(e => e.gameObject.activeSelf);

        public IEnumerable<T> GetActiveElements<T>()
        {
            foreach (var e in elements)
            {
                if (e.gameObject.activeSelf)
                {
                    yield return e.GetComponent<T>();
                }
            }
        }

        public T GetElementAt<T>(int index) where T : Element
        {
            return elements[index] as T;
        }

        public virtual void SetUp<E, T>(IEnumerable<T> objects, Action<T, E> initMethod) where E : Element
        {
            if (IsElementOfType<E>() == false)
            {
                return;
            }
            element.gameObject.SetActive(false);
            Clear();
            int i = 0;
            foreach (var o in objects)
            {
                E e;
                if (i < elements.Count)
                {
                    e = elements[i].GetComponent<E>();
                }
                else
                {
                    e = Instantiate(element, transform).GetComponent<E>();
                    e.Init();
                    elements.Add(e);
                }
                e.Show();
                initMethod(o, e);
                i++;
            }
        }

        protected bool IsElementOfType<E>()
        {
            if (element == null)
            {
                Debug.LogError($"Cannot init ElementSet <b>{name}</b>, element source is null!");
                return false;
            }

            if (element.GetComponent<E>() == null)
            {
                Debug.LogError($"Cannot init ElementSet <b>{name}</b> for type <b>{typeof(E).Name}</b>!  <b>{element.name}</b> does not have this component attached!");
                return false;
            }
            return true;
        }

        public void SetUp<E, T>(IEnumerable<T> objects) where E : ElementFor<T>
        {
            SetUp<E, T>(objects, (o, e) => e.SetUp(o));
        }

        public void SetUp<E, T>(IEnumerable<T> objects, Action<ElementFor<T>> onClick) where E : ElementFor<T>
        {
            SetUp<E, T>(objects, (o, e) => e.SetUp(o));
            foreach (var element1 in elements)
            {
                var e = (ElementFor<T>) element1;
                e.OnClick -= onClick;
                e.OnClick += onClick;
            }
        }

        public void Clear()
        {
            element.gameObject.SetActive(false);
            foreach (var e in elements)
            {
                e.gameObject.SetActive(false);
                e.Hide();
            }
        }
    }
}
