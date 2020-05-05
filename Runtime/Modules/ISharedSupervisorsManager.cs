using SwiftFramework.Core.Supervisors;
using System;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface ISharedSupervisorsManager
    {
        event Action OnAssignenmentsChange;
        bool IsAssigned(SupervisorLink supervisor);
        IAbilityManager Abilities { get; }
        IPromise<bool> Buy(SupervisorLink supervisor);
        void Acquire(SupervisorLink supervisor);
        void Assign(SupervisorLink supervisorLink, Link zone);
        IEnumerable<SharedSupervisorState> GetSupervisors(SupervisorFamilyLink family);
        SharedSupervisorState GetSupervisorState(SupervisorLink link);
        bool IsPurchased(SupervisorLink supervisorLink);
        bool IsPerkActive(SupervisorLink supervisorLink, PerkLink perk, Link zone);
    }
}
