using Swift.Core;
using UnityEngine;

namespace Swift.Utils.UI
{
    public class AnimateOnEnable : MonoBehaviour
    {
        [SerializeField] private GenericAnimation genericAnimation = new GenericAnimation();
        
        private void OnEnable()
        {
            genericAnimation.Animate();
        }
    }
}