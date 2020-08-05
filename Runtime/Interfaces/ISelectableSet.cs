using System;

namespace SwiftFramework.Core
{
    public interface ISelectableSet
    {
        int SelectedIndex { get; set; }
        event Action<int> OnSelectionChanged;
    }
}