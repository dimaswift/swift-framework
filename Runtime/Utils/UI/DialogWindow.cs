using Swift.Core;
using Swift.Core.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace Swift.Utils.UI
{
    public class DialogWindow : Window
    {
        [SerializeField] private GenericText messageText = null;
        [SerializeField] private GenericText cancelText = null;
        [SerializeField] private GenericText yesText = null;
        [SerializeField] private Button yesButton = null;
        [SerializeField] private Button cancelButton = null;

        private Promise<bool> promise;

        public override void Init(WindowsManager windowsManager)
        {
            base.Init(windowsManager);
            cancelButton.onClick.AddListener(OnCancelClick);
            yesButton.onClick.AddListener(OnYesClick);
        }

        private void OnCancelClick()
        {
            if(promise != null)
            {
                promise.Resolve(false);
                promise = null;
            }

            Hide();
        }

        private void OnYesClick()
        {
            if (promise != null)
            {
                promise.Resolve(true);
                promise = null;
            }

            Hide();
        }

        public IPromise<bool> ShowDialog(string messageKey, string yesKey = "#yes", string cancelKey = "#cancel")
        {
            promise = Promise<bool>.Create();
            messageText.Text = App.Core.Local.GetText(messageKey);
            cancelText.Text = App.Core.Local.GetText(cancelKey);
            yesText.Text = App.Core.Local.GetText(yesKey);
            return promise;
        }
    }
}