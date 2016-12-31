using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SynapseGaming.LightingSystem.Rendering;
using static System.Math;

namespace Ship_Game
{
    // Added by RedFox
    public static class MathExt
    {
        // Gets the Squared distance from source point a to destination b
        // This is faster than Vector2.Distance()
        public static float SqDist(this Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx*dx + dy*dy;
        }

        // Squared distance between two Vector3's
        public static float SqDist(this Vector3 a, Vector3 b)
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


        // Gets the accurate distance from source point a to destination b
        // This is slower than Vector3.SqDist()
        public static float Distance(this Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)Sqrt(dx*dx + dy*dy + dz*dz);
        }


        // True if this given position is within the radius of Circle [center,radius]
        public static bool InRadius(this Vector2 position, Vector2 center, float radius)
            => position.SqDist(center) <= radius*radius;

        // True if this given position is within the radius of Circle [center,radius]
        public static bool InRadius(this Vector3 position, Vector3 center, float radius)
            => position.SqDist(center) <= radius*radius;

        // Reverse of WithinRadius, returns true if position is outside of Circle [center,radius]
        public static bool OutsideRadius(this Vector2 position, Vector2 center, float radius)
            => position.SqDist(center) > radius*radius;


        // Reverse of WithinRadius, returns true if position is outside of Circle [center,radius]
        public static bool OutsideRadius(this Vector3 position, Vector3 center, float radius)
            => position.SqDist(center) > radius*radius;


        // Returns true if Frustrum either partially or fully contains this 2D circle
        public static bool Contains(this BoundingFrustum frustrum, Vector2 center, float radius)
        {
            return frustrum.Contains(new BoundingSphere(new Vector3(center, 0f), radius))
                != ContainmentType.Disjoint; // Disjoint: no intersection at all
        }

        // Returns true if Frustrum either partially or fully contains this 2D circle
        public static bool Contains(this BoundingFrustum frustrum, Vector3 center, float radius)
        {
            return frustrum.Contains(new BoundingSphere(center, radius))
                != ContainmentType.Disjoint; // Disjoint: no intersection at all
        }

        // Widens this Vector2 to a Vector3, the new Z component will have a value of 0f
        public static Vector3 ToVec3(this Vector2 a) => new Vector3(a.X, a.Y, 0f);

        // Widens this Vector2 to a Vector3, the new Z component is provided as argument
        public static Vector3 ToVec3(this Vector2 a, float z) => new Vector3(a.X, a.Y, z);

        // Narrows this Vector3 to a Vector2, the Z component is truncated
        public static Vector2 ToVec2(this Vector3 a) => new Vector2(a.X, a.Y);

        // Negates this Vector2's components
        public static Vector2 Neg(this Vector2 a) => new Vector2(-a.X, -a.Y);

        // Center of a Texture2D. Not rounded! So 121x121 --> {60.5;60.5}
        public static Vector2 Center(this Texture2D texture)   => new Vector2(texture.Width / 2f, texture.Height / 2f);
        public static Vector2 Position(this Texture2D texture) => new Vector2(texture.Width, texture.Height);
        public static Vector2 Pos(this MouseState ms) => new Vector2(ms.X, ms.Y);

        // Center of the screen
        public static Vector2 Center(this ScreenManager screenMgr)
        {
            var p = screenMgr.GraphicsDevice.PresentationParameters;
            return new Vector2(p.BackBufferWidth / 2f, p.BackBufferHeight / 2f);
        }

        // True if pos is inside the rectangle
        public static bool HitTest(this Rectangle r, Vector2 pos)
        {
            return pos.X > r.X && pos.Y > r.Y && pos.X < r.X + r.Width && pos.Y < r.Y + r.Height;
        }
        public static bool HitTest(this Rectangle r, int x, int y)
        {
            return x > r.X && y > r.Y && x < r.X + r.Width && y < r.Y + r.Height;
        }

        // result between [0, 360)
        public static float AngleToTarget(this Vector2 origin, Vector2 target)
        {
            return (float)(180 - Atan2(target.X - origin.X, target.Y - origin.Y) * 180.0 / PI);
        }

        // result from [0, +180, -181, -359]  it's kind of weird, but this is the essential logic from SD source code 
        public static float AngleToTargetSigned(this Vector2 origin, Vector2 target)
        {
            double n = Atan2(target.X - origin.X, target.Y - origin.Y) * 180 / PI;
            double s = n >= 0.0 ? 1.0 : -1.0;
            return (float)((180 - n) * s);
        }

        // result between [0, 2rad)
        public static float RadiansToTarget(this Vector2 origin, Vector2 target)
        {
            return (float)(PI - Atan2(target.X - origin.X, target.Y - origin.Y));
        }

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

        // Converts a Vector3 with XYZ degrees into Vector3 XYZ radians
        public static Vector3 DegsToRad(this Vector3 degrees)
        {
            return degrees * ((float)PI / 180.0f);
        }
        public static Vector2 FindVectorBehindTarget(this GameplayObject ship, float distance)
        {
            Vector2 vector2 = new Vector2(0f, 0f);
            Vector2 forward = new Vector2((float)Math.Sin((double)ship.Rotation), -(float)Math.Cos((double)ship.Rotation));
            forward = Vector2.Normalize(forward);
            return ship.Position - (forward * distance);
        }
        public static Vector2 FindVectorToTarget(this Vector2 origin, Vector2 target)
        {
            return Vector2.Normalize(target - origin);
        }

        public static Vector2 FindPredictedVectorToTarget(this Vector2 origin, float speedToTarget, Vector2 target, Vector2 targetVelocity)
        {
            Vector2 vectorToTarget = target - origin;
            float distance = vectorToTarget.Length();
            float time = distance / speedToTarget;
            Vector2 spaceTraveled = targetVelocity * time;
            Vector2 predictedVector = target + spaceTraveled;
            vectorToTarget = predictedVector;
            return vectorToTarget;
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
        public static float AngleDiffTo(this GameplayObject origin, Vector2 target, out Vector2 right)
        {
            var forward = new Vector2((float)Math.Sin(origin.Rotation), -(float)Math.Cos(origin.Rotation));
            right = new Vector2(-forward.Y, forward.X);
            return (float)Math.Acos(Vector2.Dot(target, forward));
        }

        public static float Facing(this GameplayObject origin, Vector2 right)
        {
            return Vector2.Dot(Vector2.Normalize(origin.Velocity), right) > 0f ? 1f : -1f;
        }

        // Creates a 3D Forward vector from XYZ RADIANS rotation
        // X = Yaw;  Y = Pitch;  Z = Roll
        public static Vector3 RadiansToForward(this Vector3 radians)
        {
            return Matrix.CreateFromYawPitchRoll(radians.X, radians.Y, radians.Z).Forward;
        }
        public static Vector3 RadiansToRight(this Vector3 radians)
        {
            return Matrix.CreateFromYawPitchRoll(radians.X, radians.Y, radians.Z).Right;
        }
        public static Vector3 RadiansToUp(this Vector3 radians)
        {
            return Matrix.CreateFromYawPitchRoll(radians.X, radians.Y, radians.Z).Up;
        }


        // Creates a 3D Forward vector from XYZ DEGREES rotation
        // X = Yaw;  Y = Pitch;  Z = Roll
        public static Vector3 DegreesToForward(this Vector3 degrees) => degrees.DegsToRad().RadiansToForward();
        public static Vector3 DegreesToRight(this Vector3 degrees)   => degrees.DegsToRad().RadiansToRight();
        public static Vector3 DegreesToUp(this Vector3 degrees)      => degrees.DegsToRad().RadiansToUp();


        // Creates an Affine World transformation Matrix
        public static Matrix AffineTransform(Vector3 position, Vector3 rotationRadians, float scale)
        {
            return Matrix.CreateScale(scale)
                * Matrix.CreateRotationX(rotationRadians.X)
                * Matrix.CreateRotationY(rotationRadians.Y)
                * Matrix.CreateRotationZ(rotationRadians.Z)
                * Matrix.CreateTranslation(position);
        }

        // Sets the Affine World transformation Matrix for this SceneObject
        public static void AffineTransform(this SceneObject so, Vector3 position, Vector3 rotationRadians, float scale)
        {
            so.World = Matrix.CreateScale(scale)
                * Matrix.CreateRotationX(rotationRadians.X)
                * Matrix.CreateRotationY(rotationRadians.Y)
                * Matrix.CreateRotationZ(rotationRadians.Z)
                * Matrix.CreateTranslation(position);
        }

        // Sets the Affine World transformation Matrix for this SceneObject
        public static void AffineTransform(this SceneObject so, Vector3 position, float xRads, float yRads, float zRads, float scale)
        {
            so.World = Matrix.CreateScale(scale)
                * Matrix.CreateRotationX(xRads)
                * Matrix.CreateRotationY(yRads)
                * Matrix.CreateRotationZ(zRads)
                * Matrix.CreateTranslation(position);
        }

        // Sets the Affine World transformation Matrix for this SceneObject
        public static void AffineTransform(this SceneObject so, float x, float y, float z, float xRads, float yRads, float zRads, float scale)
        {
            so.World = Matrix.CreateScale(scale)
                * Matrix.CreateRotationX(xRads)
                * Matrix.CreateRotationY(yRads)
                * Matrix.CreateRotationZ(zRads)
                * Matrix.CreateTranslation(x, y, z);
        }

        // Sets the Affine World transformation Matrix for this SceneObject
        public static void AffineTransform(this SceneObject so, Vector2 position, float xRads, float yRads, float zRads)
        {
            so.World = Matrix.CreateRotationX(xRads)
                * Matrix.CreateRotationY(yRads)
                * Matrix.CreateRotationZ(zRads)
                * Matrix.CreateTranslation(position.ToVec3());
        }
    }
}
