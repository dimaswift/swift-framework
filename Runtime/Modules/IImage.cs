using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public interface IImage
    {
        void SetSprite(SpriteLink sprite);
        void SetSprite(Sprite sprite);
        void SetAlpha(float alpha);
        void SetColor(Color color);
    }
    
    [Serializable]
    public class GenericImage : InterfaceComponentField<IImage> { }

}