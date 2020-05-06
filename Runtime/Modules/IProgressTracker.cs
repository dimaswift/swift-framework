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
        TimeSpan GetQuestCooldown(string id);
        ProgressItemStatus GetAchievementStatus(string id);
        void RefreshCredits();
        (int current, int target) GetQuestProgress(string id);
        (int current, int target, int step) GetAchievementProgress(string id);
        bool IsAnyRewardAvailable();
        IStatefulEvent<bool> IsAppLive { get; }
    }

    public interface IProgressRestoreHandler
    {
        void RestoreAchievements(string id, int current, out int pendingIncrements);
        void RestoreQuests(string id, int current, out int pendingIncrements);
    }
}
