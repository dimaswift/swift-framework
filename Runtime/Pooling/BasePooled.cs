namespace SwiftFramework.Core.Pooling
{
    public abstract class BasePooled : IPooled
    {
        public virtual int InitialPoolCapacity => 100;

        public bool Active { get; set; }

        private IPool pool;

        public void Init(IPool pool)
        {
            this.pool = pool;
        }

        public void ReturnToPool()
        {
            Active = false;
            pool.Return(this);
            OnReturnedToPool();
        }

        public void TakeFromPool()
        {
            Active = true;
            OnTakenFromPool();
        }

        protected virtual void OnTakenFromPool() { }
        protected virtual void OnReturnedToPool() { }
        public virtual void Dispose() { }

    }
}

