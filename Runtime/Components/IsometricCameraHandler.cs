using UnityEngine;

namespace SwiftFramework.Core
{
    public abstract class IsometricCameraHandler : MonoBehaviour
    {
        [SerializeField] private float cameraSize = 5;

        protected Camera cam;
        protected Vector3? defaultPos;

        protected bool dragging;

        protected bool pointerDown;

        protected Bounds cameraBounds;

        protected IEventManager eventManager;

        private void OnEnable()
        {
            cam = Camera.main;

            if (defaultPos.HasValue == false)
            {
                defaultPos = transform.position;
            }

            App.WaitForState(AppState.ModulesInitialized, () =>
            {
                cam.orthographicSize = cameraSize;
                UpdateBounds();
                eventManager = App.Core.GetModule<IEventManager>();
                OnInit();
            });

            UpdateBounds();
        }

        public void UpdateBounds()
        {
            if (!cam)
            {
                return;
            }
            cameraBounds = new Bounds()
            {
                center = Vector2.zero,
                extents = new Vector3(cam.aspect * cam.orthographicSize, cam.orthographicSize, 0)
            };
        }

        private void LateUpdate()
        {
            cam.transform.position = transform.position;
        }

        protected abstract void OnInit();

        protected bool IsPointerDown()
        {
            return Input.GetMouseButtonDown(0)
                && (eventManager == null || eventManager.IsPointerHandledByUI == false);
        }

        protected bool IsPointerUp()
        {
            return Input.GetMouseButtonUp(0);
        }

        protected Vector3 GetWorldPointer()
        {
            var y = (Input.mousePosition.y / Screen.height) * (cam.orthographicSize * 2);
            var x = (Input.mousePosition.x / Screen.width) * (cam.orthographicSize * 2 * cam.aspect);
            return new Vector3(x, y);
        }

    }
}
