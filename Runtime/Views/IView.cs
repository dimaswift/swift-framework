using SwiftFramework.Core.Pooling;
using UnityEngine;

namespace SwiftFramework.Core
{
    public interface IView<T> : IView
    {
        void Init(T controller);
    }

    public interface IView : IPooled
    {
        void Process(float deltaTime);
        GameObject GetRoot();
    }
}
