using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;

namespace Ship_Game
{
    /// <summary>
    /// This provides higher performance radian Sin/Cos related calculations
    /// </summary>
    public static class RadMath
    {
        public const double PID       = 3.14159265358979;        // 180 degrees
        public const double TwoPID    = 3.14159265358979 * 2.0;  // 360 degrees
        public const double HalfPID   = 3.14159265358979 * 0.5;  // 90 degrees
        public const double InvTwoPID = 1.0 / TwoPID;
        public const double Inv360D   = 1.0 / 360.0;
        public const double RadianToDegreeD = 180.0 / PID;
        public const double DegreeToRadianD = PID / 180.0;

        public const float PI        = (float)PID;     // 180 degrees
        public const float TwoPI     = (float)TwoPID;  // 360 degrees
        public const float HalfPI    = (float)HalfPID; // 90 degrees
        public const float InvTwoPI  = (float)InvTwoPID;
        public const float Inv360    = (float)Inv360D;
        public const float RadianToDegree = (float)RadianToDegreeD;
        public const float DegreeToRadian = (float)DegreeToRadianD;

        public const float Deg10AsRads = PI / 18; // 10 degrees, expressed as radians
        public const float Deg20AsRads = Deg10AsRads*2; // 20 degrees, expressed as radians

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
        public const float RadiansUp    = 0f;
        public const float RadiansRight = HalfPI;
        public const float RadiansDown  = PI;
        public const float RadiansLeft  = PI + HalfPI;

        // Converts a radian float to degrees
        public static float ToDegrees(this float radians)
        {
            return radians * RadianToDegree;
        }

        // Converts DEGREES angle to radians
        // Always in a normalized absolute [0, 2PI] range
        public static float ToRadians(this float degrees)
        {
            // BEWARE! Floating point nasal demons lie here!
            float ratio = degrees * Inv360;
            // compare degrees, because Inv360 isn't accurate in
            // case: ToRadians(360f) expected: 2PI
            if (degrees > 360.001f) // NOTE: .001f is important for rounding 360+EPSILON as TwoPI
            {
                return Math.Min((ratio - (int)ratio) * TwoPI, TwoPI); 
            }
            else if (degrees < 0f)
            {
                return Math.Min((1f + ratio - (int)ratio) * TwoPI, TwoPI);
            }
            else
            {
                return Math.Min(ratio * TwoPI, TwoPI);
            }
        }

        // Converts existing unbounded RADIANS angle to
        // a normalized absolute [0, 2PI] range
        public static float AsNormalizedRadians(this float radians)
        {
            // BEWARE! Floating point nasal demons lie here!
            float ratio = radians * InvTwoPI;
            // compare radians, because InvTwoPI isn't accurate in
            // case: AsNormalizedRadians(2PI) expected: 2PI
            const float nearlyTwoPI = TwoPI + 0.001f; // 0.05degrees tolerance
            if (radians > nearlyTwoPI)
            {
                return Math.Min((ratio - (int)ratio) * TwoPI, TwoPI);
            }
            else if (radians < 0f)
            {
                return Math.Min((1f + ratio - (int)ratio) * TwoPI, TwoPI);
            }
            else
            {
                return Math.Min(ratio * TwoPI, TwoPI);
            }
        }

        // Converts a direction vector to radians
        // Always in a normalized absolute [0, 2PI] range
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

        public static bool IsTargetInsideArc(Vector2 origin, Vector2 target,
            float arcFacingRads, float arcSizeRadians)
        {
            // For 360 Arc Sizes, the target is always inside the arc
            // Using Epsilon comparison here because of float imprecision
            const float almost360 = TwoPI - 0.001f; // 0.05 degrees tolerance
            if (arcSizeRadians > almost360)
                return true;

            // NOTE: Atan2(dy,dx) swapped to Atan2(dx,dy)
            // rotates radians -90, because StarDrive uses UP=-Y
            float radsToTarget = PI - (float)Math.Atan2(target.X-origin.X, target.Y-origin.Y); // [0; +2PI]

            // Ship.Rotation and FacingRadians are normalized to [0; +2PI]
            float radsFacing = arcFacingRads; // so this can be 2PI + 2PI
            if (radsFacing > TwoPI)  // normalize back to [0; +2PI]
                radsFacing -= TwoPI;

            // comparing angles is a bit more complicated, due to 0 and 2PI being equivalent
            // so this 180 degree subtraction is needed to constrain the comparison to half circle sector
            // https://gamedev.stackexchange.com/questions/4467/comparing-angles-and-working-out-the-difference
            float difference = PI - Math.Abs(Math.Abs(radsToTarget - radsFacing) - PI);
            return difference <= (arcSizeRadians * 0.5f);
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

        // Takes self and rotates it around the center pivot by some radians
        [DllImport("SDNative.dll")]
        public static extern Vector2 RotateAroundPoint(this in Vector2 self, in Vector2 center, float radians);

        // Takes self and rotates it around world center [0,0] by some radians
        [DllImport("SDNative.dll")]
        public static extern Vector2 RotatePoint(this Vector2 self, float radians);

        // This only deals with a local orbit offset, independent from orbit center
        // So the local center is always [0,0] and the current [offset] is relative to that
        [DllImport("SDNative.dll")]
        public static extern Vector2 OrbitalOffsetRotate(Vector2 offset, float orbitRadius, float radians);
    }
}
