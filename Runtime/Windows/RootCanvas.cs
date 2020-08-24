using UnityEngine;

namespace SwiftFramework.Core.Windows
{
    [RequireComponent(typeof(Canvas))]
    public class RootCanvas : MonoBehaviour, IRootCanvas
    {
        public CanvasType Type
        {
            get => type;
            set => type = value;
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

        public float ScreenToWorldSpaceRatio { get; private set; }

        [SerializeField] private RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
        [SerializeField] private float planeDistance = 10;
        [SerializeField] private CanvasType type = CanvasType.Window;
        [SerializeField] private RectTransform safeAreaRect = null;

        private Canvas canvas;
        private RectTransform rectTransform;

        private void OnEnable()
        {
            Canvas.renderMode = renderMode;
            Canvas.worldCamera = Camera.main;
            Canvas.planeDistance = planeDistance;
            Canvas.sortingLayerName = "UI";
            if (Canvas.worldCamera != null)
            {
                if (Canvas.worldCamera.orthographic)
                {
                    Vector3 p1 = Canvas.worldCamera.ScreenToWorldPoint(Vector3.zero);
                    Vector3 p2 = Canvas.worldCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0));
                    ScreenToWorldSpaceRatio = Screen.width / (Mathf.Abs(p1.x - p2.x));
                }
                else
                {
                    Ray ray1 = Canvas.worldCamera.ViewportPointToRay(Vector3.zero);
                    Ray ray2 = Canvas.worldCamera.ViewportPointToRay(new Vector3(1, 0));
                    ScreenToWorldSpaceRatio = (Screen.width / (Mathf.Abs(ray1.GetPoint(planeDistance).x - ray2.GetPoint(planeDistance).x)));
                }
            }

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
            Rect pixelRect = Canvas.pixelRect;
            
            anchorMin.x /= pixelRect.width;
            anchorMin.y /= pixelRect.height;
            anchorMax.x /= pixelRect.width;
            anchorMax.y /= pixelRect.height;

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
        }
    }
}
