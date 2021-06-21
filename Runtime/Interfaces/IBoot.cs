﻿using System;

namespace Swift.Core
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
        BootConfig Config { get; }
    }
}
