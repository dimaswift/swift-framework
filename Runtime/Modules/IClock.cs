using System;

namespace SwiftFramework.Core
{
    [ModuleGroup(ModuleGroups.Core)]
    public interface IClock : IModule
    {
        IPromise<long> GetUnixNow();
        IStatefulEvent<long> Now { get; }
        IPromise<DateTime> GetNow();
        long GetSecondsLeft(long timeStamp);
    }
}
