using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public class ScriptableController<TState, TLink> : LinkedScriptableObject, ILinked where TLink : class, ILink where TState : new()
    {
        [NonSerialized] protected TState state;

        protected override void OnLinked()
        {
            App.Core.Storage.RegisterState(() => state, Link);
            App.Core.Storage.OnBeforeSave += OnSave;
            state = App.Core.Storage.LoadOrCreateNew(Link, GetDefaultState);
            OnLoaded(state);
        }

        protected virtual TState GetDefaultState() => new TState();
        protected virtual void OnSave() { }
        protected virtual void OnLoaded(TState state) { }

    }
}
