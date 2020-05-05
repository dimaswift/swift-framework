using System;

namespace SwiftFramework.Core.SharedData
{
    [Serializable]
    public class QuestConfig : ProgressItemConfig
    {
        public int targetAmount;
        public bool autoStart;
        public string[] questsToUnlock;
        public bool repeatable;
        public int cooldownHours;
        public int reward;
        public QuestData CreateQuest()
        {
            return new QuestData()
            {
                id = id,
                progress = 0
            };
        }
    }


}
