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
            this.link = link;
            OnLinked();
        }

        protected virtual void OnLinked() { }


    }
}
