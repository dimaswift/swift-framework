using System;
using UnityEngine;

namespace Swift.Core
{
    public interface IImage
    {
        void SetSprite(SpriteLink sprite);
        void SetSprite(Sprite sprite);
        void SetAlpha(float alpha);
        void SetColor(Color color);
        bool PreserveAspect { get; set; }
    }
    
    [Serializable]
    public class GenericImage : InterfaceComponentField<IImage> { }

}