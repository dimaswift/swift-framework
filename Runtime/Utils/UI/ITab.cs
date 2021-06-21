using Swift.Core;
using UnityEngine;

namespace Swift.Utils.UI
{
    public interface ITab
    {
        string Name { get; }
        SpriteLink Icon { get; }
    }
}
