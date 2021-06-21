using System;

namespace Swift.Core
{
    [BuiltInModule]
    [ModuleGroup(ModuleGroups.Core)]
    public interface IClock : IModule
    {
        IPromise<long> GetUnixNow();
        IStatefulEvent<long> Now { get; }
        IPromise<DateTime> GetNow();
        long GetSecondsLeft(long timeStamp);
    }
}
