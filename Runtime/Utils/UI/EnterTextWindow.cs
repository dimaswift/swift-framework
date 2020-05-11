using SwiftFramework.Core;
using SwiftFramework.Core.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftFramework.Utils.UI
{
    public class EnterTextArgs
    {
        public string titleKey;
        public int characherLimit = 12;
        public int minCharsAmount = 3;
        public string originalText;
    }

    public class EnterTextWindow : WindowWithArgsAndResult<EnterTextArgs, string>
    {
        [SerializeField] private GenericInputText textField = null;
        [SerializeField] private GenericText title = null;
        [SerializeField] private Button confirmButton = null;

        public override void Init(WindowsManager windowsManager)
        {
            base.Init(windowsManager);
            confirmButton.onClick.AddListener(OnConfirmClick);
        }

        private void OnConfirmClick()
        {
            if (textField.Value.Text.Length < Arguments.minCharsAmount)
            {
                App.Core.Windows.Show<PopUpMessage, string>("#too_short");
                return;
            }
            Resolve(textField.Value.Text);
            Hide();
        }

        public override void OnStartShowing(EnterTextArgs args)
        {
            base.OnStartShowing(args);
            title.Text = App.Core.Local.GetText(args.titleKey);
            textField.Value.Text = args.originalText;
            textField.Value.CharacterLimit = args.characherLimit;
        }
    }
}