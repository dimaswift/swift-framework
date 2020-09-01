using System;

namespace SwiftFramework.Core
{
    public interface IGenericAnimation
    {
        IPromise Animate();
    }

    [Serializable]
    public class GenericAnimation : InterfaceComponentField<IGenericAnimation>
    {
        public IPromise Animate() => HasValue ? Value.Animate() : Promise.Resolved();
    }
}