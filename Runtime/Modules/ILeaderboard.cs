using SwiftFramework.Core.Pooling;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    public interface ILeaderboard
    {
        bool EntriesInSync { get; }
        event Action OnPlayerEntryUpdated;
        string GetId();
        string GetTitle();
        IPromise<IEnumerable<LeaderboardPlayerEntry>> FetchEntries(bool isFriendsOnly);
        IEnumerable<LeaderboardPlayerEntry> GetPlayers();
        LeaderboardPlayerEntry GetPlayerEntry();
        IPromise<LeaderboardPlayerEntry> FetchPlayerEntry();
        void TryPostScore();
        IPromise PostScore();
    }

    public class LeaderboardPlayerEntry : BasePooled
    {
        public long Position { get; set; }
        public string DisplayName { get; set; }
        public BigNumber Score { get; set; }
        public IPromise<Texture2D> Avatar { get; set; }
        public SpriteLink ValueIcon { get; set; }

        public override void Dispose()
        {
            if(Avatar != null)
            {
                Avatar.Done(t => 
                {
                    if(t != null)
                    {
                        UnityEngine.Object.Destroy(t);
                    }
                });
                Avatar = null;
            }
        }

        protected override void OnReturnedToPool()
        {
            Dispose();
        }
    }
}
