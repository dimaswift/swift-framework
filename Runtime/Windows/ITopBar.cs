using UnityEngine;

namespace Swift.Core
{
    public interface ITopBar
    {
        void Init();
        bool IsShown { get; set; }
        RectTransform RectTransform { get; }
    }
}
