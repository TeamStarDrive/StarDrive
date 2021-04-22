using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using static System.Math;

namespace Ship_Game
{
    public static class Vectors
    {

        // Up in the world is -Y, down is +Y
        public static Vector2 Up    = new Vector2( 0, -1);
        public static Vector2 Down  = new Vector2( 0,  1);
        public static Vector2 Left  = new Vector2(-1,  0);
        public static Vector2 Right = new Vector2( 1,  0);
        public static Vector2 TopLeft  => new Vector2(-1, -1);
        public static Vector2 TopRight => new Vector2(+1, -1);
        public static Vector2 BotLeft  => new Vector2(-1, +1);
        public static Vector2 BotRight => new Vector2(+1, +1);


        public static string String(this Vector3 v, int precision = 0)
        {
            return $"{{X {v.X.String(precision)} Y {v.Y.String(precision)} Z {v.Z.String(precision)}}}";
        }


        public static Vector2 Clamped(this Vector2 v, float minXy, float maxXy)
        {
            return new Vector2( Max(minXy, Min(v.X, maxXy)), 
                                Max(minXy, Min(v.Y, maxXy)) );
        }
        public static Vector2 Clamped(this Vector2 v, float minX, float minY, float maxX, float maxY)
        {
            return new Vector2( Max(minX, Min(v.X, maxX)), 
                                Max(minY, Min(v.Y, maxY)) );
        }
        public static Vector2 Clamped(this Vector2 v, Vector2 min, Vector2 max)
        {
            return new Vector2( Max(min.X, Min(v.X, max.X)),
                                Max(min.Y, Min(v.Y, max.Y)) );
        }

        public static Vector2 LerpTo(this Vector2 start, Vector2 end, float amount)
        {
            return new Vector2( start.X + (end.X - start.X) * amount,
                                start.Y + (end.Y - start.Y) * amount );
        }

        public static Vector3 LerpTo(this Vector3 start, Vector3 end, float amount)
        {
            return new Vector3( start.X + (end.X - start.X) * amount,
                                start.Y + (end.Y - start.Y) * amount,
                                start.Z + (end.Z - start.Z) * amount );
        }

        public static Vector2 Floored(this Vector2 v) => new Vector2((int)v.X, (int)v.Y);
        public static Vector2 Rounded(this Vector2 v) => new Vector2((float)Round(v.X), (float)Round(v.Y));
        public static Vector2 AbsVec(this Vector2 v)  => new Vector2(Abs(v.X), Abs(v.Y));
        public static Vector2 Swapped(this Vector2 v) => new Vector2(v.Y, v.X);

        public static bool AlmostEqual(this Vector2 a, in Vector2 b, float tolerance)
        {
            return a.X.AlmostEqual(b.X, tolerance) && a.Y.AlmostEqual(b.Y, tolerance);
        }

        public static bool AlmostEqual(this Vector2 a, in Vector2 b)
        {
            return a.X.AlmostEqual(b.X) && a.Y.AlmostEqual(b.Y);
        }

        public static bool NotEqual(this Vector2 a, in Vector2 b)
        {
            return a.X.NotEqual(b.X) || a.Y.NotEqual(b.Y);
        }

        public static bool AlmostZero(this Vector2 v)
        {
            return v.X.AlmostZero() && v.Y.AlmostZero();
        }
        public static bool NotZero(this Vector2 v)
        {
            return v.X.NotZero() || v.Y.NotZero();
        }


        public static bool AlmostZero(this Vector3 v)
        {
            return v.X.AlmostZero() && v.Y.AlmostZero() && v.Z.AlmostZero();
        }
        public static bool NotZero(this Vector3 v)
        {
            return v.X.NotZero() || v.Y.NotZero() || v.Z.NotZero();
        }


        public static bool IsUnitVector(this Vector2 v)
        {
            return v.Length().AlmostEqual(1f);
        }

        // assuming this is a direction vector, gives the right side perpendicular vector
        // @note This assumes that +Y is DOWNWARDS on the screen

        public static Vector2 LeftVector(this Vector2 direction)
        {
            return new Vector2(direction.Y, -direction.X);
        }

        // Same as Vector2.LeftVector; Z axis is not modified

        public static Vector3 LeftVector(this Vector3 direction)
        {
            return new Vector3(direction.Y, -direction.X, direction.Z);
        }

        // Same as Vector3.LeftVector; but Z axis is set manually

        public static Vector3 LeftVector(this Vector3 direction, float z)
        {
            return new Vector3(direction.Y, -direction.X, z);
        }

        // assuming this is a direction vector, gives the left side perpendicular vector
        // @note This assumes that +Y is DOWNWARDS on the screen
        public static Vector2 RightVector(this Vector2 direction)
        {
            return new Vector2(-direction.Y, direction.X);
        }

        // Same as Vector3.RightVector; but Z axis is set manually
        public static Vector3 RightVector2D(this in Vector3 direction, float z)
        {
            return new Vector3(-direction.Y, direction.X, z);
        }

        // shipUpVector is required to correctly calculate the result
        public static Vector3 RightVector(this in Vector3 direction, in Vector3 shipUpVector)
        {
            return Vector3.Cross(direction, shipUpVector);
        }

        public static Vector3 LeftVector(this in Vector3 direction, in Vector3 shipUpVector)
        {
            return -Vector3.Cross(direction, shipUpVector);
        }

        // shipUpVector is required to correctly calculate the result
        public static Vector3 UpVector(this in Vector3 direction, in Vector3 shipUpVector)
        {
            var right = RightVector(direction, shipUpVector);
            return Vector3.Cross(right, direction);
        }

        public static Vector3 DownVector(this in Vector3 direction, in Vector3 shipUpVector)
        {
            var right = RightVector(direction, shipUpVector);
            return -Vector3.Cross(right, direction);
        }


        /**
         * Assuming `radians` is an Euler XYZ rotation triplet in Radians,
         * Rotate `point` around (0,0,0)
         */
        public static Vector3 RotateVector(this in Vector3 radians, in Vector3 point)
        {
            return Vector3.Transform(point, radians.RadiansToRotMatrix());
        }

        // "ArcDot" ?
        public static float RadiansFromAxis(this in Vector3 dir, in Vector3 axis)
        {
            float theta = (float)Acos( dir.Dot(axis) );
            return theta;
        }

        // Deconstructs a direction vector to Angle-Axis
        public static Vector3 ToEulerAngles(this Vector3 dir)
        {
            return new Vector3(
                dir.RadiansFromAxis(Vector3.UnitX),
                dir.RadiansFromAxis(Vector3.UnitY),
                dir.RadiansFromAxis(Vector3.UnitZ));
        }

        /**
         * @return Assuming this is a direction vector, gives XYZ Euler rotation in RADIANS
         * X: Roll
         * Y: Pitch
         * Z: Yaw
         */
        public static Vector3 ToEulerAngles2(this Vector3 dir)
        {
            double x = dir.X;
            double y = dir.Y;
            double z = dir.Z;
            double pitchAdjacent = Sqrt(x*x + z*z); 
            double pitch = Atan2(pitchAdjacent, y);
            double yaw   = Atan2(x, z);
            return new Vector3(/*roll:*/0f, (float)pitch, (float)yaw);
        }

        
        // Gets the Squared distance from source point a to destination b
        // This is faster than Vector2.Distance()
        public static float SqDist(this Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx*dx + dy*dy;
        }

        // Squared distance between two Vector3's
        public static float SqDist(this in Vector3 a, in Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return dx*dx + dy*dy + dz*dz;
        }


        // Gets the accurate distance from source point a to destination b
        // This is slower than Vector2.SqDist()

        public static float Distance(this Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Sqrt(dx*dx + dy*dy);
        }

        public static float Distance(this Vector2 a, ref Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Sqrt(dx * dx + dy * dy);
        }

        // Gets the accurate distance from source point a to destination b
        // This is slower than Vector3.SqDist()

        public static float Distance(this in Vector3 a, in Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)Sqrt(dx*dx + dy*dy + dz*dz);
        }


        public static float Dot(this Vector2 a, Vector2 b) => a.X*b.X + a.Y*b.Y;
        public static float Dot(this Vector3 a, Vector3 b) => a.X*b.X + a.Y*b.Y + a.Z*b.Z;

        // Dot product which assumes Vectors a and b are both unit vectors
        // The return value is always guaranteed to be within [-1; +1]
        public static float UnitDot(this Vector2 a, Vector2 b)
        {
            float dot = a.X*b.X + a.Y*b.Y;
            if      (dot < -1f) dot = -1f;
            else if (dot > +1f) dot = +1f;
            return dot;
        }

        public static Vector3 Cross(this in Vector3 a, in Vector3 b)
        {
            Vector3 v;
            v.X = (a.Y * b.Z - a.Z * b.Y);
            v.Y = (a.Z * b.X - a.X * b.Z);
            v.Z = (a.X * b.Y - a.Y * b.X);
            return v;
        }

        public static Vector2 Normalized(this Vector2 v)
        {
            float len = (float)Sqrt(v.X*v.X + v.Y*v.Y);
            return len > 0.0000001f ? new Vector2(v.X / len, v.Y / len) : default;
        }

        public static Vector2 Normalized(this Vector2 v, float newMagnitude)
        {
            float len = (float)Sqrt(v.X*v.X + v.Y*v.Y) / newMagnitude;
            return len > 0.0000001f ? new Vector2(v.X / len, v.Y / len) : default;
        }

        public static Vector3 Normalized(this Vector3 v)
        {
            float len = (float)Sqrt(v.X*v.X + v.Y*v.Y + v.Z*v.Z);
            return len > 0.0000001f ? new Vector3(v.X / len, v.Y / len, v.Z / len) : default;
        }

        
        // True if this given position is within the radius of Circle [center,radius]
        public static bool InRadius(this Vector2 position, Vector2 center, float radius)
            => position.SqDist(center) <= radius*radius;
        public static bool InRadius(this Vector3 position, in Vector3 center, float radius)
            => position.SqDist(center) <= radius*radius;
        public static bool InRadius(this Vector3 position, Vector2 center, float radius)
            => position.SqDist(center.ToVec3()) <= radius*radius;
        public static bool InRadius(this Vector2 position, AO ao)
            => position.SqDist(ao.Center) <= ao.Radius * ao.Radius;

        // Reverse of WithinRadius, returns true if position is outside of Circle [center,radius]
        public static bool OutsideRadius(this Vector2 position, Vector2 center, float radius)
            => position.SqDist(center) > radius*radius;
        public static bool OutsideRadius(this Vector3 position, in Vector3 center, float radius)
            => position.SqDist(center) > radius*radius;

        
        // Widens this Vector2 to a Vector3, the new Z component will have a value of 0f
        public static Vector3 ToVec3(this Vector2 a) => new Vector3(a.X, a.Y, 0f);

        // Widens this Vector2 to a Vector3, the new Z component is provided as argument
        public static Vector3 ToVec3(this Vector2 a, float z) => new Vector3(a.X, a.Y, z);

        // Narrows this Vector3 to a Vector2, the Z component is truncated
        public static Vector2 ToVec2(this Vector3 a) => new Vector2(a.X, a.Y);

        // Creates a new Rectangle from this Vector2, where Rectangle X, Y are the Vector2 X, Y
        public static Rectangle ToRect(this Vector2 a, int width, int height)
            => new Rectangle((int)a.X, (int)a.Y, width, height);

        // Negates this Vector2's components
        public static Vector2 Neg(this Vector2 a) => new Vector2(-a.X, -a.Y);

        // Center of a Texture2D. Not rounded! So 121x121 --> {60.5;60.5}
        public static Vector2 Center(this Texture2D texture)   => new Vector2(texture.Width / 2f, texture.Height / 2f);
        public static Vector2 Position(this Texture2D texture) => new Vector2(texture.Width, texture.Height);

        public static Vector2 Size(this Texture2D texture) => new Vector2(texture.Width, texture.Height);

        
        // Angle degrees from origin to tgt; result between [0, 360)
        public static float AngleToTarget(this Vector2 origin, Vector2 target)
        {
            return (float)(180 - Atan2(target.X - origin.X, target.Y - origin.Y) * 180.0 / PI);
        }

        // result between [0, +2PI)
        public static float RadiansToTarget(this Vector2 origin, Vector2 target)
        {
            return (float)(PI - Atan2(target.X - origin.X, target.Y - origin.Y));
        }

        // how many radian difference from our current direction
        // versus when looking towards position
        public static float AngleDifference(in Vector2 wantedForward, in Vector2 currentForward)
        {
            float dot = wantedForward.UnitDot(currentForward);
            return (float)Acos(dot);
        }

        // how many radian difference from our current direction
        // versus when looking towards position
        public static float AngleDifferenceToPosition(this GameplayObject origin, Vector2 targetPos)
        {
            Vector2 wantedForward = origin.Center.DirectionToTarget(targetPos);
            Vector2 currentForward = origin.Rotation.RadiansToDirection();
            return AngleDifference(wantedForward, currentForward);
        }

        // used for Projectiles 
        public static bool RotationNeededForTarget(this GameplayObject origin, Vector2 targetPos, float minDiff, 
                                                   out float angleDiff, out float rotationDir)
        {
            Vector2 wantedForward = origin.Center.DirectionToTarget(targetPos);
            Vector2 currentForward = origin.Rotation.RadiansToDirection();
            angleDiff = AngleDifference(wantedForward, currentForward);
            if (angleDiff > minDiff)
            {
                rotationDir = wantedForward.Dot(currentForward.RightVector()) > 0f ? 1f : -1f;
                return true;
            }
            rotationDir = 0f;
            return false;
        }


        // @return TRUE if Vector A is pointing at reverse/opposite direction of Vector B
        // @note Tolerance can be used to control strictness of "is reverse"
        //       Ex: 1.0 means it vectors must be PERFECTLY reversed, a == -b
        public static bool IsOppositeOf(this Vector2 a, Vector2 b, float tolerance = 0.75f)
        {
            float dot = a.Normalized().Dot(b.Normalized());
            // if dot product of 2 unit vectors is -1, they are pointing
            // at opposite directions
            // @note LessOrEqual is needed here, due to float imprecision
            return dot < 0f && dot.LessOrEqual(-tolerance);
        }

        public static void Swap(ref Vector2 a, ref Vector2 b)
        {
            Vector2 tmp = a;
            a = b;
            b = tmp;
        }

        public static void Swap(ref Vector3 a, ref Vector3 b)
        {
            Vector3 tmp = a;
            a = b;
            b = tmp;
        }

        public static Vector2 Expand(this Vector2 origin, float amount)
        {
            origin.X += origin.X > 0 ? amount : -amount;
            origin.Y += origin.Y > 0 ? amount : -amount;
            return origin;
        }

        public static Vector2 Contract(this Vector2 origin, float amount)
        {
            origin.X -= origin.X > 0 ? amount : -amount;
            origin.Y += origin.Y > 0 ? amount : -amount;
            return origin;
        }
    }
}
