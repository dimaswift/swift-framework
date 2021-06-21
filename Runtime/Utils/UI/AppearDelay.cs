using System;
using Swift.Core;
using UnityEngine;

namespace Swift.Utils.UI
{
    public class AppearDelay : MonoBehaviour
    {
        [SerializeField] private float delay = 1f;
        [SerializeField] private GenericAnimation objectToAppear = new GenericAnimation();
        
        private void OnEnable()
        {
            objectToAppear.SetActive(false);
            App.Core.Timer.WaitForUnscaled(delay).Done(() =>
            {
                objectToAppear.SetActive(true);
                objectToAppear.Animate();
            });
        }
    }
}