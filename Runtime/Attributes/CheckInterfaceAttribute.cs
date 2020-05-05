using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CheckInterfaceAttribute : PropertyAttribute
    {
        public readonly InterfaceSearch autoSeach = InterfaceSearch.None;
        public readonly List<Type> interfaces;

        public CheckInterfaceAttribute(params Type[] interfaces)
        {
            this.interfaces = new List<Type>(interfaces);
        }

        public CheckInterfaceAttribute(InterfaceSearch autoSeach, params Type[] interfaces)
        {
            this.autoSeach = autoSeach;
            this.interfaces = new List<Type>(interfaces);
        }
    }

    [Serializable]
    public abstract class InterfaceComponentField
    {
        public abstract Type InterfaceType { get; }
    }

    [Serializable]
    public abstract class InterfaceComponentField<T> : InterfaceComponentField where T : class
    {
        public void SetActive(bool active)
        {
            if (target)
            {
                target.SetActive(active);
            }
        }

        public bool HasValue => target;

        public override Type InterfaceType => typeof(T);

        public GameObject GameObject => target;

        public T Value
        {
            get
            {
                if (value == null)
                {
                    if (!target)
                    {
                        Debug.LogError($"Unassigned variable on InterfaceComponentField: {typeof(T).Name}");
                        return null;
                    }
                    value = target.GetComponent<T>();
                }
                return value;
            }
        }

        [SerializeField] private GameObject target = null;
        [NonSerialized] private T value;
    }


    [Flags]
    public enum InterfaceSearch
    {
        None = 0, Scene = 1, Assets = 2
    }
}
