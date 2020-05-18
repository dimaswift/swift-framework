using UnityEngine;

namespace SwiftFramework.Core
{
    public class CameraAnchor : MonoBehaviour
    {
        [SerializeField] private TextAnchor type = TextAnchor.UpperCenter;
        [SerializeField] private Vector2 offset = Vector2.zero;

        private float aspect = 0;

        private Camera cam;

        private void LateUpdate()
        {
            if (!cam)
            {
                cam = Camera.main;
            }

            if (aspect != cam.aspect)
            {
                aspect = cam.aspect;
                Vector3 targetPosition = cam.transform.position;
                targetPosition.z = transform.position.z;
                Bounds cameraBounds = cam.GetIsometricBounds();
                switch (type)
                {
                    case TextAnchor.UpperLeft:
                        targetPosition.x = targetPosition.x - cameraBounds.extents.x + offset.x;
                        targetPosition.y = targetPosition.y + cameraBounds.extents.y + offset.y;
                        break;
                    case TextAnchor.UpperCenter:
                        targetPosition.y = targetPosition.y + cameraBounds.extents.y + offset.y;
                        targetPosition.x += offset.x;
                        break;
                    case TextAnchor.UpperRight:
                        targetPosition.x = targetPosition.x + cameraBounds.extents.x + offset.x;
                        targetPosition.y = targetPosition.y + cameraBounds.extents.y + offset.y;
                        break;
                    case TextAnchor.MiddleLeft:
                        targetPosition.y += offset.y;
                        targetPosition.x = targetPosition.x - cameraBounds.extents.x + offset.x;
                        break;
                    case TextAnchor.MiddleCenter:
                        targetPosition.x += offset.x;
                        targetPosition.y += offset.y;
                        break;
                    case TextAnchor.MiddleRight:
                        targetPosition.x = targetPosition.x + cameraBounds.extents.x + offset.x;
                        targetPosition.y += offset.y;
                        break;
                    case TextAnchor.LowerLeft:
                        targetPosition.x = targetPosition.x - cameraBounds.extents.x + offset.x;
                        targetPosition.y = targetPosition.y - cameraBounds.extents.y + offset.y;
                        break;
                    case TextAnchor.LowerCenter:
                        targetPosition.x += offset.x;
                        targetPosition.y = targetPosition.y - cameraBounds.extents.y + offset.y;
                        break;
                    case TextAnchor.LowerRight:
                        targetPosition.x = targetPosition.x + cameraBounds.extents.x + offset.x;
                        targetPosition.y = targetPosition.y - cameraBounds.extents.y + offset.y;
                        break;
                }

                transform.position = targetPosition;
            }
        }
    }
}