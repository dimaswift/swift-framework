using System;
using UnityEngine.Events;

namespace SwiftFramework.Core
{
    public interface IButton
    {
        bool Interactable { get; set; }
        void AddListener(UnityAction action);
    }

    [Serializable]
    public class GenericButton : InterfaceComponentField<IButton>
    {
        public bool Interactable
        {
            get => Value.Interactable;
            set => Value.Interactable = value;
        }
        
        public void AddListener(UnityAction action)
        {
            Value.AddListener(action);
        }
    }

}