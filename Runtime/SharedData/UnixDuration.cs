using System;

namespace SwiftFramework.Core
{
    [Serializable]
    public struct UnixDuration
    {
        public long seconds;

        public UnixDuration(long seconds, long minutes, long hours, long days)
        {
            this.seconds = GetSeconds(seconds, minutes, hours, days);
        }

        public UnixDuration(long seconds)
        {
            this.seconds = seconds;
        }

        public static long GetSeconds(long seconds, long minutes, long hours, long days)
        {
            return (days * 24 * 60 * 60) + (hours * 60 * 60) + (minutes * 60) + seconds;
        }

        public static bool operator ==(UnixDuration a, UnixDuration b)
        {
            return a.seconds == b.seconds;
        }

        public static bool operator >(UnixDuration a, UnixDuration b)
        {
            return a.seconds > b.seconds;
        }

        public static bool operator <(UnixDuration a, UnixDuration b)
        {
            return a.seconds < b.seconds;
        }

        public static bool operator >=(UnixDuration a, UnixDuration b)
        {
            return a.seconds >= b.seconds;
        }

        public static bool operator <=(UnixDuration a, UnixDuration b)
        {
            return a.seconds <= b.seconds;
        }

        public static bool operator ==(int a, UnixDuration b)
        {
            return a == b.seconds;
        }

        public static bool operator !=(int a, UnixDuration b)
        {
            return a != b.seconds;
        }

        public static bool operator ==(long a, UnixDuration b)
        {
            return a == b.seconds;
        }

        public static bool operator !=(long a, UnixDuration b)
        {
            return a != b.seconds;
        }

        public static bool operator >(int a, UnixDuration b)
        {
            return a > b.seconds;
        }

        public static bool operator <(int a, UnixDuration b)
        {
            return a < b.seconds;
        }

        public static bool operator >=(int a, UnixDuration b)
        {
            return a >= b.seconds;
        }

        public static bool operator <=(int a, UnixDuration b)
        {
            return a <= b.seconds;
        }

        public static bool operator <(long a, UnixDuration b)
        {
            return a < b.seconds;
        }

        public static bool operator >(long a, UnixDuration b)
        {
            return a > b.seconds;
        }

        public static bool operator >(UnixDuration a, int b)
        {
            return a.seconds > b;
        }

        public static bool operator <(UnixDuration a, long b)
        {
            return a.seconds < b;
        }

        public static bool operator >(UnixDuration a, long b)
        {
            return a.seconds > b;
        }

        public static bool operator <=(UnixDuration a, long b)
        {
            return a.seconds <= b;
        }

        public static bool operator >=(UnixDuration a, long b)
        {
            return a.seconds >= b;
        }

        public static bool operator <(UnixDuration a, int b)
        {
            return a.seconds < b;
        }

        public static UnixDuration operator +(UnixDuration a, UnixDuration b)
        {
            return new UnixDuration(a.seconds + b.seconds);
        }

        public static UnixDuration operator -(UnixDuration a, UnixDuration b)
        {
            return new UnixDuration(a.seconds - b.seconds);
        }

        public static UnixDuration operator +(UnixDuration a, int b)
        {
            return new UnixDuration(a.seconds + b);
        }

        public static UnixDuration operator -(UnixDuration a, int b)
        {
            return new UnixDuration(a.seconds - b);
        }

        public static UnixDuration operator +(UnixDuration a, long b)
        {
            return new UnixDuration(a.seconds + b);
        }

        public static UnixDuration operator -(UnixDuration a, long b)
        {
            return new UnixDuration(a.seconds - b);
        }

        public static UnixDuration operator *(UnixDuration a, UnixDuration b)
        {
            return new UnixDuration(a.seconds * b.seconds);
        }

        public static UnixDuration operator *(UnixDuration a, int b)
        {
            return new UnixDuration(a.seconds * b);
        }

        public static UnixDuration operator /(UnixDuration a, long b)
        {
            return new UnixDuration(a.seconds * b);
        }

        public static UnixDuration operator /(UnixDuration a, UnixDuration b)
        {
            return new UnixDuration(a.seconds / b.seconds);
        }

        public static UnixDuration operator /(UnixDuration a, int b)
        {
            return new UnixDuration(a.seconds / b);
        }

        public static implicit operator UnixDuration(int value)
        {
            return new UnixDuration(value);
        }

        public override string ToString()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            return $"{timeSpan.Seconds::00}s,{timeSpan.Minutes:00}m,{timeSpan.Hours:00}h";
        }

        public string ToDurationString()
        {
            return seconds.ToDurationString(App.Core.Local);
        }

        public static implicit operator UnixDuration(long value)
        {
            return new UnixDuration(value);
        }

        public static bool operator !=(UnixDuration a, UnixDuration b)
        {
            return a.seconds == b.seconds;
        }

        public override int GetHashCode()
        {
            return seconds.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is UnixDuration == false)
            {
                return false;
            }
            return (UnixDuration)obj == this;
        }
    }
}
