using System.Collections.Generic;
using Swift.Core;
using Swift.Core.Windows;
using UnityEngine;

namespace Swift.Utils.UI
{
    public class LanguageSelectorWindow : WindowWithArgs<ILocalizationManager>
    {
        [SerializeField] private ElementSet languageButtonSet = null;

        private readonly List<LanguageView> languageViews = new List<LanguageView>();

        public override void OnStartShowing(ILocalizationManager local)
        {
            base.OnStartShowing(local);

            UpdateContnent();
        }

        private void UpdateContnent()
        {
            languageViews.Clear();
            ILocalizationManager localization = Arguments;
            SystemLanguage currentLanguage = localization.CurrentLanguage;
            foreach (SystemLanguage availableLanguage in localization.GetAvailableLanguages())
            {
                LanguageView view = new LanguageView()
                {
                    Icon = localization.GetLanguageIcon(availableLanguage),
                    IsCurrentLanguage = currentLanguage == availableLanguage,
                    Language = availableLanguage
                };

                languageViews.Add(view);
            }

            languageButtonSet.SetUp<LanguageButton, LanguageView>(languageViews, (b) => OnLanguageButtonClick(b));
        }

        private void OnLanguageButtonClick(ElementFor<LanguageView> button)
        {
            Arguments.SetLanguage(button.Value.Language);
            UpdateContnent();
        }
    }

    public struct LanguageView
    {
        public SystemLanguage Language { get; set; }
        public Sprite Icon { get; set; }
        public bool IsCurrentLanguage { get; set; }
    }
}
