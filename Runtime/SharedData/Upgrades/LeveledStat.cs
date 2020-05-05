using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SwiftFramework.Core.SharedData.Upgrades
{
    [Serializable]
    public class BaseStat
    {

    }

    [Serializable]
    public class FloatStat : BaseStat
    {
        [SerializeField] List<Stat> stats = new List<Stat>();

        [Serializable]
        public struct Stat
        {
            public float value;
            public int level;
        }

        public float GetValue(int level)
        {
            foreach (var stat in stats)
            {
                if (stat.level == level)
                {
                    return stat.value;
                }
            }
            return stats.LastFast().value;
        }
    }

    [Serializable]
    public class FloatStatRanged : BaseStat
    {
        [SerializeField] private List<Stat> stats = new List<Stat>();

        [Serializable]
        public struct Stat
        {
            public float value;
            public int fromLevel;
            public int tilLevel;
        }

        public float GetValue(int level)
        {
            foreach (var stat in stats)
            {
                if (level >= stat.fromLevel && level < stat.tilLevel)
                {
                    return stat.value;
                }
            }
            return stats.LastFast().value;
        }
    }

    [Serializable]
    public class IntStat : BaseStat
    {
        [SerializeField] List<Stat> stats = new List<Stat>();

        [Serializable]
        public struct Stat
        {
            public int level;
            public int value;
        }

        public int GetValue(int level)
        {
            foreach (var stat in stats)
            {
                if (stat.level == level)
                {
                    return stat.value;
                }
            }
            return stats.LastFast().value;
        }
    }

    [Serializable]
    public class IntStatRanged : BaseStat
    {
        [SerializeField] private List<Stat> stats = new List<Stat>();

        [Serializable]
        public struct Stat
        {
            public int value;
            public int fromLevel;
            public int tilLevel;
        }

        public int GetValue(int level)
        {
            foreach (var stat in stats)
            {
                if (level >= stat.fromLevel && level < stat.tilLevel)
                {
                    return stat.value;
                }
            }
            return stats.LastFast().value;
        }
    }

    [Serializable]
    public class BigNumberStat : BaseStat
    {
        [SerializeField] List<Stat> stats = new List<Stat>();

        [Serializable]
        public struct Stat
        {
            public BigNumber value;
            public int level;
        }

        public BigNumber GetValue(int level)
        {
            foreach (var stat in stats)
            {
                if (stat.level == level)
                {
                    return stat.value;
                }
            }
            return stats.LastFast().value;
        }
    }

    [Serializable]
    public class BigNumberStatRange : BaseStat
    {
        [SerializeField] private List<Stat> stats = new List<Stat>();

        [Serializable]
        public struct Stat
        {
            public BigNumber value;
            public int fromLevel;
            public int tilLevel;
        }

        public BigNumber GetValue(int level)
        {
            foreach (var stat in stats)
            {
                if (level >= stat.fromLevel && level < stat.tilLevel)
                {
                    return stat.value;
                }
            }
            return stats.LastFast().value;
        }
    }
}
