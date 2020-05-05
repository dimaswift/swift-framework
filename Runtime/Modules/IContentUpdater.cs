namespace SwiftFramework.Core
{
    public interface IContentUpdater : IModule
    {
        IPromise CheckForUpdatesManually();
        long GetUpdateSize();
        IPromise Download(FileDownloadHandler callback);
        void RestartApp();
        void SkipUpdate();
        bool AllowSkip { get; }
    }

}
