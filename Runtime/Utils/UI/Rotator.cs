using UnityEngine;

namespace Swift.Utils.UI
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private float speed = 360;

        private void Update()
        {
            transform.Rotate(Time.unscaledDeltaTime * Vector3.forward * speed);
        }
    }

}
