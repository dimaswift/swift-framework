using System;

namespace SwiftFramework.Core.SharedData
{
    public class Phase<T>
    {
        public T state;
        public float speed;
        public int iterations;
        public Action onComplete;
        public Action onStart;
        public Func<bool> completeCondition;
        public Func<bool> startCondition;
        public Func<float> time;
    }
}
