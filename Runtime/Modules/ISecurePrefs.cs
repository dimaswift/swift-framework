namespace Swift.Core
{
    public interface ISecurePrefs : IModule
    {
        string GetValue(string key);
        void SetValue(string key, string value);
        bool Exists(string key);
        void Save();
        void Delete(string key);
    }
}
