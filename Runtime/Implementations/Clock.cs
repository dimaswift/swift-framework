using System;

namespace SwiftFramework.Core
{
    [DefaultModule]
    [DisallowCustomModuleBehaviours]
    public class Clock : BehaviourModule, IClock
    {
        public IStatefulEvent<long> Now
        {
            get
            {
                Update();
                return now;
            }
        }

        private long prevNow;

        private readonly StatefulEvent<long> now = new StatefulEvent<long>(0);
   
        protected override IPromise GetInitPromise()
        {
            Promise result = Promise.Create();
            GetUnixNow().Done(now => 
            {
                result.Resolve();
            });
            return result;
        }

        private void Update()
        {
            if(Initialized == false)
            {
                return;
            }
            var newNow = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (prevNow != newNow)
            {
                prevNow = newNow;
                now.SetValue(newNow);
            }
        }

        public IPromise<DateTime> GetNow()
        {
            return Promise<DateTime>.Resolved(DateTime.Now);
        }

        public long GetSecondsLeft(long timeStamp)
        {
            if(Now.Value > timeStamp)
            {
                return 0;
            }

            return timeStamp - Now.Value;
        }

        public IPromise<long> GetUnixNow()
        {
            return Promise<long>.Resolved(DateTimeOffset.Now.ToUnixTimeSeconds());
        }
    }
}