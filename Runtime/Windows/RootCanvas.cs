using UnityEngine;

namespace SwiftFramework.Core.Windows
{
    [RequireComponent(typeof(Canvas))]
    public class RootCanvas : MonoBehaviour, IRootCanvas
    {
        public CanvasType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        public RectTransform RectTransform
        {
            get
            {
                if (safeAreaRect != null)
                {
                    return safeAreaRect;
                }
                if (rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }
                return rectTransform;
            }
        }

        public Canvas Canvas
        {
            get
            {
                if (canvas == null)
                {
                    canvas = GetComponent<Canvas>();        
                }
                return canvas;
            }
        }

        [SerializeField] private CanvasType type = CanvasType.Window;
        [SerializeField] private RectTransform safeAreaRect = null;

        private Canvas canvas;
        private RectTransform rectTransform;

        private void OnEnable()
        {
            ApplySafeArea();
        }

        public void ApplySafeArea()
        {
            if (safeAreaRect == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;

            if (safeArea.x.IsValid() == false || safeArea.y.IsValid() == false || safeArea.width.IsValid() == false || safeArea.height.IsValid() == false)
            {
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Canvas.pixelRect.width;
            anchorMin.y /= Canvas.pixelRect.height;
            anchorMax.x /= Canvas.pixelRect.width;
            anchorMax.y /= Canvas.pixelRect.height;

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
        }
    }
}
