using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftFramework.Core.Windows
{
    [AddrLabel(AddrLabels.Window)]
    [AddrGroup(AddrGroups.Windows)]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public abstract class Window : MonoBehaviour, IWindow
    {
        public CanvasType CanvasType => canvasType;
        public bool ShowToolbarBackButton => showTopBarBackButton;

        public IPromise HidePromise
        {
            get
            {
                if (hidePromise == null)
                {
                    hidePromise = Promise.Create();
                }
                return hidePromise;
            }
        }

        public bool IsFullScreen => isFullScreen;

        public bool ShowTopBar => showTopBar;

        public bool CanBeClosed => closeButton != null || showTopBarBackButton;

        public bool IsShown { get; private set; }

        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }
                return rectTransform;
            }
        }

        public IAppearAnimationHandler Animation => animationHandler.HasValue ? animationHandler.Value : null;

        private RectTransform rectTransform = null;
        protected WindowsManager windowsManager = null;
        private CanvasGroup canvasGroup;
        private Promise hidePromise;
        private Vector3 defaultLocalPosition;

        [SerializeField] private AppearAnimationHandler animationHandler = null;
        [SerializeField] private CanvasType canvasType = CanvasType.Window;
        [SerializeField] private bool isFullScreen = false;
        [SerializeField] private bool showTopBar = false;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private bool showTopBarBackButton = false;
        

        public void Hide()
        {
            windowsManager.Hide(this);
        }

        public void Show()
        {
            windowsManager.Show(this);
        }

        public virtual void Init(WindowsManager windowsManager)
        {
            canvasGroup = gameObject.GetComponent<CanvasGroup>() ? gameObject.GetComponent<CanvasGroup>() : gameObject.AddComponent<CanvasGroup>();
            defaultLocalPosition = RectTransform.localPosition;
            this.windowsManager = windowsManager;
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseButtonClick);
            }
        }

        public virtual void HandleCloseButtonClick()
        {
            windowsManager.OnBackClick(this);
        }

        public virtual void WarmUp() { }

        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (closeButton == null && transform.Find("CloseButton"))
            {
                closeButton = transform.Find("CloseButton").GetComponent<Button>();
            }

            if (animationHandler == null || animationHandler.HasValue == false)
            {
                animationHandler = new AppearAnimationHandler();
                
                FadeWindowAnimation fadeWindowAnimation = GetComponent<FadeWindowAnimation>();

                if (fadeWindowAnimation == null)
                {
                    gameObject.AddComponent<FadeWindowAnimation>();
                }
                
                animationHandler.SetTarget(gameObject);

                UnityEditor.EditorUtility.SetDirty(this);
            }

            List<Transform> children = new List<Transform>(GetComponentsInChildren<Transform>(true));
            
            foreach (FieldInfo field in GetType().GetFields(BindingFlags.NonPublic | 
                                                            BindingFlags.Instance))
            {
                if (typeof(InterfaceComponentField).IsAssignableFrom(field.FieldType))
                {
                    InterfaceComponentField value = (InterfaceComponentField)field.GetValue(this);

                    if (value == null)
                    {
                        continue;
                    } 
                     
                    Transform button = children.Find(c =>
                    {
                        if (c.name == field.Name || c.name == field.Name.FirstCharToUpper())
                        {
                            if (c.GetComponent(value.InterfaceType) != null)
                            {
                                return true;
                            }
                        }
                        return false;
                        
                    });

                    if (button != null)
                    {
                        value.SetTarget(button.gameObject);
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                }
            }
#endif
        }

        protected void SetCloseButtonActive(bool active)
        {
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(active);
            }
        }

        public virtual void OnStartShowing()
        {

        }

        public virtual void OnShown()
        {

        }

        public virtual void OnStartHiding()
        {

        }

        public virtual void OnHidden()
        {

        }

        public void SetShown()
        {
            RectTransform.SetAsLastSibling();
            gameObject.SetActive(true);
            IsShown = true;
            RectTransform.localPosition = defaultLocalPosition;
        }

        public void SetHidden()
        {
            gameObject.SetActive(false);
            IsShown = false;
            if (hidePromise != null)
            {
                hidePromise.Resolve();
                hidePromise = null;
            }
        }

        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;   
        }
    }
}
