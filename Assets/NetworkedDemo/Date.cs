namespace NetworkedDemo
{
    using System;

    public class Date
    {
        private static readonly DateTime _st = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public ulong milliseconds;

        public Date()
        {
            var t = (DateTime.Now.ToUniversalTime() - _st);
            this.milliseconds = (ulong)t.TotalMilliseconds;
        }

        public static bool operator ==(Date a, Date b)
        {
            return a.milliseconds == b.milliseconds;
        }

        public static bool operator !=(Date a, Date b)
        {
            return !(a == b);
        }

        public static implicit operator ulong(Date a)
        {
            return a.milliseconds;
        }

        public static implicit operator Date(ulong a)
        {
            return new Date()
            {
                milliseconds = a
            };
        }

        public override bool Equals(object obj)
        {
            var d = obj as Date;
            if (d == null)
            {
                return false;
            }

            return d == this;
        }

        public override int GetHashCode()
        {
            return this.milliseconds.GetHashCode();
        }
    }
}