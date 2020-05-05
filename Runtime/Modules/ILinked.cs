namespace SwiftFramework.Core
{
    public interface ILinked
    {
        ILink Link { get; }
        void SetLink(Link link);
    }
}