namespace SwiftFramework.Core
{
    public interface ITimeLimit
    {
        long TimeTillStart { get; }
        long TimeTillEnd { get; }
    }
}
