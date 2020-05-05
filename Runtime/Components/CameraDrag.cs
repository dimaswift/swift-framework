using UnityEngine;

namespace SwiftFramework.Core
{
    public class CameraDrag : IsometricCameraHandler
    {
        [SerializeField] private float damping = 1;
        [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        [SerializeField] private float dragStartThresholdDistance = 0.1f;
        [SerializeField] private float dragSpeed = 20f;

        private Vector3 pressedScreenPoint;
        private Vector3 pressedPoint;

        private Vector3 dragVelocity;

        protected override void OnInit()
        {
            

        }

        private void Update()
        {
            if (App.Initialized == false)
            {
                return;
            }

            if (dragging == false)
            {
                if (IsPointerDown())
                {
                    dragVelocity = Vector3.zero;
                    pointerDown = true;
                    pressedScreenPoint = GetWorldPointer();
                    pressedPoint = transform.position;
                    dragging = true;
                }
            }

            if (pointerDown && IsPointerUp())
            {
                pointerDown = false;
            }

            if (dragging)
            {
                Vector3 screenPoint = GetWorldPointer();
                var pos = transform.position;
                var offset = pressedScreenPoint - screenPoint;

                if (offset.sqrMagnitude >= dragStartThresholdDistance)
                {
                    var targetPos = GetClampedPosition(pressedPoint + (pressedScreenPoint - screenPoint));
                    transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * dragSpeed);
                }

                dragVelocity = transform.position - pos;

                if (IsPointerUp())
                {
                    dragging = false;
                }
            }
            else
            {
                if (Mathf.Approximately(dragVelocity.sqrMagnitude, 0) == false)
                {
                    dragVelocity = Vector3.Lerp(dragVelocity, Vector3.zero, damping * Time.deltaTime);
                }
                Move(dragVelocity);
            }
          

        }

        private void Move(Vector3 delta)
        {
            if (delta.sqrMagnitude == 0)
            {
                return;
            }
            Vector3 targetPos = transform.position;
            targetPos += delta;
            targetPos = GetClampedPosition(targetPos);
            transform.position = targetPos;
        }

        private Vector3 GetClampedPosition(Vector3 pos)
        {
            pos.z = transform.position.z;
            pos.x = Mathf.Clamp(pos.x, -bounds.extents.x + cameraBounds.extents.x, bounds.extents.x - cameraBounds.extents.x);
            pos.y = Mathf.Clamp(pos.y, -bounds.extents.y + cameraBounds.extents.y, bounds.extents.y - cameraBounds.extents.y);
            return pos;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, bounds.size);
        }
#endif
    }
}
