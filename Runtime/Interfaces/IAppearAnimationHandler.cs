using System;

namespace Swift.Core
{
    public interface IAppearAnimationHandler
    {
        void ProcessShowing(float timeNormalized);
        void ProcessHiding(float timeNormalized);
    }

    [Serializable]
    public class AppearAnimationHandler : InterfaceComponentField<IAppearAnimationHandler> { }
}
