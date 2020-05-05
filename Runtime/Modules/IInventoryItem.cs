namespace SwiftFramework.Core
{
    public interface IInventoryItem
    {
        ILink Link { get; }
        IStatefulEvent<BigNumber> Amount { get; }
        void Add(BigNumber amount);
        bool Take(BigNumber amount);
        SpriteLink Icon { get; }
    }
}
