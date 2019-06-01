using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    /// <summary>
    /// This provides higher performance radian Sin/Cos related calculations
    /// </summary>
    public static class RadMath
    {
        public const float  PI      = 3.14159265358979f;        // 180 degrees
        public const float  TwoPI   = 3.14159265358979f * 2.0f; // 360 degrees
        public const double TwoPID  = 3.14159265358979  * 2.0;  // 360 degrees
        public const float  HalfPI  = 3.14159265358979f * 0.5f; // 90 degrees
        public const double HalfPID = 3.14159265358979  * 0.5;   // 90 degrees
        public const float  InvTwoPI = 1.0f / TwoPI;

        const int TableSize = 2000;
        const double TableSizeD = TableSize;

        static readonly float[] CosTable = new float[TableSize]; // Sin and Cos both use the Cos table

        // static constructor ended up being faster
        // than lazy init in Sin/Cos
        static RadMath()
        {
            for (int i = 0; i < TableSize; ++i)
            {
                CosTable[i] = (float)Math.Cos(TwoPID * (i / TableSizeD));
            }
        }

        const float InvTwoPiTableFactor = InvTwoPI * TableSize;

        // Fast Cosine approximation
        public static float Cos(float radians)
        {
            int idx = (int)(radians * InvTwoPiTableFactor) % TableSize;
            if (idx < 0) idx = -idx;
            return CosTable[idx];
        }

        // Fast Sine approximation
        public static float Sin(float radians)
        {
            int idx = (int)((radians-HalfPI) * InvTwoPiTableFactor) % TableSize;
            if (idx < 0) idx = -idx;
            return CosTable[idx];
        }
        
        // Fast Sine approximation
        public static float Sin(double radians)
        {
            int idx = (int)((radians-HalfPID) * InvTwoPiTableFactor) % TableSize;
            if (idx < 0) idx = -idx;
            return CosTable[idx];
        }

        // Converts rotation radians into a 2D direction vector
        [DllImport("SDNative.dll")]
        public static extern Vector2 RadiansToDirection(this float radians);

        // Converts rotation radians into a 3D direction vector, with Z = 0
        public static Vector3 RadiansToDirection3D(this float radians)
        {
            return new Vector3(radians.RadiansToDirection(), 0f);
        }

        // Converts an angle value to a 2D direction vector
        public static Vector2 AngleToDirection(this float degrees)
        {
            float radians = degrees * DegreeToRadian;
            return RadiansToDirection(radians);
        }

        //////////////////////////////////////////////////////////////////////

        // @note This is the StarDrive radian/coordinate system
        public static float RadiansUp    = 0f;
        public static float RadiansRight = HalfPI;
        public static float RadiansDown  = PI;
        public static float RadiansLeft  = PI + HalfPI;

        const float RadianToDegree = 180.0f / PI;
        const float DegreeToRadian = PI / 180.0f;

        // Converts a radian float to degrees
        public static float ToDegrees(this float radians)
        {
            return radians * RadianToDegree;
        }

        // Converts a degree float to radians
        public static float ToRadians(this float degrees)
        {
            return degrees * DegreeToRadian;
        }

        // Converts a direction vector to radians
        public static float ToRadians(this Vector2 direction)
        {
            if (direction.X == 0f && direction.Y == 0f)
                return 0f; // Up

            // atan2(y,x) gives angle in radians from X axis (--> right)
            // StarDrive uses a different coordinate system, where 0.0 is UP
            // so we rotate the result 90 degrees
            float radians = (float)Math.Atan2(direction.Y, direction.X) + HalfPI;
            return radians;
        }

        // Almost identical to `ToRadians()`,
        // but converts the angle to always be in a normalized absolute [0, 2PI] range
        public static float ToNormalizedRadians(this Vector2 direction)
        {
            if (direction.X == 0f && direction.Y == 0f)
                return 0f; // Up

            float radians = (float)Math.Atan2(direction.Y, direction.X) + HalfPI;
            return ToNormalizedRadians(radians);
        }

        // Converts radians angle to always be in a normalized absolute [0, 2PI] range
        public static float ToNormalizedRadians(this float radians)
        {
            float ratio = radians / TwoPI;
            ratio -= (int)ratio;
            if (ratio < 0f) ratio = -ratio;
            return ratio * TwoPI;
        }

        // Converts a direction vector to degrees
        public static float ToDegrees(this Vector2 direction)
        {
            if (direction.X == 0f && direction.Y == 0f)
                return 0f; // Up

            float radians = (float)Math.Atan2(direction.Y, direction.X) + HalfPI;
            return radians * RadianToDegree;
        }

        // Converts a Vector3 with XYZ degrees into Vector3 XYZ radians
        public static Vector3 DegsToRad(this Vector3 degrees)
        {
            return degrees * DegreeToRadian;
        }

        // @note Assumes orbitPos is very close to orbit line
        // @note Radians should be small, for example PI/12
        [DllImport("SDNative.dll")]
        public static extern Vector2 OrbitalRotate(in Vector2 center, in Vector2 orbitPos, float orbitRadius, float radians);

        // This only deals with a local orbit offset, independent from orbit center
        [DllImport("SDNative.dll")]
        public static extern Vector2 OrbitalOffsetRotate(Vector2 offset, float orbitRadius, float radians);
    }
}
