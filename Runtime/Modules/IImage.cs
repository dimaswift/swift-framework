using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public interface IImage
    {
        void SetSprite(SpriteLink sprite);
        void SetSprite(Sprite sprite);
        void SetAlpha(float alpha);
    }
    
    [Serializable]
    public class GenericImage : InterfaceComponentField<IImage> { }

}