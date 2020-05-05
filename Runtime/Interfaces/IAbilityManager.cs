using System;

namespace SwiftFramework.Core
{
    public interface IAbilityManager
    {
        event Action OnStateChanged;
        bool IsAbilityActive(Link owner, AbilityLink abilityLink, out AbilityState state);
        bool TryGetAbility(Link owner, out AbilityState state);
        IPromise<bool> ActivateAbility(Link owner, AbilityLink link, int level);
        IPromise DisableAbility(Link owner, AbilityLink ability);
        void RemoveAbility(Link owner);
        bool ReadyToUseAbility(Link owner);
    }
}
