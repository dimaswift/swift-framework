using UnityEngine;

namespace SwiftFramework.Core
{
    public class BasePrice : ScriptableObject
    {
        [SerializeField] private SpriteLink icon = Link.CreateNull<SpriteLink>();

        public SpriteLink Icon => icon;
    }
}