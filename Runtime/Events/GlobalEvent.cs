using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    [CreateAssetMenu(menuName = "SwiftFramework/Events/GlobalEvent", fileName = "GlobalEvent")]
    public class GlobalEvent : ScriptableObject
    {
        public EventArguments DefaultArguments => defaultArguments.Value;

        [SerializeField] private EventArgumentsLink defaultArguments = Link.CreateNull<EventArgumentsLink>();

        [NonSerialized] private readonly List<GlobalEventHandler> listeners = new List<GlobalEventHandler>();

        public void Invoke()
        {
            Invoke(defaultArguments.Value);
        }

        public void Invoke(EventArguments arguments)
        {
            foreach (GlobalEventHandler eventListener in listeners)
            {
                try
                {
                    eventListener?.Invoke(arguments);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exeption thrown while trying to invoke {name} event on {eventListener}:\n {e.Message}");
                }
            }
        }

        public void AddListener(GlobalEventHandler eventHandler)
        {
            if (listeners.Contains(eventHandler))
            {
                return;
            }
            listeners.Add(eventHandler);
        }

        public bool RemoveListener(GlobalEventHandler eventHandler)
        {
            return listeners.Remove(eventHandler);
        }

        public void RemoveAllListeners()
        {
            listeners.Clear();
        }
    }

    public delegate void GlobalEventHandler(EventArguments arguments);

}
