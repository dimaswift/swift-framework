namespace SwiftFramework.Core
{
    public interface IProgressBar
    {
        void SetUp(float progressNormalized);
        void SetUp(int current, int total);
    }

    [System.Serializable]
    public class ProgressBar : InterfaceComponentField<IProgressBar> { }
}