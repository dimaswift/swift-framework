using System;

namespace SwiftFramework.Core
{
    public interface IBoot
    {
        GlobalEvent AppInitialized { get; }
        void IgnoreNextPauseEvent();
        IPromise Restart();
        event Action OnPaused;
        event Action OnResumed;
        event Action OnInitialized;
        event Action<bool> OnFocused;
        GlobalConfig GlobalConfig { get; }
    }
}
