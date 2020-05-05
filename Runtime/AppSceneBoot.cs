using UnityEngine;

namespace SwiftFramework.Core
{
    public abstract class AppSceneBoot<A> : AppBoot<A>, IBoot where A : App<A>, new()
    {
        [SerializeField] private SceneLink mainScene = null;

        [SerializeField]
        [CheckInterface(InterfaceSearch.Scene, typeof(ILoadingScreen))]
        private GameObject loadingScreenWrapper = null;

        protected virtual int targetFrameRate { get; } = 60;
        protected virtual int sleepTimeout { get; } = SleepTimeout.NeverSleep;
        private ILoadingScreen loadingScreen;

        private bool isUnloading;

        protected override bool IsReadyToRestart()
        {
            if (isUnloading)
            {
                return false;
            }
            return true;
        }

        protected override void OnAppWillBoot()
        {
            Application.targetFrameRate = targetFrameRate;

            Screen.sleepTimeout = sleepTimeout;

            if (loadingScreenWrapper != null)
            {
                loadingScreen = loadingScreenWrapper.GetComponent<ILoadingScreen>();
            }

            if (loadingScreen != null)
            {
                loadingScreen.SetVersion(Application.version);
            }

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

        protected virtual void OnMainSceneLoaded() { }

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
            if (mainScene.HasValue == false)
            {
                return Promise<bool>.Resolved(true);
            }
            
            return mainScene.Load(UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

    }
}
