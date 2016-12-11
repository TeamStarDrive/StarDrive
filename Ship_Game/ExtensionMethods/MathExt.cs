using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using static System.Math;

namespace Ship_Game
{
    internal static class MathExt
    {
        // Added by RedFox
        public static float SqDist(this Vector2 a, Vector2 b)
        {
            float dista = a.X-b.X;
            float distb = a.Y-b.Y;
            return dista*dista + distb*distb;
        }

        public static bool WithinRadius(this Vector2 position, Vector2 center, float radius)
        {
            return position.SqDist(center) <= radius*radius;
        }

        // Added by RedFox
        // result between [0, 360)
        public static float AngleToTarget(this Vector2 origin, Vector2 target)
		{
            return (float)(180 - Atan2(target.X - origin.X, target.Y - origin.Y) * 180.0 / PI);
		}

        // Added by RedFox
        // result from [0, +180, -181, -359]  it's kind of weird, but this is the essential logic from SD source code 
        public static float AngleToTargetSigned(this Vector2 origin, Vector2 target)
        {
            double n = Atan2(target.X - origin.X, target.Y - origin.Y) * 180 / PI;
            double s = n >= 0.0 ? 1.0 : -1.0;
            return (float)((180 - n) * s);
        }

        // Added by RedFox
        // result between [0, 2rad)
        public static float RadiansToTarget(this Vector2 origin, Vector2 target)
        {
            return (float)(PI - Atan2(target.X - origin.X, target.Y - origin.Y));
        }

        // Added by RedFox
        // result between [0, 1rad, -1rad, -2rad)  this kinda weird logic again, seems to be used for module overlay rendering
        public static float RadiansToTargetSigned(this Vector2 origin, Vector2 target)
        {
            double n = Atan2(target.X - origin.X, target.Y - origin.Y);
            double s = n >= 0.0 ? 1.0 : -1.0;
            return (float)((PI - n) * s);
        }

        // Converts a radian float to degrees
        public static float ToDegrees(this float radians)
        {
            return radians * (180.0f / (float)PI);
        }

        // Converts a degree float to radians
        public static float ToRadians(this float degrees)
        {
            return degrees * ((float)PI / 180.0f);
        }

        // Converts a direction vector to radians
        public static float ToRadians(this Vector2 direction)
        {
            return (float)(PI - Atan2(direction.X, direction.Y));
        }

        // Converts a direction vector to degrees
        public static float ToDegrees(this Vector2 direction)
        {
            return (float)(180 - Atan2(direction.X, direction.Y) * 180.0 / PI);
        }

        // Generates a new point on a circular radius from position
        // Input angle is given in degrees
		public static Vector2 PointFromAngle(this Vector2 center, float degrees, float circleRadius)
		{
            double rads = degrees * (PI / 180.0);
            return center + new Vector2((float)Sin(rads), (float)-Cos(rads)) * circleRadius;
		}

        // Generates a new point on a circular radius from position
        // Input angle is given in radians
		public static Vector2 PointFromRadians(this Vector2 center, float radians, float circleRadius)
		{
            return center + new Vector2((float)Sin(radians), (float)-Cos(radians)) * circleRadius;
		}

        // @todo This is just an alias for PointFromAngle... which one to keep?
		public static Vector2 PointOnCircle(this Vector2 center, float degrees, float circleRadius)
		{
            // @note manual inlining instead of calling PointFromAngle
            double rads = degrees * (PI / 180.0);
            return center + new Vector2((float)Sin(rads), (float)-Cos(rads)) * circleRadius;
		}

        public static Vector2 PointOnCircle(float degrees, float circleRadius)
        {
            double rads = degrees * (PI / 180.0);
            return new Vector2((float)Sin(rads), (float)-Cos(rads)) * circleRadius;
        }
    }
}
