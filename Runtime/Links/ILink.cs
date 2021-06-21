namespace Swift.Core
{
    public interface ILink
    {
        bool IsEmpty { get; }

        bool HasValue { get; }

        string GetPath();

        bool IsGenerated();
    }
}
