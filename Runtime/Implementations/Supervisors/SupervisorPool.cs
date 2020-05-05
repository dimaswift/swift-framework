using SwiftFramework.Core.Abilities;
using System;
using System.Collections.Generic;

namespace SwiftFramework.Core.Supervisors
{
    [Serializable]
    public class SupervisorsPool : IDeepCopy<SupervisorsPool>
    {
        public List<SupervisorLink> supervisors = new List<SupervisorLink>();
        public AbilityManagerState abilityState = new AbilityManagerState();
        public int supervisorsPurchased;

        public SupervisorsPool DeepCopy()
        {
            return new SupervisorsPool()
            {
                supervisors = new List<SupervisorLink>(supervisors),
                abilityState = abilityState.DeepCopy(),
                supervisorsPurchased = supervisorsPurchased
            };
        }
    }

}
