using SwiftFramework.Core.SharedData.SeasonPass;
using SwiftFramework.Core.SharedData.Shop;
using System;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface ISeasonPass : IModule, ITimeLimit
    {
        bool TryGetCurrentSeason(out SeasonLink seasonLink);
        IStatefulEvent<bool> PremiumPassPurchased { get; }
        IStatefulEvent<int> CurrentMilestone { get; }
        IStatefulEvent<(int current, int total)> Points { get; }
        float CurrentPointsProgress { get; }
        IPromise<IEnumerable<RewardLink>> ClaimMilestoneRewards(int index, bool premium);
        void AddPoints(int amount);
        bool IsMilestoneRewardClaimed(int index, bool premium);
        bool IsMilestoneRewardAvailable(int index, bool premium);
        int MilestonesCount { get; }
        ShopItemLink GetPremiumPassOffer();
        IEnumerable<(Milestone milestone, float progress)> GetMilestones();
        (Milestone milestone, float progress) GetMilestone(int index);
    }

    public interface ISeasonPassView : IView, IStage
    {

    }
}
