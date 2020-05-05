namespace SwiftFramework.Core.Pooling
{
    public interface IPool
    {
        int CurrentCapacity { get; }
        T Take<T>() where T : class, IPooled;
        void Return(IPooled pooledObject);
        void WarmUp(int capacity);
        IPromise WarmUpAsync(int capacity);
        void ReturnAll();
        void Dispose();
    }
}

