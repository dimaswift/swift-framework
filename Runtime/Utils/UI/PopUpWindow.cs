using UnityEngine;
using SwiftFramework.Core.Windows;
using SwiftFramework.Core;
using UnityEngine.UI;

namespace SwiftFramework.Utils.UI
{
    [AddrSingleton(Folders.Windows)]
    public class PopUpWindow : WindowWithArgs<string>
    {
        [SerializeField] private GenericText messageText = null;
        [SerializeField] private GenericButton okayButton = null;

        public override void Init(WindowsManager windowsManager)
        {
            base.Init(windowsManager);
            okayButton.AddListener(OnOkayClicked);
        }

        private void OnOkayClicked()
        {
            Hide();
        }

        public override void OnStartShowing(string messageKey)
        {
            base.OnStartShowing(messageKey);
            messageText.Text = App.Core.Local.GetText(messageKey);
        }
    }
}