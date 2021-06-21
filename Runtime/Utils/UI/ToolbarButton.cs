using Swift.Core;
using UnityEngine;

namespace Swift.Utils.UI
{
    public class ToolbarButton : ElementFor<ITab>
    {
        public bool Selected { get; private set; }

        [SerializeField] private GenericText nameText = null;
        [SerializeField] private GenericImage icon = null;
        [SerializeField] private GameObject selectedState = null;
        [SerializeField] private GameObject unSelectedState = null;

        protected override void OnClicked()
        {

        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            selectedState.SetActive(selected);
            unSelectedState.SetActive(!selected);
        }

        protected override void OnSetUp(ITab value)
        {
            nameText.Text = value.Name;
            if (icon.HasValue)
            {
                icon.Value.SetSprite(value.Icon);
            }
        }
    }
}
