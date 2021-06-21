using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swift.Core
{
    [CreateAssetMenu(menuName = "SwiftFramework/Events/GlobalEvent", fileName = "GlobalEvent")]
    [PrewarmAsset]
    public class GlobalEvent : ScriptableObject
    {
        [SerializeField] private EventArgumentsLink defaultArguments = Link.CreateNull<EventArgumentsLink>();

        [NonSerialized] private readonly HashSet<GlobalEventWithArgsHandler> listenersWithArgs = new HashSet<GlobalEventWithArgsHandler>();
        [NonSerialized] private readonly HashSet<GlobalEventHandler> listeners = new HashSet<GlobalEventHandler>();
        
        public void Invoke(EventArguments arguments)
        {
            foreach (GlobalEventWithArgsHandler eventListener in listenersWithArgs)
            {
                try
                {
                    eventListener?.Invoke(arguments);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception thrown while trying to invoke {name} event on {eventListener}:\n {e.Message}");
                }
            }
        }
        
        public void Invoke()
        {
            foreach (GlobalEventHandler eventListener in listeners)
            {
                try
                {
                    eventListener?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception thrown while trying to invoke {name} event on {eventListener}:\n {e.Message}");
                }
            }
        }

        public void AddListener(GlobalEventWithArgsHandler eventHandler)
        {
            if (listenersWithArgs.Contains(eventHandler))
            {
                return;
            }
            listenersWithArgs.Add(eventHandler);
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
        
        public bool RemoveListener(GlobalEventWithArgsHandler eventHandler)
        {
            return listenersWithArgs.Remove(eventHandler);
        }

        public void RemoveAllListeners()
        {
            listeners.Clear();
            listenersWithArgs.Clear();
        }
    }

    public delegate void GlobalEventWithArgsHandler(EventArguments arguments);
    public delegate void GlobalEventHandler();

}
