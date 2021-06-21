using System;

namespace Swift.Core
{
    public interface ISelectableSet
    {
        int SelectedIndex { get; set; }
        event Action<int> OnSelectionChanged;
    }
}