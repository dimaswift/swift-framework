using System;
using UnityEngine;

namespace Swift.Core
{
    public class ScriptableController<TState, TLink> : LinkedScriptableObject, ILinked where TLink : class, ILink where TState : new()
    {
        [NonSerialized] protected TState state;

        protected override void OnLinked()
        {
            App.Core.Storage.RegisterState(() => state, Link);
            App.Core.Storage.OnBeforeSave += OnSave;
            state = App.Core.Storage.LoadOrCreateNew(Link, GetDefaultState);
#if UNITY_EDITOR
            App.OnDomainReloaded += ResetState;
#endif
            OnLoaded(state);
        }
        
        protected virtual TState GetDefaultState() => new TState();
        protected virtual void OnSave() { }
        protected virtual void OnLoaded(TState state) { }
        
        protected virtual void ResetState() {}

    }
}
