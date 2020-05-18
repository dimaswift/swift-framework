using UnityEngine;

namespace SwiftFramework.Core
{
    public abstract class AppListener : MonoBehaviour
    {
        private void Start()
        {
            App.WaitForState(AppState.ModulesInitialized, OnAppInitialized);
        }

        protected abstract void OnAppInitialized();
    }
}