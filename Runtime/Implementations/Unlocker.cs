using SwiftFramework.Core;
using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public class Unlocker : IUnlocker
    {
        public IStatefulEvent<UnlockStatus> UnlockStatus => status;

        public PriceLink UnlockPrice => config.price;

        public PriceLink SkipUnlockPrice => config.skipUnlockPrice;

        public long UnlockTimeToReduce => config.unlockTimeReduction.seconds;

        public float UnlockTimeTotal => config.unlockDuration;

        public PriceLink ReduceUnlockTimePrice => config.unlockTimeReduction.price;

        private readonly StatefulEvent<UnlockStatus> status = new StatefulEvent<UnlockStatus>();

        private readonly ILink link;

        private State state;

        private readonly UnlockerConfig config;

        private readonly Func<float> discountHandler;

        public Unlocker(ILink link, UnlockerConfig config, Func<float> discountHandler)
        {
            this.discountHandler = discountHandler;
            this.config = config;
            this.link = link;
            Load();
            status.SetValue(state.unlockStatus);
            status.OnValueChanged += v => state.unlockStatus = v;
            App.Core.Clock.Now.OnValueChanged += Now_OnValueChanged;
            App.Core.Storage.RegisterState(() => state, link);
            App.Core.Storage.OnAfterLoad += Load;
        }


        private void Now_OnValueChanged(long value)
        {
            if (status.Value == Core.UnlockStatus.IsUnlocking && GetTimeTillUnlock() == 0)
            {
                status.SetValue(Core.UnlockStatus.JustUnlocked);
            }
        }

        private void Load()
        {
            if (App.Core.Storage.Exists<State>(link) == false)
            {
                state = new State()
                {
                    unlockStatus = config.defaultStatus,
                    unlockTimestamp = 0
                };
            }
            else
            {
                state = App.Core.Storage.Load<State>(link);
            }

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (status.Value == Core.UnlockStatus.IsUnlocking)
            {
                if (App.Core.Clock.Now.Value >= state.unlockTimestamp)
                {
                    status.SetValue(Core.UnlockStatus.JustUnlocked);
                }
            }
        }

        public void CompleteUnlock()
        {
            status.SetValue(Core.UnlockStatus.Unlocked);
        }

        public long GetTimeTillUnlock()
        {
            long secondsLeft = state.unlockTimestamp - App.Core.Clock.Now.Value;
            if(secondsLeft < 0)
            {
                secondsLeft = 0;
            }
            return secondsLeft;
        }

        public float GetTimeReductionDiscount()
        {
            if (status.Value != Core.UnlockStatus.IsUnlocking)
            {
                return 1;
            }
            float timeTillUnlock = GetTimeTillUnlock();
            if (timeTillUnlock >= config.unlockTimeReduction.seconds)
            {
                return 0;
            }
            return 1f - (timeTillUnlock / config.unlockTimeReduction.seconds);
        }

        public float GetSkipUnlockDiscount()
        {
            float timeTillUnlock = GetTimeTillUnlock();
            return 1f - (timeTillUnlock / config.unlockDuration);
        }

        public float GetUnlockDiscount()
        {
            return discountHandler();
        }

        public IPromise<bool> ReduceUnlockWaitTime()
        {
            Promise<bool> promise = Promise<bool>.Create();
            if (status.Value != Core.UnlockStatus.IsUnlocking || GetTimeTillUnlock() == 0)
            {
                promise.Resolve(false);
                return promise;
            }

            float discount = GetTimeReductionDiscount();

            config.unlockTimeReduction.price.Pay(discount).Done(success => 
            {
                if (success)
                {
                    state.unlockTimestamp -= config.unlockTimeReduction.seconds;
                    if (GetTimeTillUnlock() == 0)
                    {
                        status.SetValue(Core.UnlockStatus.JustUnlocked);
                    }
                    promise.Resolve(true);
                }
                else
                {
                    promise.Resolve(false);
                }
            });

            return promise; 
        }

        public IPromise<bool> SkipUnlockWaitTime()
        {
            Promise<bool> promise = Promise<bool>.Create();
            if (status.Value != Core.UnlockStatus.IsUnlocking)
            {
                promise.Resolve(false);
                return promise;
            }

            float discount = GetSkipUnlockDiscount();
            config.skipUnlockPrice.Pay(discount).Done(success =>
            {
                if (success)
                {
                    state.unlockTimestamp = App.Core.Clock.Now.Value;
                    status.SetValue(Core.UnlockStatus.JustUnlocked);
                    promise.Resolve(true);
                }
                else
                {
                    promise.Resolve(false);
                }
            });
            return promise;
        }

        public IPromise<bool> Unlock()
        {
            Promise<bool> promise = Promise<bool>.Create();
            if (status.Value != Core.UnlockStatus.Locked)
            {
                promise.Resolve(false);
                return promise;
            }

            config.price.Pay(discountHandler()).Done(success =>
            {
                if (success)
                {
                    if (config.unlockDuration == 0)
                    {
                        status.SetValue(Core.UnlockStatus.JustUnlocked);
                    }
                    else
                    {
                        state.unlockTimestamp = App.Core.Clock.Now.Value + config.unlockDuration;
                        status.SetValue(Core.UnlockStatus.IsUnlocking);
                    }
                    promise.Resolve(true);
                }
                else
                {
                    promise.Resolve(false);
                }
            });
            return promise;
        }

        [Serializable]
        private class State
        {
            public UnlockStatus unlockStatus;
            public long unlockTimestamp;
        }
    }
}
