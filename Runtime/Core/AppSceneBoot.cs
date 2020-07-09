using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwiftFramework.Core
{
    public abstract class AppSceneBoot<A> : AppBoot
    {
        [SerializeField] private SceneLink mainScene = null;

        [SerializeField] [CheckInterface(InterfaceSearch.Scene, typeof(ILoadingScreen))]
        private GameObject loadingScreenWrapper = null;

        protected virtual int TargetFrameRate { get; } = 60;
        protected virtual int SleepTimeout { get; } = UnityEngine.SleepTimeout.NeverSleep;
        private ILoadingScreen loadingScreen;

        private bool isUnloading;

        protected override bool IsReadyToRestart()
        {
            return !isUnloading;
        }

        protected override void OnAppWillBoot()
        {
            Application.targetFrameRate = TargetFrameRate;

            Screen.sleepTimeout = SleepTimeout;

            if (loadingScreenWrapper != null)
            {
                loadingScreen = loadingScreenWrapper.GetComponent<ILoadingScreen>();
            }

            loadingScreen?.SetVersion(Application.version);

            ShowLoadingScreen();

            base.OnAppWillBoot();
        }


        public virtual IPromise ShowLoadingScreen()
        {
            if (loadingScreen == null)
            {
                return Promise.Resolved();
            }

            loadingScreen.SetLoadProgress(0);
            return loadingScreen.Show();
        }

        protected override void OnAppInitialized()
        {
            base.OnAppInitialized();
            LoadMainScene().Always(success =>
            {
                HideLoadingScreen();
                OnMainSceneLoaded();
            });
        }

        protected virtual void OnMainSceneLoaded()
        {
        }

        protected override void OnLoadingProgressChanged(float progress)
        {
            loadingScreen?.SetLoadProgress(progress);
        }

        public virtual IPromise HideLoadingScreen()
        {
            if (loadingScreen == null)
            {
                return Promise.Resolved();
            }

            return loadingScreen.Hide();
        }

        protected override void OnAppWillBeRestarted()
        {
            base.OnAppWillBeRestarted();
            isUnloading = true;
            mainScene.Unload().Done(() => isUnloading = false);
            ShowLoadingScreen();
        }

        protected virtual IPromise<bool> LoadMainScene()
        {
            return mainScene.HasValue == false ? Promise<bool>.Resolved(true) : mainScene.Load(LoadSceneMode.Additive);
        }
    }
}