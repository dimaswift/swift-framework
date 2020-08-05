using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    [ModuleGroup(ModuleGroups.Core)]
    public interface IWindowsManager : IModule
    {
        IPromise<IWindow> Show(WindowLink link);
        void Hide(WindowLink link);
        IPromise<T> ShowChained<T>(WindowLink link = null) where T : IWindow;
        IPromise<T> Show<T>(WindowLink link = null) where T : IWindow;
        IPromise<R> Show<T, R>(WindowLink link = null) where T : IWindowWithResult<R>;
        IPromise<T> Show<T, A>(A args, WindowLink link = null) where T : IWindowWithArgs<A>;
        IPromise<T> ShowChained<T, A>(A args, WindowLink link = null) where T : IWindowWithArgs<A>;
        IPromise<R> Show<T, A, R>(A args, WindowLink link = null) where T : IWindowWithArgsAndResult<A, R>;
        void Hide<T>(WindowLink link = null) where T : IWindow;
        IPromise<T> GetWindow<T>(WindowLink link = null) where T : IWindow;
        IWindow GetBottomWindow();
        IWindow GetTopWindow();
        IEnumerable<IWindow> GetWindowStack();
        void HideTopFullScreenWindow();
        void HideAll();
        IPromise SetStack(params WindowLink[] windows);
        IPromise SetStack(IEnumerable<IWindow> windows);
        IPromise SetStack<W1>() where W1 : IWindow;
        IPromise SetStack<W1, W2>() where W1 : IWindow where W2 : IWindow;
        IPromise SetStack<W1, W2, W3>() where W1 : IWindow where W2 : IWindow where W3 : IWindow;
        IPromise SetStack<W1, W2, W3, W4>() where W1 : IWindow where W2 : IWindow where W3 : IWindow where W4 : IWindow;
        bool IsShowingAnimation();
        void SetTopBarShown(bool visible);
        ITopBar GetTopBar();
        IRootCanvas GetRootCanvas(CanvasType type);
        IPromise MakeTransition(IPromise promiseToWait, Action action);
        bool IsInTransition { get; }
        event Action<IWindow> OnWindowShown;
        event Action<IWindow> OnWindowWillBeShown;
        event Action<IWindow> OnWindowHidden;
        event Action<IWindow> OnWindowWillBeHidden;
        event Action<IWindow> OnWindowJustEnabled;

        void ShowTooltip(string message, Vector3 screenPoint, float duration = 1, Color? color = null);
    }

    public enum CanvasType
    {
        Window = 0, HUD = 1 
    }

    public interface IRootCanvas
    {
        Canvas Canvas { get; }
        float ScreenToWorldSpaceRatio { get; }
    }
}
