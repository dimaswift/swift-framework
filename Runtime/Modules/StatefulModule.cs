namespace Swift.Core
{
    public abstract class StatefulModule<T> : Module where T : class, new()
    {
        protected override void OnInit()
        {
            base.OnInit();
            App.Storage.RegisterState(() => state);
            App.Storage.OnAfterLoad += Storage_OnAfterLoad;
        }

        private void Storage_OnAfterLoad()
        {
            state = App.Storage.LoadOrCreateNew<T>();
        }

        public T State
        {
            get
            {
                if (state != null)
                {
                    return state;
                }
                state = App.Storage.LoadOrCreateNew(GetDefaultState);
                return state;
            }
        }

        private T state;

        protected virtual T GetDefaultState()
        {
            return new T();
        }
    }
}
