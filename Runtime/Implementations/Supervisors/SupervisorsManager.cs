using SwiftFramework.Core.Abilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.Supervisors
{
    public class SupervisorsManager : ISupervisorsManager
    {
        public SupervisorFamilyLink Family { get; }

        public Link Zone { get; }

        public IStatefulEvent<bool> IsAssigned => isAssigned;

        public IAbilityManager Abilities => abilityManager;

        public SupervisorLink Current
        {
            get
            {
                if (state.assignedSupervisor == null || state.assignedSupervisor.HasValue == false)
                {
                    return null;
                }
                return state.assignedSupervisor;
            }
        }

        public string Title => subordinate.SupervisorsTitle;

        public ISharedSupervisorsManager Shared => sharedSupervisors;

        public event Action OnInventoryChanged = () => { };
        public event Action OnAbilitiesChanged = () => { };
        public event Action OnAbilityActivated = () => { };

        private readonly SubordinateState state;
        private readonly ISubordinate subordinate;
        private readonly SupervisorsPool pool;
        private readonly AbilityManager abilityManager;
        private readonly SharedSupervisorsManager sharedSupervisors;

        private readonly StatefulEvent<bool> isAssigned = new StatefulEvent<bool>();

        public SupervisorsManager(Link zone, SharedSupervisorsManager sharedSupervisors, SupervisorsPool pool, SubordinateState state, ISubordinate subordinate, IClock clock)
        {
            Zone = zone;
            this.sharedSupervisors = sharedSupervisors;
            this.subordinate = subordinate;
            this.state = state;
            this.pool = pool;
            abilityManager = new AbilityManager(pool.abilityState, clock, true, () => Current);
            Family = subordinate.GetSupervisorTemplate().Value.family;

            Abilities.OnStateChanged += Abilities_OnStateChanged;
            Shared.Abilities.OnStateChanged += Abilities_OnStateChanged;

            if (state.assignedSupervisor != null && state.assignedSupervisor.HasValue)
            {
                if (Current != null)
                {
                    if (Current.Value.isShared && Shared.GetSupervisorState(Current).assginedZone != zone)
                    {
                        isAssigned.SetValue(false);
                        state.assignedSupervisor = null;
                    }
                    else
                    {
                        isAssigned.SetValue(true);
                    }
                }
            }
        }

        private void Abilities_OnStateChanged()
        {
            OnAbilitiesChanged();
        }

        public IPromise Init()
        {
            return Promise.Resolved();
        }

        public IPromise<bool> Assign(SupervisorLink link)
        {
            Promise<bool> promise = Promise<bool>.Create();

            IPromise<bool> unassignCurrentPromise = Unassign();

            unassignCurrentPromise.Done(r =>
            {
                link.Load(supervisor =>
                {
                    state.assignedSupervisor = link;
                    pool.supervisors.RemoveAll(l => l == link);
                    isAssigned.SetValue(true);
                    if (supervisor.isShared)
                    {
                        sharedSupervisors.Assign(link, Zone);
                    }
                    OnAbilitiesChanged();
                    promise.Resolve(true);
                });
            });

            return promise;
        }

        public IPromise<PriceLink> GetPrice()
        {
            Promise<PriceLink> promise = Promise<PriceLink>.Create();

            subordinate.GetSupervisorTemplate().Load().Done(template =>
            {
                PriceLink price = template.GetPrice(pool.supervisorsPurchased);
                promise.Resolve(price);
            });

            return promise;
        }

        public IPromise<SupervisorLink> BuyNew()
        {
            Promise<SupervisorLink> promise = Promise<SupervisorLink>.Create();

            subordinate.GetSupervisorTemplate().Load().Done(template =>
            {
                PriceLink price = template.GetPrice(pool.supervisorsPurchased);
                price.Pay().Done(paid =>
                {
                    if (paid)
                    {
                        GenerateSupervisor(price, template, out SupervisorLink link);
                        pool.supervisors.Add(link);
                        abilityManager.AddAbility(link, link.Value.ability);
                        pool.supervisorsPurchased++;
                        OnInventoryChanged();
                        if (IsAssigned.Value == false)
                        {
                            Assign(link).Done(s => promise.Resolve(link));
                        }
                        else
                        {
                            promise.Resolve(link);
                        }
                    }
                    else
                    {
                        promise.Reject(new InvalidOperationException($"Cannot pay for the supervisor. Payment failed: {price}"));
                    }
                });
            });

            return promise;
        }

        private Supervisor GenerateSupervisor(PriceLink price, SupervisorTemplate template, out SupervisorLink link)
        {
            Supervisor supervisor = ScriptableObject.CreateInstance<Supervisor>();
            supervisor.ability = template.abilitiesPool.Random();
            supervisor.price = price;
            template.RollLevel(out supervisor.level, out supervisor.skin);
            supervisor.nameKey = template.namesPool.Random() + " " + template.lastNamesPool.Random();
            supervisor.family = template.family;

            link = Link.Generate<SupervisorLink>();
            App.Core.Storage.Save(supervisor, link);

            return supervisor;
        }


        public IPromise Sell(SupervisorLink link, float penalty)
        {
            Promise promise = Promise.Create();

            void PerformSell()
            {
                link.Load(supervisor =>
                {
                    supervisor.price.Refund(penalty).Done(s =>
                    {
                        pool.supervisors.RemoveAll(i => i == link);
                        if (link.IsGenerated())
                        {
                            App.Core.Storage.Delete<Supervisor>(link);
                        }
                        Abilities.RemoveAbility(link);
                        OnInventoryChanged();
                        promise.Resolve();
                    });
                });
            }

            if (Current == link)
            {
                Unassign().Done(s => PerformSell());
            }
            else
            {
                PerformSell();
            }

            return promise;
        }

        public IEnumerable<SupervisorLink> GetUnassigned()
        {
            foreach (SharedSupervisorState supervisorState in sharedSupervisors.GetSupervisors(Family))
            {
                if (supervisorState.assginedZone != Zone)
                {
                    yield return supervisorState.supervisor;
                }
            }

            foreach (SupervisorLink supervisor in pool.supervisors)
            {
                yield return supervisor;
            }
        }

        public IPromise<bool> Unassign()
        {
            Promise<bool> promise = Promise<bool>.Create();

            if (Current == null)
            {
                promise.Resolve(false);
                return promise;
            }

            if (Current.Value.isShared)
            {
                isAssigned.SetValue(false);
                var current = Current;
                state.assignedSupervisor = null;
                OnAbilitiesChanged();
                return sharedSupervisors.Unassign(current);
            }

            Abilities.DisableAbility(Current, Current.Value.ability).Always(() =>
            {
                pool.supervisors.Add(state.assignedSupervisor);
                state.assignedSupervisor = null;
                isAssigned.SetValue(false);
                OnAbilitiesChanged();
                promise.Resolve(true);
            });

            return promise;
        }

        public IEnumerable<AbilityLink> GetAllAbilities()
        {
            return subordinate.GetSupervisorTemplate().Value.abilitiesPool;
        }

        public PriceLink GetTemplateSupervisorPrice()
        {
            return subordinate.GetSupervisorTemplate().Value.GetPrice(pool.supervisorsPurchased);
        }

        public bool IsAbilityActive(AbilityLink ability, out AbilityState abilityState)
        {
            if (Current == null)
            {
                abilityState = null;
                return false;
            }

            if (Current.Value.isShared)
            {
                return sharedSupervisors.Abilities.IsAbilityActive(Current, ability, out abilityState);
            }

            return Abilities.IsAbilityActive(Current, ability, out abilityState);
        }

        public bool ReadyToUseAbility()
        {
            if (Current == null)
            {
                return false;
            }

            if (Current.Value.isShared)
            {
                return sharedSupervisors.Abilities.ReadyToUseAbility(Current);
            }

            return Abilities.ReadyToUseAbility(Current);
        }

        public IPromise<bool> ActivateCurrentAbility()
        {
            if (Current == null)
            {
                return Promise<bool>.Resolved(false);
            }

            if (Current.Value.isShared)
            {
                return sharedSupervisors.Abilities.ActivateAbility(Current, Current.Value.ability, Current.Value.level);
            }

            OnAbilityActivated();

            return Abilities.ActivateAbility(Current, Current.Value.ability, Current.Value.level);
        }

        public bool TryGetAbility(Link owner, out AbilityState state)
        {
            SupervisorLink supervisor = owner as SupervisorLink;
            if (supervisor != null && supervisor.Value.isShared)
            {
                return Shared.Abilities.TryGetAbility(owner, out state);
            }

            return Abilities.TryGetAbility(owner, out state);
        }

        public bool IsPerkActive(PerkLink perk, Link zone)
        {
            if (Current == null)
            {
                return false;
            }
            return Shared.IsPerkActive(Current, perk, zone);
        }
    }
}
