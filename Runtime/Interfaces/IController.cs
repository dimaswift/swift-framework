using System;

namespace SwiftFramework.Core
{
    public interface IController
    {
        ViewLink View { get; }
        event Action OnActivated;
        event Action OnBoosted;
        void Activate();
        bool ReadyToWork { get; }
        void SyncState();
        void Boost();
        void Update(float delta);
        IPromise Init();
        FracturedBigNumber ProductionSpeed { get; }
        string Title { get; }
    }
}
