using UnityEngine;
using UnityEngine.EventSystems;

namespace Swift.Core
{
    public abstract class IsometricCameraHandler : MonoBehaviour
    {
        [SerializeField] private float cameraSize = 5;

        protected Camera cam;
        protected Vector3? defaultPos;

        protected bool dragging;

        protected bool pointerDown;

        protected Bounds cameraBounds;
        
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
                OnInit();
            });

            UpdateBounds();
        }

        private void UpdateBounds()
        {
            if (!cam)
            {
                return;
            }

            float orthographicSize;
            cameraBounds = new Bounds()
            {
                center = Vector2.zero,
                extents = new Vector3(cam.aspect * (orthographicSize = cam.orthographicSize), orthographicSize, 0)
            };
        }

        private void LateUpdate()
        {
            cam.transform.position = transform.position;
        }

        protected abstract void OnInit();

        protected bool IsPointerDown()
        {
            return Input.GetMouseButtonDown(0) &&
                  (!EventSystem.current || EventSystem.current.IsPointerOverGameObject() == false);
        }

        protected static bool IsPointerUp()
        {
            return Input.GetMouseButtonUp(0);
        }

        protected Vector3 GetWorldPointer()
        {
            var orthographicSize = cam.orthographicSize;
            var y = (Input.mousePosition.y / Screen.height) * (orthographicSize * 2);
            var x = (Input.mousePosition.x / Screen.width) * (orthographicSize * 2 * cam.aspect);
            return new Vector3(x, y);
        }
    }
}