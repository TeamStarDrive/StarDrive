using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    /// <summary>
    /// This provides higher performance radian Sin/Cos calculation using a lookup table
    /// RadiansToDirection is also implemented via a lookup table
    /// </summary>
    public static class RadMath
    {
        public const float  PI      = 3.14159265358979f;        // 180 degrees
        public const float  TwoPI   = 3.14159265358979f * 2.0f; // 360 degrees
        public const double TwoPID  = 3.14159265358979  * 2.0;  // 360 degrees
        public const float  HalfPI  = 3.14159265358979f * 0.5f; // 90 degrees
        public const double HalfPID = 3.14159265358979  * 0.5;   // 90 degrees

        public const float InvTwoPI = 1.0f / TwoPI;
        public const float InvHalfPI = 1.0f / HalfPI;

        const int TableSize = 10000;
        const float TableSizeF = TableSize;
        const double TableSizeD = TableSize;

        static readonly float[]   CosTable = new float[TableSize]; // Sin and Cos both use the Cos table
        static readonly Vector2[] DirTable = new Vector2[TableSize];

        // static constructor ended up being faster
        // than lazy init in Sin/Cos
        static RadMath()
        {
            for (int i = 0; i < TableSize; ++i)
            {
                CosTable[i] = (float)Math.Cos(TwoPID * (i / TableSizeD));
            }

            for (int i = 0; i < TableSize; ++i)
            {
                double r = TwoPID * (i / TableSizeD);
                // @note This direction formula assumes -Y is UP on the screen,
                //       and Rotation(0rad) will be UP dir
                DirTable[i] = new Vector2((float)Math.Sin(r), -(float)Math.Cos(r));
            }
        }

        // Fast Cosine approximation
        public static float Cos(float radians)
        {
            // cosine is symmetrical, so always use abs value
            if (radians < 0.0f) radians = -radians;

            float r = radians * InvTwoPI;
            r -= (int)r; // clamp [0,1] using integer flooring
            int idx = (int)(TableSizeF * r);
            return CosTable[idx];
        }

        // Fast Sine approximation
        public static float Sin(float radians)
        {
            // shift by -90 degrees so we can use CosTable
            return Cos(radians - HalfPI);
        }

        public static float Sin(double radians)
        {
            // shift by -90 degrees so we can use CosTable
            return Cos((float)radians - HalfPI);
        }

        // Converts rotation radians into a 2D direction vector
        // Conversion is done using a lookup table, which gives roughly 3.5x perf increase
        public static Vector2 RadiansToDirection(this float radians)
        {
            float r = radians * InvTwoPI;
            r -= (int)r; // clamp [-1, +1] using integer flooring

            // for correct direction index, we need to invert the relative pos
            if (r < 0f) r = 1 + r;

            int idx = (int) (TableSizeF * r);
            return DirTable[idx];
        }

        // Converts an angle value to a 2D direction vector
        public static Vector2 AngleToDirection(this float degrees)
        {
            float radians = degrees * (PI / 180.0f);
            return RadiansToDirection(radians);
        }

    }
}
