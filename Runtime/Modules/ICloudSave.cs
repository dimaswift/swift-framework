using System.Collections.Generic;

namespace SwiftFramework.Core
{
    public interface ICloudSave
    {
        IEnumerable<(string, string)> GetValuesDescription();
        long SaveTimestamp { get; }
    }
}
