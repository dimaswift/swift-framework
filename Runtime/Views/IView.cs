using Swift.Core.Pooling;
using UnityEngine;

namespace Swift.Core
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
