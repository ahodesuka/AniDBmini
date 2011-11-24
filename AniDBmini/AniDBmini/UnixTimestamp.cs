using System;

namespace AniDBmini
{
    public sealed class UnixTimestamp : IEquatable<UnixTimestamp>
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private double _timestamp;

        public UnixTimestamp(double timestamp)
        {
            _timestamp = timestamp;
        }

        public static implicit operator UnixTimestamp(double timestamp)
        {
            return new UnixTimestamp(timestamp);
        }

        public DateTime ToDateTime(bool local = true)
        {
            if (!local)
                return Epoch.AddSeconds(_timestamp);

            return Epoch.AddSeconds(_timestamp).ToLocalTime();
        }

        /// <summary>
        /// Creates a new UnixTimestamp from a datetime.
        /// </summary>
        public static UnixTimestamp FromDateTime(DateTime dt)
        {
            return new UnixTimestamp(dt.Subtract(Epoch).TotalSeconds);
        }

        public override string ToString()
        {
            return _timestamp.ToString();
        }

        #region IEquatable

        public bool Equals(UnixTimestamp other)
        {
            if (Object.ReferenceEquals(other, null))
                return false;

            return _timestamp == other._timestamp;
        }

        #endregion IEquatable

        #region Overrides

        public override bool Equals(object obj)
        {
            return this.Equals(obj as UnixTimestamp);
        }

        public override int GetHashCode()
        {
            return _timestamp.GetHashCode();
        }

        #endregion Overrides

        #region Operators

        public static bool operator ==(UnixTimestamp x, UnixTimestamp y)
        {
            if (Object.ReferenceEquals(x, null))
            {
                if (Object.ReferenceEquals(y, null))
                    return true;

                return false;
            }

            return x.Equals(y);
        }

        public static bool operator !=(UnixTimestamp x, UnixTimestamp y)
        {
            return !(x == y);
        }

        #endregion

    }
}
