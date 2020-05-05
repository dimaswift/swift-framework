using SwiftFramework.Core.Abilities;
using System;
using System.Collections.Generic;

namespace SwiftFramework.Core.Supervisors
{
    public class SharedSupervisorsManager : ISharedSupervisorsManager
    {
        public IAbilityManager Abilities => abilities;

        private readonly SharedSupervisorsManagerState state;
        private readonly AbilityManager abilities;

        public event Action OnAssignenmentsChange = () => { };

        public SharedSupervisorsManager(SharedSupervisorsManagerState state, IClock clock)
        {
            this.state = state;
            abilities = new AbilityManager(state.abilities, clock, false, () => null);
        }

        private SharedSupervisorState GetState(SupervisorLink supervisorLink)
        {
            return state.supervisors.Find(s => s.supervisor == supervisorLink);
        }

        public bool IsPurchased(SupervisorLink supervisor)
        {
            return GetState(supervisor) != null;
        }

        public bool IsAssigned(SupervisorLink supervisorLink, out Link zone)
        {
            foreach (SharedSupervisorState supervisorState in state.supervisors)
            {
                if (supervisorState.supervisor == supervisorLink && supervisorState.assginedZone != null)
                {
                    zone = supervisorState.assginedZone;
                    return true;
                }
            }
            zone = null;
            return false;
        }

        public void Assign(SupervisorLink supervisorLink, Link zone)
        {
            SharedSupervisorState supervisorState = GetState(supervisorLink);
            supervisorState.assginedZone = zone;
            OnAssignenmentsChange();
        }

        public IPromise<bool> Unassign(SupervisorLink supervisorLink)
        {
            Promise<bool> promise = Promise<bool>.Create();
            SharedSupervisorState supervisorState = GetState(supervisorLink);
            abilities.DisableAbility(supervisorLink, supervisorLink.Value.ability).Done(() =>
            {
                supervisorState.assginedZone = null;
                OnAssignenmentsChange();
                promise.Resolve(true);
            });

            return promise;

        }

        public IEnumerable<SharedSupervisorState> GetSupervisors(SupervisorFamilyLink family)
        {
            foreach (SharedSupervisorState supervisorState in state.supervisors)
            {
                if (supervisorState.supervisor.Value.family == family)
                {
                    yield return supervisorState;
                }
            }
        }

        public IEnumerable<SharedSupervisorState> GetAllSupervisors()
        {
            return state.supervisors;
        }

        public IPromise<bool> Buy(SupervisorLink supervisor)
        {
            Promise<bool> promise = Promise<bool>.Create();

            supervisor.Value.price.Pay().Done(paid =>
            {
                if (paid)
                {
                    Acquire(supervisor);
                    promise.Resolve(true);
                }
                else
                {
                    promise.Resolve(false);
                }
            });

            return promise;
        }

        public bool IsAssigned(SupervisorLink supervisor)
        {
            var state = GetState(supervisor);
            if (state == null)
            {
                return false;
            }

            return state.assginedZone != null;
        }

        public SharedSupervisorState GetSupervisorState(SupervisorLink link)
        {
            return GetState(link);
        }

        public bool IsPerkActive(SupervisorLink supervisor, PerkLink perk, Link zone)
        {
            foreach (SharedSupervisorState state in state.supervisors)
            {
                if (state.supervisor == supervisor && state.supervisor.Value.perk == perk && state.assginedZone == zone)
                {
                    return true;
                }
            }
            return false;
        }

        public void Acquire(SupervisorLink supervisor)
        {
            SharedSupervisorState supervisorsState = new SharedSupervisorState()
            {
                supervisor = supervisor,
                assginedZone = null
            };
            abilities.AddAbility(supervisor, supervisor.Value.ability);
            state.supervisors.Add(supervisorsState);
        }
    }

    [Serializable]
    public class SharedSupervisorsManagerState : IDeepCopy<SharedSupervisorsManagerState>
    {
        public List<SharedSupervisorState> supervisors = new List<SharedSupervisorState>();

        public AbilityManagerState abilities = new AbilityManagerState();

        public SharedSupervisorsManagerState DeepCopy()
        {
            return new SharedSupervisorsManagerState()
            {
                supervisors = supervisors.DeepCopy(),
                abilities = abilities.DeepCopy()
            };
        }
    }

    [Serializable]
    public class SharedSupervisorState : IDeepCopy<SharedSupervisorState>
    {
        public SupervisorLink supervisor;
        public AbilityState ability;
        public Link assginedZone;

        public SharedSupervisorState DeepCopy()
        {
            return new SharedSupervisorState()
            {
                supervisor = supervisor,
                ability = ability,
                assginedZone = assginedZone
            };
        }
    }

}
