using Swift.Core;
using UnityEngine;

namespace Swift.Utils.UI
{
    public class LoadingScreen : MonoBehaviour, ILoadingScreen
    {
        [SerializeField] private ProgressBar loadingBar = null;
        [SerializeField] private GenericText versionText = null;

        private float currentLoadingProgress;

        public IPromise Hide()
        {
            gameObject.SetActive(false);
            return Promise.Resolved();
        }

        public void SetLoadProgress(float progress)
        {
            currentLoadingProgress = progress;
        }

        private void Update()
        {
            if(loadingBar == null)
            {
                return;
            }

            currentLoadingProgress = Mathf.MoveTowards(currentLoadingProgress, 1, Time.deltaTime / 2);
            
            loadingBar.Value.SetUp(currentLoadingProgress);
        }

        public void SetVersion(string version)
        {
            if(versionText == null)
            {
                return;
            }
            versionText.Text = version;
        }

        public IPromise Show()
        {
            if(loadingBar != null)
            {
                loadingBar.Value.SetUp(0);
            }
            gameObject.SetActive(true);
            return Promise.Resolved();
        }
    }

}
