using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public class DummyLogger : ILogger
    {
        public void Log(object message)
        {
            Debug.Log(message);
        }

        public void LogError(object message)
        {
            Debug.LogError(message);
        }

        public void LogException(Exception exception)
        {
            Debug.LogException(exception); 
        }

        public void LogWarning(object message)
        {
            
        }
    }
}
