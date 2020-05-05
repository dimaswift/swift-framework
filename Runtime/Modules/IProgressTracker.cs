using System;
using System.Collections.Generic;
using SwiftFramework.Core.SharedData;
namespace SwiftFramework.Core
{
    public interface IProgressTracker : IModule
    {
        event Action<AchievementData> OnAchievementUpdated;
        event Action<QuestData> OnQuestUpdated;
        IPromise<IEnumerable<(AchievementConfig config, AchievementData data)>> GetAchievements();
        IPromise<IEnumerable<(QuestConfig config, QuestData data)>> GetQuests();
        IPromise IncrementAchievement(string id, int amount);
        IPromise IncrementQuest(string id, int amount);
        IPromise ClaimAchievementReward(string id);
        IPromise ClaimQuestReward(string id);
        ProgressItemStatus GetQuestStatus(string id);
        ProgressItemStatus GetAchievementStatus(string id);
        void RefreshCredits();
    }

    public interface IProgressRestoreHandler
    {
        void RestoreAchievements(string id, int current, out int pendingIncrements);
        void RestoreQuests(string id, int current, out int pendingIncrements);
    }
}
