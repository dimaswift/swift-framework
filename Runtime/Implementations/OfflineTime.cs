using System;

namespace SwiftFramework.Core
{
    public class ManualClock : DummyModule, IClock
    {
        public void AddOffset(long offset)
        {
            now.SetValue(now.Value + offset);
        }

        public ManualClock(long now)
        {
            this.now = new StatefulEvent<long>(now);
        }

        private readonly StatefulEvent<long> now;

        public IStatefulEvent<long> Now => now;

        public IPromise<DateTime> GetNow()
        {
            return Promise<DateTime>.Resolved(DateTimeOffset.FromUnixTimeSeconds(now.Value).DateTime);
        }

        public long GetSecondsLeft(long timeStamp)
        {
            return timeStamp - now.Value;
        }

        public IPromise<long> GetUnixNow()
        {
            return Promise<long>.Resolved(now.Value);
        }
    }
}
