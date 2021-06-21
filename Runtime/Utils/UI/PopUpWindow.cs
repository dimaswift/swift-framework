using System;
using Swift.Core;
using Swift.Core.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace Swift.Utils.UI
{
    [AddrSingleton(Folders.Windows)]
    public class PopUpWindow : WindowWithArgs<string>
    {
        [SerializeField] private GenericText messageText = null;
        [SerializeField] private Button okayButton = null;
        [SerializeField] private Button cancelButton = null;

        private Action onOkay;
        
        public override void Init(WindowsManager windowsManager)
        {
            base.Init(windowsManager);
            okayButton.onClick.AddListener(OnOkayClicked);
            if (cancelButton != null)
            {
                
                cancelButton.onClick.AddListener(Hide);
            }
        }

        private void OnOkayClicked()
        {
            onOkay?.Invoke();
            onOkay = null;
            Hide();
        }
        
        public void OnOkay(Action onOkay)
        {
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(true);
            }
            this.onOkay = onOkay;
        }

        public override void OnStartShowing(string messageKey)
        {
            base.OnStartShowing(messageKey);
            onOkay = null;
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
            }
            messageText.Text = App.Core.Local.GetText(messageKey);
        }
    }
}