using Swift.Helpers;
using UnityEngine;

namespace Swift.Core
{
    public class Route : MonoBehaviour
    {
        private Transform[] points = { };

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (points.Length > 0)
            {
                return;
            }
            points = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                points[i] = transform.GetChild(i);
            }
        }

        public Vector3 Evaluate(float time)
        {
            return Vector3.Lerp(GetStartPoint(), GetEndPoint(), time);
        }

        public Vector3 Evaluate(float time, int index)
        {
            if (IsIndexValid(index) == false)
            {
                return GetStartPoint();
            }
            return Vector3.Lerp(points[index].position, points[index + 1].position, time);
        }

        public Vector3 GetScale(int index)
        {
            if (IsIndexValid(index) == false)
            {
                return Vector3.one;
            }
            return Vector3.one;
        }

        public float GetLookAngle()
        {
            return GeometryHelper.LookRotation2D(GetEndPoint(), GetStartPoint());
        }

        private bool IsIndexValid(int index)
        {
            if (index + 1 >= points.Length)
            {
                Debug.LogWarning($"Cannot evaluate route with start index: <b>{index}</b>. Only <b>{points.Length}</b> specified!");
                return false;
            }
            return true;
        }

        public float GetLookAngle(int index)
        {
            Init();
            if (IsIndexValid(index) == false)
            {
                return 0;
            }
            return GeometryHelper.LookRotation2D(points[index + 1].position, points[index].position);
        }


        public Vector3 GetStartPoint()
        {
            Init();
            return points[0].position;
        }

        public Vector3 GetEndPoint()
        {
            Init();
            return points[points.Length - 1].position;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            for (int i = 1; i < transform.childCount; i++)
            {
                Gizmos.DrawLine(transform.GetChild(i - 1).position, transform.GetChild(i).position);
            }
        }
#endif
    }
}
