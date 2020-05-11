using UnityEngine;
using SwiftFramework.Core;
using UnityEngine.UI;
using System;

namespace SwiftFramework.Utils.UI
{
    [RequireComponent(typeof(Button))]
    public class AsyncButton : MonoBehaviour
    {
        public Button Button
        {
            get
            {
                if (button == null)
                {
                    button = GetComponent<Button>();
                }
                return button;
            }
        }

        [SerializeField] protected GameObject loadingGroup = null;
        [SerializeField] protected GameObject mainGroup = null;

        private Button button;
        private Func<IPromise> promise;
        private bool loading;

        private void Awake()
        {
            Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if(promise == null || loading)
            {
                return;
            }
            loading = true;
            IPromise p = promise();
            loadingGroup.SetActive(true);
            mainGroup.SetActive(false);
            p.Always(() =>
            {
                loadingGroup.SetActive(false);
                mainGroup.SetActive(true);
                loading = false;
            });
        }

        protected void OnEnable()
        {
            loadingGroup.SetActive(false);
            mainGroup.SetActive(true);
        }

        public void Init(Func<IPromise> promise)
        {
            this.promise = promise;
        }
    }

}
