using Swift.Core;
using Swift.Utils.UI;

namespace Swift.Utils.UI
{
    public class SoundToggle : SimpleToggle
    {
        private void Awake()
        {
            App.WaitForState(AppState.ModulesInitialized, () =>
            {
                SetValue(App.Core.GetModule<ISoundManager>().IsMutedAll());
                OnValueChanged.AddListener(OnToggleChanged);
            });
        }

        private void OnToggleChanged(bool value)
        {
            App.Core.GetModule<ISoundManager>().SetMutedAll(value);
        }
    }
}

