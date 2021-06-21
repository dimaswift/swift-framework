namespace Swift.Core
{
    public interface ILoadingScreen
    {
        IPromise Show();
        IPromise Hide();
        void SetLoadProgress(float progress);
        void SetVersion(string version);
    }
}
