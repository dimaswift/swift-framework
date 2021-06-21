using System;
using System.Collections.Generic;

namespace Swift.Core
{
    [BuiltInModule]
    [ModuleGroup(ModuleGroups.Core)]
    public interface ITimer : IModule
    {
        event Action OnUpdate;
        IPromise WaitFor(float seconds);
        IPromise WaitForAll(IEnumerable<Action> actions);
        IPromise WaitForUnscaled(float seconds);
        IPromise WaitForNextFrame();
        IPromise WaitForFixedUpdate();
        IPromise WaitUntil(Func<bool> condition);
        IPromise Evaluate(float duration, Action<float> callback);
        bool Cancel(IPromise promise);
        IPromise WaitForMainThread();
    }
}
