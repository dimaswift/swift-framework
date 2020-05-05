using System;

namespace SwiftFramework.Core
{
    [Serializable]
    public struct UnixTimestamp
    {
        public bool HasValue => timestampSeconds != 0;
        public DateTime DateTime => DateTimeOffset.FromUnixTimeSeconds(timestampSeconds).DateTime;

        public long timestampSeconds;

        public UnixTimestamp(long seconds)
        {
            timestampSeconds = seconds;
        }

        public UnixTimestamp(DateTimeOffset date)
        {
            timestampSeconds = date.ToUnixTimeSeconds();
        }
    }
}
