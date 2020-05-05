using SwiftFramework.Core;
using System;

namespace SwiftFramework.Core.SharedData
{
    [Serializable]
    public abstract class ProgressItemConfig
    {
        public string id;
        public string titleKey;
        public string descriptionKey;
        public SpriteLink icon;
    }
}
