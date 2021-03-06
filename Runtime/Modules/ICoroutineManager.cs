﻿using System.Collections;
using UnityEngine;

namespace SwiftFramework.Core
{
    [BuiltInModule]
    [ModuleGroup(ModuleGroups.Core)]
    public interface ICoroutineManager : IModule
    {
        Coroutine Begin(IEnumerator coroutine);
        void Begin(IEnumerator coroutine, ref Coroutine current);
        void Stop(ref Coroutine coroutine);
    }
}
