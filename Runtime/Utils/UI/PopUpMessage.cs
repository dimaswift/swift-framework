using SwiftFramework.Core;
using SwiftFramework.Core.Windows;
using UnityEngine;

namespace SwiftFramework.Utils.UI
{
    [AddrSingleton]
    public class PopUpMessage : WindowWithArgs<string>
    {
        
        [SerializeField] private float showDuration = 1f;
        [SerializeField] private GenericText messageText = null;

        public override void OnStartShowing(string args)
        {
            base.OnStartShowing(args);
            messageText.Text = App.Core.Local.GetText(args);
            if (showDuration > 0)
            {
                App.Core.Timer.WaitForUnscaled(showDuration).Done(Hide);
            }
         
        }
    }
}