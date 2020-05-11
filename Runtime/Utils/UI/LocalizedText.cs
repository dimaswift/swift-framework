using UnityEngine;
using SwiftFramework.Core;

namespace SwiftFramework.Utils.UI
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key = null;

        private IText text;

        private void Start()
        {
            App.WaitForState(AppState.CoreModulesInitialized, () =>
            {
                ILocalizationManager localization = App.Core.GetModule<ILocalizationManager>();
                localization.OnLanguageChanged += Localization_OnLanguageChanged;
            });
        }

        private void Localization_OnLanguageChanged()
        {
            Translate();
        }

        private void Translate()
        {
            if (text == null)
            {
                text = GetComponent<IText>();
            }
            ILocalizationManager localization = App.Core.GetModule<ILocalizationManager>();
            text.Text = localization.GetText(key);
        }

        private void OnEnable()
        {
            App.WaitForState(AppState.CoreModulesInitialized, Translate);
        }
    }
}
