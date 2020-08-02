using SwiftFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Utils.UI
{
    [RequireComponent(typeof(ElementSet))]
    public class Toolbar : MonoBehaviour
    {
        public IStatefulEvent<int> SelectedTab => selectedTab;

        private readonly StatefulEvent<int> selectedTab = new StatefulEvent<int>(-1);

        private ElementSet tabsSet = null;

        public void SetUp(IEnumerable<ITab> tabs, int selectedIndex)
        {
            if (tabsSet == null)
            {
                tabsSet = GetComponent<ElementSet>();
            }

            tabsSet.SetUp<ToolbarButton, ITab>(tabs, selectedButton => 
            {
                int i = 0;       
                foreach (ToolbarButton button in tabsSet.GetActiveElements<ToolbarButton>())
                {
                    if (button == selectedButton)
                    {
                        SetSelected(i);
                    }
                    i++;
                }
            });
            
            SetSelected(selectedIndex);
        }

        public void SetSelected(int index)
        {
            int i = 0;
            foreach (ToolbarButton button in tabsSet.GetActiveElements<ToolbarButton>())
            {
                button.SetSelected(i == index);
                i++;
            }
            selectedTab.SetValue(index);
        }
    }
}
