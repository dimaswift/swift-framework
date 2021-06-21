using Swift.Core;
using UnityEngine;

namespace Swift.Utils.UI
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

        protected virtual IText GetText() => GetComponent<IText>();

        private void Translate()
        {
            if (text == null)
            {
                text = GetText();
                if (text == null)
                {
                    Debug.LogError($"IText interface is missing on {gameObject.GetFullName()}");
                    return;
                }
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
