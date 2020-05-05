namespace SwiftFramework.Core.SharedData
{
    public interface IProgressData
    {
        string GetId();
        (int current, int target) GetProgress(ProgressItemConfig config);
        float GetProgressNormalized(ProgressItemConfig config);
        ProgressItemStatus GetStatus(ProgressItemConfig config);
        int GetReward(ProgressItemConfig config);
    }

}
