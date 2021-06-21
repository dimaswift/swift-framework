using System.Collections.Generic;

namespace Swift.Core
{
    public interface ICloudSave
    {
        IEnumerable<(string, string)> GetValuesDescription();
        long SaveTimestamp { get; }
    }
}
