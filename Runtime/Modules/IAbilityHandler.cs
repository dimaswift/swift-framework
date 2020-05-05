using System;

namespace SwiftFramework.Core
{
    public interface IAbilityHandler
    {
        event Action OnAbilityStarted;
        event Action OnAbilityEnded;
        void HandleStart(long power, long endTime);
        void HandleEnd();
    }

    [Serializable]
    [FlatHierarchy]
    public class AbilityHandlerLink : LinkToScriptable<IAbilityHandler>
    {

    }

}
