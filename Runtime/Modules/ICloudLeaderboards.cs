using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface ICloudLeaderboards : IModule
    {
        IEnumerable<ILeaderboard> GetLeaderboards();
        IPromise ForcePostAllScores();
    }
}
