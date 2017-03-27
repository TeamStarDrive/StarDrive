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
        // clamp a value between [min, max]: min <= value <= max
        public static float Clamp(this float value, float min, float max)
        {
            return Max(min, Min(value, max));
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


        public static Vector2 Normalized(this Vector2 v)
        {
            float len = (float)Sqrt(v.X * v.X + v.Y * v.Y);
            return len > 0.0000001f ? new Vector2(v.X / len, v.Y / len) : new Vector2();
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

        // Angle degrees from origin to tgt; result between [0, 360)
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

        // assuming this is a direction vector, gives the right side perpendicular vector
        public static Vector2 RightVector(this Vector2 directionVector)
        {
            return new Vector2(directionVector.Y, -directionVector.X);
        }

        // assuming this is a direction vector, gives the left side perpendicular vector
        public static Vector2 LeftVector(this Vector2 directionVector)
        {
            return new Vector2(-directionVector.Y, directionVector.X);
        }

        // Converts rotation radians into a 2D direction vector
        public static Vector2 RadiansToDirection(this float radians)
        {
            return new Vector2((float)Sin(radians), -(float)Cos(radians));
        }

        // Converts an angle value to a 2D direction vector
        public static Vector2 AngleToDirection(this float degrees)
        {
            double rads = degrees * (PI / 180.0);
            return new Vector2((float)Sin(rads), -(float)Cos(rads));
        }

        public static Vector2 FindVectorBehindTarget(this GameplayObject ship, float distance)
        {
            Vector2 forward = new Vector2((float)Sin(ship.Rotation), -(float)Cos(ship.Rotation));
            forward = Vector2.Normalize(forward);
            return ship.Position - (forward * distance);
        }

        public static Vector2 FindVectorToTarget(this Vector2 origin, Vector2 target)
        {
            return Vector2.Normalize(target - origin);
        }

        public static Vector2 FindPredictedTargetPosition(this Vector2 weaponPos, Vector2 ownerVelocity,
            float projectileSpeed, Vector2 targetPos, Vector2 targetVelocity)
        {
            //Vector2 pos0 = weaponPos.FindPredictedTargetPosition0(ownerVelocity, projectileSpeed , targetPos, targetVelocity);
            Vector2 pos1 = weaponPos.FindPredictedTargetPosition1(ownerVelocity, projectileSpeed , targetPos, targetVelocity);
            //Log.Info("PredictTargetPos 0={0}  1={1}", pos0, pos1);
            return pos1;
        }

        public static Vector2 FindPredictedTargetPosition0(this Vector2 weaponPos, Vector2 ownerVelocity,
            float projectileSpeed, Vector2 targetPos, Vector2 targetVelocity)
        {
            Vector2 vectorToTarget     = targetPos - weaponPos;
            Vector2 projectileVelocity = ownerVelocity + ownerVelocity.Normalized() * projectileSpeed;            
            float distance = vectorToTarget.Length();
            float time = distance / projectileVelocity.Length();
            return targetPos + targetVelocity * time;
        }

        public static Vector2 FindPredictedTargetPosition1(this Vector2 weaponPos, Vector2 ownerVelocity, 
            float projectileSpeed, Vector2 targetPos, Vector2 targetVelocity)
        {
            Vector2 delta = targetPos - weaponPos;

            // projectile inherits parent velocity
            Vector2 projectileVelocity = ownerVelocity + ownerVelocity.Normalized() * projectileSpeed;

            float a = Vector2.Dot(targetVelocity, targetVelocity) - projectileVelocity.LengthSquared();
            float bm = -2 * Vector2.Dot(delta, targetVelocity);
            float c = Vector2.Dot(delta, delta);

            // Then solve the quadratic equation for a, b, and c.That is, time = (-b + -sqrt(b * b - 4 * a * c)) / 2a.
            if (Abs(a) < 0.0001f)
                return Vector2.Zero; // no solution

            float sqrt = bm*bm - 4 * a * c;
            if (sqrt < 0.0f)
                return Vector2.Zero; // no solution
            sqrt = (float)Sqrt(sqrt);

            // Those values are the time values at which point you can hit the target.
            float timeToImpact1 = (bm - sqrt) / (2 * a);
            float timeToImpact2 = (bm + sqrt) / (2 * a);

            // If any of them are negative, discard them, because you can't send the target back in time to hit it.  
            // Take any of the remaining positive values (probably the smaller one).
            if (timeToImpact1 < 0.0f && timeToImpact2 < 0.0f)
                return Vector2.Zero; // no solution, can't go back in time

            float predictedTimeToImpact;
            if      (timeToImpact1 < 0.0f) predictedTimeToImpact = timeToImpact2;
            else if (timeToImpact2 < 0.0f) predictedTimeToImpact = timeToImpact1;
            else predictedTimeToImpact = timeToImpact1 < timeToImpact2 ? timeToImpact1 : timeToImpact2;

            // this is the predicted target position
            return targetPos + targetVelocity * predictedTimeToImpact;
        }

        // can be used for collision detection
        public static Vector2 FindClosestPointOnLine(this Vector2 center, Vector2 lineStart, Vector2 lineEnd)
        {
            float a1 = lineEnd.Y - lineStart.Y;
            float b1 = lineStart.X - lineEnd.X;
            float c1 = (lineEnd.Y - lineStart.Y) * lineStart.X + (lineStart.X - lineEnd.X) * lineStart.Y;
            float c2 = -b1 * center.X + a1 * center.Y;
            float det = a1*a1 + b1*b1;
            if (det > 0.0f)
            {
                return new Vector2(
                    (a1 * c1 - b1 * c2) / det,
                    (a1 * c2 - -b1 * c1) / det);
            }
            return center;
        }

        // does this wide RAY collide with our Circle?
        public static bool RayHitTestCircle(this Vector2 center, float radius, Vector2 rayStart, Vector2 rayEnd, float rayWidth)
        {
            float a1 = rayEnd.Y - rayStart.Y;
            float b1 = rayStart.X - rayEnd.X;
            float c1 = (rayEnd.Y - rayStart.Y) * rayStart.X + (rayStart.X - rayEnd.X) * rayStart.Y;
            float c2 = -b1 * center.X + a1 * center.Y;
            float det = a1 * a1 + b1 * b1;
            if (det > 0.0f)
            {
                float r2 = radius + rayWidth / 2;
                float dx = center.X - ((a1 * c1 - b1 * c2) / det);
                float dy = center.Y - ((a1 * c2 - -b1 * c1) / det);
                return dx * dx + dy * dy <= r2 * r2;
            }
            return true;
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

        // @todo AngleDiffTo to ?What? 
        public static float AngleDiffTo(this GameplayObject origin, Vector2 target, out Vector2 right, out Vector2 forward)
        {
            forward = new Vector2((float)Sin(origin.Rotation), -(float)Cos(origin.Rotation));
            right = new Vector2(forward.Y, -forward.X);
            return (float)Acos(Vector2.Dot(target, forward));
        }

        public static float Facing(this Vector2 facingTo, Vector2 right)
        {
            return Vector2.Dot(Vector2.Normalize(facingTo), right) > 0f ? 1f : -1f;
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

        // Returns true if a is almost equal to b, within float epsilon error margin
        public static bool AlmostEqual(this float a, float b)
        {
            float delta = a - b;
            return -1.40129846432482E-45 <= delta && delta <= 1.40129846432482E-45;
        }

        public static Vector2 ProjectTo2D(this Viewport viewport, Vector3 source, ref Matrix projection, ref Matrix view)
        {
            Matrix.Multiply(ref view, ref projection, out Matrix matrix);
            Vector3.Transform(ref source, ref matrix, out Vector3 vector3);
            float len = source.X*matrix.M14 + source.Y*matrix.M24 + source.Z*matrix.M34 + matrix.M44;
            if (!len.AlmostEqual(1f)) // normalize
                vector3 /= len;
            return new Vector2((vector3.X + 1.0f)  * 0.5f * viewport.Width  + viewport.X,
                               (-vector3.Y + 1.0f) * 0.5f * viewport.Height + viewport.Y);
        }
    }
}
