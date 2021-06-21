using System;
using UnityEngine;

namespace Swift.Core
{
    public interface IWindow
    {
        IAppearAnimationHandler Animation { get; }
        void Show();
        void Hide();
        bool IsFullScreen { get; }
        bool IsShown { get; }
        RectTransform RectTransform { get; }
        IPromise HidePromise { get; }
        CanvasType CanvasType { get; }
        bool ShowToolbarBackButton { get; }
    }

}
