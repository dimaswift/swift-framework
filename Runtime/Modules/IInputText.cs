using System;
using UnityEngine.Events;

namespace SwiftFramework.Core
{
    public interface IInputText
    {
        int CharacterLimit { get; set; }
        string Text { get; set; }
        void AddListener(UnityAction<string> action);
    }
    [Serializable]
    public class GenericInputText : InterfaceComponentField<IInputText> { }
}