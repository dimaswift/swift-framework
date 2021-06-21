using Swift.Core;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Swift.Utils.UI
{
    public class ButtonWrapper : Button, IButton
    {
        public bool Interactable
        {
            get
            {
                return interactable;
            }
            set
            {
                interactable = value;
            }
        }

        public void AddListener(UnityAction action)
        {
            onClick.AddListener(action);
        }

        public void RemoveAllListeners()
        {
            onClick.RemoveAllListeners();
        }
        
    }
}
