using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    public interface IEffect
    {
        void Play(Vector3 position, Quaternion rotation = default);
    }

    [Serializable]
    public class Effect : InterfaceComponentField<IEffect> { }
}
