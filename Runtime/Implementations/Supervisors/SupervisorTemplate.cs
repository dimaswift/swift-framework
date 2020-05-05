using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.Supervisors
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Supervisors/Template")]
    public class SupervisorTemplate : ScriptableObject
    {
        [Header("Randomized parameters")]
        public List<string> namesPool;
        public List<string> lastNamesPool;
        public List<LevelRoll> levelsPool;
        public List<AbilityLink> abilitiesPool;
        public SupervisorFamilyLink family;

        [Header("Pricing")]
        public float priceMultiplier = 3;
        public PriceLink basePrice;

        [Serializable]
        public class LevelRoll
        {
            public int level;
            public float chance;
            public SupervisorSkinLink skin;
        }

        public void RollLevel(out int level, out SupervisorSkinLink skin)
        {
            float rand = UnityEngine.Random.value;
            foreach (LevelRoll roll in levelsPool)
            {
                if (rand <= roll.chance)
                {
                    level = roll.level;
                    skin = roll.skin;
                    return;
                }
            }
            var l = levelsPool.Random();
            level = l.level;
            skin = l.skin;
        }

        public PriceLink GetPrice(int amountOfSupervisors)
        {
            BigNumber amount = basePrice.amount;
            for (int i = 0; i < amountOfSupervisors; i++)
            {
                amount = amount.Value.MultiplyByFloat(priceMultiplier);
            }
            PriceLink newPrice = Link.Create<PriceLink>(basePrice.GetPath());
            newPrice.amount = amount;
            return newPrice;
        }
    }



    [Serializable]
    [LinkFolder(Folders.Configs + "/Supervisors/Templates")]
    public class SupervisorTemplateLink : LinkTo<SupervisorTemplate>
    {

    }
}
