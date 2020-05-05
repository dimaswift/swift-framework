using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SwiftFramework.Core;
namespace SwiftFramework.CoroutineManager
{
    [DefaultModule]
    [DisallowCustomModuleBehaviours]
    internal class CoroutineManager : BehaviourModule, ICoroutineManager
    {
        public Coroutine Begin(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }

        public void Begin(IEnumerator coroutine, ref Coroutine current)
        {
            current = StartCoroutine(coroutine);
        }

        public void Stop(ref Coroutine coroutine)
        {
            if(coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
           
        }
    }
}
 