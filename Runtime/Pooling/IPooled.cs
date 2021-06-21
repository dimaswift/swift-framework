namespace Swift.Core.Pooling
{
    public interface IPooled
    {
        void Init(IPool pool);
        void TakeFromPool();
        void ReturnToPool();
        int InitialPoolCapacity { get; }
        bool Active { get; set; }
        void Dispose();
    }
}

