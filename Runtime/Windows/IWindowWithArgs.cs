namespace SwiftFramework.Core
{
    public interface IWindowWithArgs<A> : IWindow
    {
        void Show(A args);
        void SetArgs(A args);
    }

    public interface IWindowWithArgsAndResult<A, R> : IWindowWithResult<R>
    {
        void Show(A args);
        void SetArgs(A args);
    }

    public interface IWindowWithResult<R> : IWindow
    {
        IPromise<R> Result { get; }
        void CreateNewResultPromise();
    }
}
