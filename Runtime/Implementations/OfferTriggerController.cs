using SwiftFramework.Core.SharedData.Shop;
using SwiftFramework.Core.SharedData.Shop.OfferSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    public class OfferTriggerController : IOfferTriggerController
    {
        public event Action<(OfferTriggerLink trigger, long expireTimestamp)> OnOfferTriggered = i => { };
        public event Action<OfferTriggerLink> OnOfferExpired = o => { };

        private readonly List<OfferTriggerLink> triggers = new List<OfferTriggerLink>();

        private OffersState state;

        [Serializable]
        private class OffersState
        {
            public long firstSessionTimestamp;
            public List<OfferTriggerState> scheduledTriggers = new List<OfferTriggerState>();
            public List<OfferTriggerState> activeTriggers = new List<OfferTriggerState>();
            public List<OfferTriggerLink> retiredTriggers = new List<OfferTriggerLink>();
        }

        [Serializable]
        private class OfferTriggerState
        {
            public OfferTriggerLink trigger;
            public long timestamp;
        }

        private readonly IShopManager shop;

        private readonly Queue<OfferTriggerLink> windowShowQueue = new Queue<OfferTriggerLink>();

        public OfferTriggerController(IShopManager shop, IEnumerable<OfferTriggerLink> triggers)
        {
            this.shop = shop;
            state = App.Core.Storage.LoadOrCreateNew(CreateDefaultState);
            App.Core.Storage.RegisterState(() => state);

            App.WaitForState(AppState.ModulesInitialized, () =>
            {
                shop.OnTransactionCompleted += OnTransactionCompleted;
                App.OnClockTick += App_OnClockTick;
                this.triggers.Clear();
                this.triggers.AddRange(triggers);

                foreach (var t in triggers)
                {
                    InitTrigger(t);
                }
            });
        }

        private OffersState CreateDefaultState()
        {
            return new OffersState()
            {
                firstSessionTimestamp = App.Now,
                activeTriggers = new List<OfferTriggerState>(),
                retiredTriggers = new List<OfferTriggerLink>(),
                scheduledTriggers = new List<OfferTriggerState>()
            };
        }

        private void App_OnClockTick(long now)
        {
            for (int i = state.activeTriggers.Count - 1; i >= 0; i--)
            {
                if (state.activeTriggers[i].timestamp != -1 && now >= state.activeTriggers[i].timestamp)
                {
                    OfferTriggerLink triggerLink = state.activeTriggers[i].trigger;
                    Expire(triggerLink);
                }
            }

            for (int i = state.scheduledTriggers.Count - 1; i >= 0; i--)
            {
                if (now >= state.scheduledTriggers[i].timestamp)
                {
                    OfferTriggerLink triggerLink = state.scheduledTriggers[i].trigger;
                    if (TryTriggerOffer(triggerLink))
                    {
                        state.scheduledTriggers.RemoveAt(i);
                    }
                }
            }
        }

        private void Expire(OfferTriggerLink offerTrigger)
        {
            Debug.Log($"{offerTrigger} Expired");
            state.activeTriggers.RemoveAll(t => t.trigger == offerTrigger);
            OnOfferExpired(offerTrigger);
            if (offerTrigger.Value.repeat)
            {
                TrySchedule(offerTrigger, App.Now + offerTrigger.Value.repeatAfterTime.seconds);
            }
            else
            {
                Retire(offerTrigger);
            }
        }

        private void Purchase(OfferTriggerLink offerTrigger)
        {
            state.activeTriggers.RemoveAll(t => t.trigger == offerTrigger);

            OnOfferExpired(offerTrigger);

            if (offerTrigger.Value.retireAfterPurchase)
            {
                Retire(offerTrigger);
            }
            else
            {
                TrySchedule(offerTrigger, App.Now + offerTrigger.Value.repeatAfterTime.seconds);
            }

            OnOfferPurchased(offerTrigger.Value);
        }

        private void Retire(OfferTriggerLink offerTrigger)
        {
            Debug.Log($"{offerTrigger} retired");
            state.retiredTriggers.Add(offerTrigger);
        }

        private void TrySchedule(OfferTriggerLink offerTrigger, long timestamp)
        {
            if (state.scheduledTriggers.FindIndex(t => t.trigger == offerTrigger) != -1)
            {
                return;
            }
            Debug.Log($"{offerTrigger} Scheduled");
            state.scheduledTriggers.Add(new OfferTriggerState()
            {
                trigger = offerTrigger,
                timestamp = timestamp
            });
        }

        private void OnTransactionCompleted(ShopItemLink item, TransactionResult result)
        {
            if (result == TransactionResult.Success)
            {
                foreach (OfferTriggerLink triggerLink in triggers)
                {
                    if (triggerLink.Value.offer == item)
                    {
                        Purchase(triggerLink);
                        break;
                    }
                }
            }
        }

        private void OnOfferPurchased(OfferTrigger trigger)
        {
            foreach (OfferTriggerListener listener in trigger.GetListeners())
            {
                listener.OnOfferPurchased(trigger);
            }
        }

        private void InitTrigger(OfferTriggerLink trigger)
        {
            if (state.retiredTriggers.FindIndex(t => t == trigger) != -1)
            {
                return;
            }

            foreach (var cond in trigger.Value.conditions)
            {
                cond.Value.Init(shop);
            }

            if (trigger.Value.appearHandler.HasValue)
            {
                trigger.Value.appearHandler.Value.Init(shop);

                if (trigger.Value.appearHandler.Value.ShouldBeOffered.Value)
                {
                    TryTriggerOffer(trigger);
                }

                trigger.Value.appearHandler.Value.ShouldBeOffered.OnValueChanged += shouldBeOffered =>
                {
                    if (shouldBeOffered)
                    {
                        TryTriggerOffer(trigger);
                    }
                };
            }
            else
            {
                TrySchedule(trigger, state.firstSessionTimestamp + trigger.Value.minTimeSinceFirstSession.seconds);
            }
        }

        private bool TryTriggerOffer(OfferTriggerLink trigger)
        {
            foreach (OfferConditionHandlerLink condition in trigger.Value.conditions)
            {
                if (condition.Value.AreConditionsMet() == false)
                {
                    return false;
                }
            }

            foreach (OfferTriggerListener listener in trigger.Value.GetListeners())
            {
                listener.OnOfferTriggered(trigger.Value);
            }

            long expirationTimestamp = trigger.Value.limited == false ? -1 : App.Now + trigger.Value.duration.seconds;

            if (state.activeTriggers.Find(o => o.trigger == trigger) == null)
            {
                state.activeTriggers.Add(new OfferTriggerState() { trigger = trigger, timestamp = expirationTimestamp });
            }

            OnOfferTriggered((trigger, expirationTimestamp));

            Debug.Log($"{trigger} triggered");

            if (trigger.Value.showOfferWindowOnTrigger)
            {
                windowShowQueue.Enqueue(trigger);
            }

            return true;
        }

        public int GetActiveOffersAmount()
        {
            return state.activeTriggers.Count;
        }

        public IEnumerable<(ShopItemLink offer, long expireTimestamp)> GetActiveOffers()
        {
            foreach (var trigger in state.activeTriggers)
            {
                yield return (trigger.trigger.Value.offer, trigger.timestamp);
            }
        }

        public bool TryGetOfferState(OfferTriggerLink offerTriggerLink, out long expireTimestamp)
        {
            foreach (var item in state.activeTriggers)
            {
                if (item.trigger == offerTriggerLink)
                {
                    expireTimestamp = item.timestamp;
                    return true;
                }
            }
            expireTimestamp = -1;
            return false;
        }
    }
}
