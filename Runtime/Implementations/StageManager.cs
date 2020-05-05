using System;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public class StageManager : IStageManager
    {
        public IStage ActiveStage { get; private set; }
        public ViewLink ActiveStageLink { get; private set; }
        public IStatefulEvent<bool> IsLoading => isLoading;

        private readonly StatefulEvent<bool> isLoading = new StatefulEvent<bool>();

        private readonly Stack<ViewLink> history = new Stack<ViewLink>();

        public event Action<ViewLink> OnStageLoaded = s => { };

        private readonly Dictionary<ViewLink, IStage> loadedStages = new Dictionary<ViewLink, IStage>();

        public IPromise<T> Load<T>(ViewLink stageLink) where T : class, IStage
        {
            if (IsLoading.Value)
            {
                return Promise<T>.Rejected(new InvalidOperationException("Already loading stage"));
            }

            isLoading.SetValue(true);
            Promise<T> promise = Promise<T>.Create();
            Promise loadPromise = Promise.Create();

            App.Core.MakeTransition(loadPromise, () =>
            {
                if (ActiveStageLink != null && ActiveStageLink.GetPath() != stageLink.GetPath())
                {
                    history.Push(ActiveStageLink);
                    ActiveStage.CloseStage(this);
                }

                void SetActive(ViewLink link, T stage)
                {
                    ActiveStage = stage;
                    ActiveStageLink = link;
                    stage.OpenStage(this).Done(() =>
                    {
                        isLoading.SetValue(false);
                        loadPromise.Resolve();
                        promise.Resolve(stage);
                       
                        if (loadedStages.ContainsKey(link) == false)
                        {
                            loadedStages.Add(link, stage);
                        }
                        OnStageLoaded(link);
                    });
                }

                if (loadedStages.ContainsKey(stageLink))
                {
                    T loaded = loadedStages[stageLink] as T;
                    SetActive(stageLink, loaded);
                }
                else
                {
                    App.Core.Views.CreateAsync<T>(stageLink).Done(stage =>
                    {
                        SetActive(stageLink, stage);
                    });
                }
            });

            return promise;
        }

        public IPromise LoadPrevious()
        {
            if (history.Count == 0)
            {
                return Promise.Resolved();
            }

            return Load<IStage>(history.Pop()).Then();
        }

        public void UnloadStage(ViewLink stage)
        {
            if (loadedStages.TryGetValue(stage, out var loadedStage))
            {
                loadedStage.UnloadStage(this);
                loadedStages.Remove(stage);
            }
        }
    }
}