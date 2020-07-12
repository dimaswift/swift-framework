using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public interface IText
    {
        Color Color { get; set; }
        string Text { get; set; }
        void SetAsync<T>(IPromise<T> promise);
        void SetAsync<T>(IPromise<T> promise, string format);
    }
    
    [Serializable]
    public class GenericText : InterfaceComponentField<IText> 
    {
        public string Text
        {
            get => Value?.Text;
            set
            {
                if (HasValue)
                {
                    Value.Text = value;
                }
            }
        }
    }
}