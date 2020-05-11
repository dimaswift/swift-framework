using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SwiftFramework.Core;
using SwiftFramework.Utils.UI;

namespace SwiftFramework.Utils
{
    public class Physics2DButton : MonoBehaviour, IPhysics2DClick
    {
        public bool Interactable { get; set; } = true;

        private BounceAnimation bounce;

        private UnityEvent onClick = new UnityEvent();

        private bool registered;

        public bool IsPointerDown { get; set; }

        private void Awake()
        {
            bounce = GetComponent<BounceAnimation>();
        }

        public void AddListener(UnityAction action)
        {
            if (!registered)
            {
                registered = true;
                App.WaitForState(AppState.ModulesInitialized, () => 
                {
                    IEventManager eventManager = App.Core.GetModule<IEventManager>();
                    if (eventManager == null)
                    {
                        Debug.LogError($"Physics2DButton requires IEventManager module!");
                        return;
                    }
                    eventManager.Register(this);
                });
            }
            onClick.AddListener(action);
        }

        public void RemoveListener(UnityAction action)
        {
            onClick.RemoveListener(action);
        }

        public void RemoveAllListeners()
        {
            onClick.RemoveAllListeners();
        }

        public void OnClick(Vector3 point)
        {
            if(Interactable == false)
            {
                return;
            }
            onClick.Invoke();
            bounce?.Release();
        }

        public void OnPointerDown(Vector3 point)
        {
            if (Interactable == false)
            {
                return;
            }
            bounce?.Click();
        }

        public void OnPointerUp(Vector3 point)
        {
            bounce?.Release();
        }
    }

}
