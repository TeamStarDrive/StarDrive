using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace SDGraphics
{
    public struct Vector4 : IEquatable<Vector4>
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public static readonly Vector4 Zero = new Vector4();
        public static readonly Vector4 One = new Vector4(1f, 1f, 1f, 1f);

        public static readonly Vector4 UnitX = new Vector4(1f, 0.0f, 0.0f, 0.0f);
        public static readonly Vector4 UnitY = new Vector4(0.0f, 1f, 0.0f, 0.0f);
        public static readonly Vector4 UnitZ = new Vector4(0.0f, 0.0f, 1f, 0.0f);
        public static readonly Vector4 UnitW = new Vector4(0.0f, 0.0f, 0.0f, 1f);

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(Vector2 value, float z, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = z;
            W = w;
        }

        public Vector4(Vector3 value, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = w;
        }

        public Vector4(float value)
        {
            X = Y = Z = W = value;
        }

        public override string ToString()
        {
            CultureInfo c = CultureInfo.CurrentCulture;
            return string.Format(c, "{{X:{0} Y:{1} Z:{2} W:{3}}}", X.ToString(c), Y.ToString(c), Z.ToString(c), W.ToString(c));
        }

        public bool Equals(Vector4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

        public override bool Equals(object obj)
        {
            return obj is Vector4 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                hashCode = (hashCode * 397) ^ W.GetHashCode();
                return hashCode;
            }
        }

        [Pure] public readonly float Length() => (float)Math.Sqrt(X*X + Y*Y + Z*Z + W*W);
        [Pure] public readonly float SqLen() => X*X + Y*Y + Z*Z + W*W;

        public static float Distance(in Vector4 value1, in Vector4 value2)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            float num3 = value1.Z - value2.Z;
            float num4 = value1.W - value2.W;
            return (float)Math.Sqrt(num1 * (double)num1 + num2 * (double)num2 + num3 * (double)num3 + num4 * (double)num4);
        }
    }
}
