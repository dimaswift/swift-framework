﻿using System;

namespace Swift.Core
{
    public interface ILogger
    {
        void Log(object message);
        void LogError(object message);
        void LogWarning(object message);
        void LogException(Exception exception);
    }
}
