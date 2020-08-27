using System;
using UnityEngine;

namespace SwiftFramework.Core
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
            App.OnDomainReloaded += AppOnOnDomainReloaded;
#endif
            this.link = link;
            OnLinked();
        }

        private void AppOnOnDomainReloaded()
        {
            link = null;
        }

        protected virtual void OnLinked() { }


    }
}
