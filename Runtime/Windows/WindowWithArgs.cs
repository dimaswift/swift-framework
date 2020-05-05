using SwiftFramework.Core;

namespace SwiftFramework.Core.Windows
{
    public abstract class WindowWithArgs<T> : Window, IWindowWithArgs<T>
    {
        protected T Arguments { get; private set; }

        public void Show(T arguments)
        {
            Arguments = arguments;
            Show();
        }

        public void SetArgs(T arguments)
        {
            Arguments = arguments;
        }

        public virtual void OnStartShowing(T arguments)
        {

        }

        public override void OnStartShowing()
        {
            OnStartShowing(Arguments);
        }
    }
}
