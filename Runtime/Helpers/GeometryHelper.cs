using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Helpers
{
    public static class GeometryHelper
    {
        public static float LookRotation2D(Vector2 from, Vector2 to, float offset = -90)
        {
            return (Mathf.Atan2(from.y - to.y, from.x - to.x) * Mathf.Rad2Deg) + offset;
        }
    }
}
