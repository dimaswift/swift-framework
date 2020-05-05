using SwiftFramework.Core.SharedData.Upgrades;
using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [CreateAssetMenu(menuName = "SwiftFramework/Ability")]
    public class Ability : BaseStatInfo
    {
        public IntStat duration;
        public IntStat cooldown;
        public PriceLink activationPrice;
        public AbilityHandlerLink handler;
    }

    [Serializable]
    [LinkFolder(Folders.Configs + "/Abilities")]
    public class AbilityLink : LinkToScriptable<Ability>
    {

    }

    [Serializable]
    public class AbilityState : IDeepCopy<AbilityState>
    {
        public Link owner;
        public AbilityLink ability;
        public long activationTime;
        public long endTime;
        public long refreshTime;
        public long power;
        public bool active;

        public AbilityState DeepCopy()
        {
            return new AbilityState()
            {
                owner = owner,
                ability = ability,
                activationTime = activationTime,
                active = active,
                endTime = endTime,
                power = power,
                refreshTime = refreshTime
            };
        }
    }

}
