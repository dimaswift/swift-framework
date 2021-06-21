using Swift.Core;
using UnityEngine;

namespace Swift.Utils.UI
{
    public class LanguageButton : ElementFor<LanguageView>
    {
        [SerializeField] private GenericImage icon = null;
        [SerializeField] private GenericText languageName = null;
        [SerializeField] private GameObject selectionFrame = null;

        protected override void OnClicked()
        {
            
        }

        protected override void OnSetUp(LanguageView lang)
        {
            if(icon != null)
            {
                icon.Value.SetSprite(lang.Icon);
            }
            languageName.Text = lang.Language.ToString();
            selectionFrame.SetActive(lang.IsCurrentLanguage);
        }
    }
}
