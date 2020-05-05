using System;

namespace SwiftFramework.Core
{
    public class DummyBoot : IBoot
    {
        public GlobalEvent AppInitialized => null;

        public GlobalConfig GlobalConfig => null;

        public event Action OnPaused = () => { };
        public event Action OnResumed = () => { };
        public event Action OnInitialized = () => { };
        public event Action<bool> OnFocused = (b) => { };

        public void IgnoreNextPauseEvent()
        {
            
        }

        public IPromise Restart()
        {
            return null;
        }
    }
}
