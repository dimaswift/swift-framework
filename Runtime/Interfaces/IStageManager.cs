using System;

namespace Swift.Core
{
    public interface IStageManager
    {
        IPromise<T> Load<T>(ViewLink stage) where T : class, IStage;
        IPromise LoadPrevious();
        IStage ActiveStage { get; }
        ViewLink ActiveStageLink { get; }
        IStatefulEvent<bool> IsLoading { get; }
        event Action<ViewLink> OnStageLoaded;
        void UnloadStage(ViewLink stage);
    }

    public interface IStage : IView
    {
        IPromise CloseStage(IStageManager stageManager);
        IPromise OpenStage(IStageManager stageManager);
        IPromise UnloadStage(IStageManager stageManager);
    }

}