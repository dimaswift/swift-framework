using UnityEngine;

namespace SwiftFramework.Core
{
    public interface ICameraController : IModule
    {
        void Release();
        void Lock();
        Camera Main { get; }
    }
}
