using SwiftFramework.Core;
using UnityEngine;

namespace SwiftFramework.Utils.UI
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