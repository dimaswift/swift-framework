using System;
using System.Collections.Generic;

namespace SwiftFramework.Core.Boosters
{
    [Serializable]
    public class BoostersState : IDeepCopy<BoostersState>
    {
        public List<Booster> boosters = new List<Booster>();
        public List<BoosterContainer> boostersInventory = new List<BoosterContainer>();

        public void Sort()
        {
            boosters.Sort((b1, b2) => -b1.expirationTime.CompareTo(b2.expirationTime));
        }

        public void Add(Booster booster)
        {
            boosters.Add(booster);
        }

        public BoostersState DeepCopy()
        {
            return new BoostersState()
            {
                boosters = new List<Booster>(boosters),
                boostersInventory = new List<BoosterContainer>(boostersInventory)
            };
        }
    }
}
