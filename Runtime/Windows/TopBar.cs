using SwiftFramework.Core;
using UnityEngine;

namespace SwiftFramework.Core.Windows
{
    [RequireComponent(typeof(Canvas), typeof(RectTransform))]
    public class TopBar : MonoBehaviour, ITopBar
    {
        [SerializeField] private GenericButton backButton = new GenericButton();
        
        private Canvas canvas;

        public bool IsShown
        {
            get => shown;
            set
            {
                shown = value;
                if (shown)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }
        }

        private bool shown;

        public RectTransform RectTransform { get; private set; }

        private Canvas GetCanvas()
        {
            if(canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }
            return canvas;
        }

        public virtual void Hide()
        {
            GetCanvas().enabled = false;
        }

        public virtual void Show()
        {
            GetCanvas().enabled = true;
        }

        public void Init()
        {
            canvas = GetComponent<Canvas>();
            RectTransform = GetComponent<RectTransform>();
            App.WaitForState(AppState.ModulesInitialized, OnInit);
            if (backButton.HasValue)
            {
                backButton.AddListener(() =>
                {
                    App.Core.Windows.CloseActiveWindow();
                });
            }
           
        }

        private void OnWindowWillBeShown(IWindow window)
        {
            if (backButton.HasValue && window.IsFullScreen)
            {
                backButton.SetActive(window.ShowToolbarBackButton);
            }
        }

        public virtual void OnInit()
        {
            App.Core.Windows.OnWindowWillBeShown += OnWindowWillBeShown;
        }
    }
}
