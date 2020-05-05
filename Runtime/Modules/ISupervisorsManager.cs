using SwiftFramework.Core.Abilities;
using SwiftFramework.Core.Supervisors;
using System;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface ISupervisorsManager
    {
        event Action OnAbilitiesChanged;
        event Action OnAbilityActivated;
        string Title { get; }
        SupervisorLink Current { get; }
        IStatefulEvent<bool> IsAssigned { get; }
        event Action OnInventoryChanged;
        IEnumerable<SupervisorLink> GetUnassigned();
        IPromise<bool> Assign(SupervisorLink supervisor);
        IPromise<bool> Unassign();
        IPromise<PriceLink> GetPrice();
        IPromise Sell(SupervisorLink supervisor, float penalty);
        IPromise<SupervisorLink> BuyNew();
        PriceLink GetTemplateSupervisorPrice();
        IEnumerable<AbilityLink> GetAllAbilities();
        IPromise Init();
        bool IsAbilityActive(AbilityLink ability, out AbilityState abilityState);
        bool ReadyToUseAbility();
        IPromise<bool> ActivateCurrentAbility();
        ISharedSupervisorsManager Shared { get; }
        bool TryGetAbility(Link owner, out AbilityState state);
        Link Zone { get; }
        SupervisorFamilyLink Family { get; }
        bool IsPerkActive(PerkLink perk, Link zone);
    }

}
