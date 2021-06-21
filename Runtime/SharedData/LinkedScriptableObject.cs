using System;
using UnityEngine;

namespace Swift.Core
{
    public abstract class LinkedScriptableObject : ScriptableObject, ILinked
    {
        public ILink Link => link;

        [NonSerialized] private Link link = null;

        public void SetLink(Link link)
        {
            if (this.link != null)
            {
                return;
            }
#if UNITY_EDITOR
            App.OnDomainReloaded += OnAppReloaded;
#endif
            this.link = link;
            OnLinked();
        }

        protected virtual void OnAppReloaded()
        {
            link = null;
        }

        protected virtual void OnLinked() { }


    }
}
