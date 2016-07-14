using System;

namespace BomberBot.Common
{
    public class Location : IEquatable<Location>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Location(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Location);
        }

        public bool Equals(Location p)
        {
            if (Object.ReferenceEquals(p, null)) return false;

            if (Object.ReferenceEquals(this, p)) return true;

            if (this.GetType() != p.GetType()) return false;

            return (X == p.X) && (Y == p.Y);
        }

        public override int GetHashCode()
        {
            return X * 0x00010000 + Y;
        }

        public static bool operator ==(Location lhs, Location rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null)) return true;
                return false;
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(Location lhs, Location rhs)
        {
            return !(lhs == rhs);
        }
    }
}
