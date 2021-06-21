using System;
using UnityEngine;

namespace Swift.Core
{
    public interface ICameraLimitHandler
    {
        float GetBottomPoint();
    }

    public class CameraScroll : IsometricCameraHandler
    {
        [SerializeField] private float damping = 1;
        [SerializeField] private float maxWidth = 5;
        [SerializeField] private float minWidth = 5;

        [SerializeField] private float referenceHeight = 5;
        [SerializeField] private float topPadding = 5;
        [SerializeField] private float bottomPadding = 5;
        [SerializeField] private float scrollStartThresholdDistance = .1f;
        [SerializeField] private float scrollSpeed = 10f;

        private ICameraLimitHandler limitHandler;

        private float prevAspect;

        private float prevScrollPos;

        private float tapPosition;

        private float dragVelocity;

        protected override void OnInit()
        {
            AdjustAspect();
            limitHandler = GetComponent<ICameraLimitHandler>();
            if (defaultPos != null)
            {
                Vector3 targetPos = defaultPos.Value;
                targetPos.y = GetClampedPositionY(targetPos.y);
                transform.position = targetPos;
            }
        }

        private void AdjustAspect()
        {
            if (cam.aspect > maxWidth / referenceHeight)
            {
                cam.orthographicSize = maxWidth / cam.aspect;
            }
            else if (cam.aspect < minWidth / referenceHeight)
            {
                cam.orthographicSize = minWidth / cam.aspect;
            }
            else
            {
                cam.orthographicSize = referenceHeight;
            }

            prevAspect = cam.aspect;
        }

        private void Update()
        {
            if (Math.Abs(cam.aspect - prevAspect) > float.Epsilon)
            {
                AdjustAspect();
            }

            if (App.Initialized == false)
            {
                return;
            }

            if (dragging == false)
            {
                if (IsPointerDown())
                {
                    dragVelocity = 0;
                    pointerDown = true;
                    tapPosition = GetWorldPointer().y;
                    prevScrollPos = GetWorldPointer().y;
                    dragging = true;
                }
            }

            if (pointerDown && IsPointerUp())
            {
                pointerDown = false;
            }

            if (dragging)
            {
                float worldPointer = GetWorldPointer().y;

                if (Mathf.Abs(worldPointer - tapPosition) >= scrollStartThresholdDistance)
                {
                    dragVelocity = Mathf.Lerp(dragVelocity, prevScrollPos - worldPointer, Time.deltaTime * scrollSpeed);

                    tapPosition = float.MaxValue / 2;
                }

                prevScrollPos = worldPointer;

                if (IsPointerUp())
                {
                    dragging = false;
                }
            }
            else
            {
                if (Mathf.Approximately(dragVelocity, 0) == false)
                {
                    dragVelocity = Mathf.Lerp(dragVelocity, 0, damping * Time.deltaTime);
                }
            }

            Move(dragVelocity);
        }

        private void Move(float delta)
        {
            if (Math.Abs(delta) < float.Epsilon)
            {
                return;
            }

            Vector3 targetPos = transform.position;
            targetPos.y += delta;
            targetPos.y = GetClampedPositionY(targetPos.y);
            transform.position = targetPos;
        }

        private float GetClampedPositionY(float y)
        {
            float limit = limitHandler?.GetBottomPoint() ?? 0;
            float orthographicSize = cam.orthographicSize;
            return Mathf.Clamp(y, limit + bottomPadding + orthographicSize, topPadding - orthographicSize);
        }
    }
}