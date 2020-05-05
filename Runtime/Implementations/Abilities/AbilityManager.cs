using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Abilities
{
    public class AbilityManager : IAbilityManager
    {
        public event Action OnStateChanged = () => { };

        private readonly AbilityManagerState state;
        private readonly IClock clock;
        private readonly Dictionary<AbilityLink, IAbilityHandler> abilityHandlers = new Dictionary<AbilityLink, IAbilityHandler>();
        private readonly Func<Link> ownerHandler;
        private readonly bool checkAbilityOwner;

        public AbilityManager(AbilityManagerState state, IClock clock, bool checkAbilityOwner, Func<Link> ownerHandler)
        {
            this.state = state;
            this.checkAbilityOwner = checkAbilityOwner;
            this.clock = clock;
            this.ownerHandler = ownerHandler;
            clock.Now.OnValueChanged += Now_OnValueChanged;
            foreach (AbilityState ability in state.abilities)
            {
                if (clock.Now.Value >= ability.endTime)
                {
                    ability.active = false;
                }
            }
        }

        public void RemoveAbility(Link owner)
        {
            state.abilities.RemoveAll(a => a.owner == owner);
        }

        public IPromise<bool> ActivateAbility(Link owner, AbilityLink link, int level)
        {
            Promise<bool> promise = Promise<bool>.Create();

            link.Load(ability =>
            {
                AbilityState abilityState = GetAbility(owner, link);

                long cooldown = ability.cooldown.GetValue(level);

                if (abilityState.active || abilityState.endTime + cooldown > clock.Now.Value)
                {
                    promise.Resolve(false);
                    return;
                }

                long duration = ability.duration.GetValue(level);

                long power = ability.power.GetValue(level);

                abilityState.activationTime = clock.Now.Value;
                abilityState.endTime = abilityState.activationTime + duration;
                abilityState.refreshTime = abilityState.activationTime + duration + cooldown;
                abilityState.power = power;
                abilityState.active = true;

                if (ability.handler.HasValue)
                {
                    ability.handler.Load(abilityHandler =>
                    {
                        if (abilityHandlers.ContainsKey(link) == false)
                        {
                            abilityHandlers.Add(link, abilityHandler);
                        }

                        abilityHandler.HandleStart(power, abilityState.endTime);
                        promise.Resolve(true);
                        OnStateChanged();
                    });
                }
                else
                {
                    promise.Resolve(true);
                    OnStateChanged();
                }
            });


            return promise;
        }

     
        public bool IsAbilityActive(Link owner, AbilityLink abilityLink, out AbilityState state)
        {
            foreach (AbilityState s in this.state.abilities)
            {
                if (s.owner == owner && s.ability == abilityLink && s.active && s.endTime >= clock.Now.Value)
                {
                    state = s;
                    return true;
                }
            }
            state = null;
            return false;
        }

        public AbilityState AddAbility(Link owner, AbilityLink abilityLink)
        {
            AbilityState state = new AbilityState()
            {
                ability = abilityLink,
                owner = owner
            };
            this.state.abilities.Add(state);
            return state;
        }

        public AbilityState GetAbility(Link owner, AbilityLink abilityLink)
        {
            foreach (AbilityState s in this.state.abilities)
            {
                if (s.owner == owner && s.ability == abilityLink)
                {
                    return s;
                }
            }

            return AddAbility(owner, abilityLink);
        }

        public IPromise DisableAbility(Link owner, AbilityLink ability)
        {
            Promise promise = Promise.Create();
            if (IsAbilityActive(owner, ability, out AbilityState abilityState))
            {
                abilityState.active = false;
                abilityState.refreshTime = clock.Now.Value + (abilityState.refreshTime - abilityState.endTime);
                abilityState.endTime = clock.Now.Value;
              
                if (abilityHandlers.TryGetValue(ability, out IAbilityHandler handler))
                {
                    handler.HandleEnd();
                    abilityHandlers.Remove(ability);
                    promise.Resolve();
                    OnStateChanged();
                }
                else
                {
                    ability.Load(cfg =>
                    {
                        if (cfg.handler.HasValue)
                        {
                            cfg.handler.Load(abilityHandler =>
                            {
                                abilityHandler.HandleEnd();
                                promise.Resolve();
                                OnStateChanged();
                            },
                            e => promise.Resolve());
                        }
                        else
                        {
                            promise.Resolve();
                            OnStateChanged();
                        }
                    });
                }
            }
            else
            {
                promise.Resolve();
            }

            return promise;
        }

        public bool TryGetAbility(Link owner, out AbilityState state)
        {
            foreach (AbilityState s in this.state.abilities)
            {
                if (s.owner == owner)
                {
                    state = s;
                    return true;
                }
            }
            state = null;
            return false;
        }

        public bool ReadyToUseAbility(Link owner)
        {
            if (TryGetAbility(owner, out AbilityState state) == false)
            {
                if (state.active == false && clock.Now.Value > state.refreshTime)
                {
                    return true;
                }
            }
            return false;
        }

        private void Now_OnValueChanged(long now)
        {
            for (int i = state.abilities.Count - 1; i >= 0; i--)
            {
                var data = state.abilities[i];
                if (data.active && now >= data.endTime && (checkAbilityOwner == false || ownerHandler() == data.owner))
                {
                    if (abilityHandlers.TryGetValue(data.ability, out IAbilityHandler handler))
                    {
                        handler.HandleEnd();
                        abilityHandlers.Remove(data.ability);
                    }
                    data.active = false;
                    OnStateChanged();
                }
            }
        }
    }

    [Serializable]
    public class AbilityManagerState : IDeepCopy<AbilityManagerState>
    {
        public List<AbilityState> abilities = new List<AbilityState>();

        public AbilityManagerState DeepCopy()
        {
            return new AbilityManagerState()
            {
                abilities = abilities.DeepCopy()
            };
        }
    }

}
