namespace SwiftFramework.Core
{
    public interface ILinkIconHandler
    {
        SpriteLink GetIcon(ILink link);
    }

    [System.Serializable]
    public sealed class LinkIconHandler : InterfaceComponentField<ILinkIconHandler> { }
}
