using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;
using static System.Math;

namespace Ship_Game
{
    public struct Capsule
    {
        public Vector2 Start;
        public Vector2 End;
        public float Radius;
        public Vector2 Center => (Start + End) * 0.5f;
        public Capsule(Vector2 start, Vector2 end, float radius)
        {
            Start = start;
            End = end;
            Radius = radius;
        }
        public bool HitTest(Vector2 hitPos, float hitRadius)
        {
            return hitPos.RayHitTestCircle(hitRadius, Start, End, Radius);
        }
    }

    // Added by RedFox
    // Note about StarDrive coordinate system:
    //   +X is right on screen
    //   +Y is down on screen
    //   +Z is out of the screen
    public static class MathExt
    {
        // clamp a value between [min, max]: min <= value <= max
        [Obsolete("Extension method float.Clamp has been replaced by float.Clamped")]
        public static float Clamp(this float value, float min, float max)
        {
            return Max(min, Min(value, max));
        }
        [Obsolete("Extension method int.Clamp has been replaced by int.Clamped")]
        public static int Clamp(this int value, int min, int max)
        {
            return Max(min, Min(value, max));
        }

        // clamp a value between [min, max]: min <= value <= max
        public static float Clamped(this float value, float min, float max)
        {
            return Max(min, Min(value, max));
        }
        public static int Clamped(this int value, int min, int max)
        {
            return Max(min, Min(value, max));
        }
        public static Vector2 Clamped(this Vector2 v, float minXy, float maxXy)
        {
            return new Vector2(Max(minXy, Min(v.X, maxXy)), 
                               Max(minXy, Min(v.Y, maxXy)));
        }
        public static Vector2 Clamped(this Vector2 v, float minX, float minY, float maxX, float maxY)
        {
            return new Vector2(Max(minX, Min(v.X, maxX)), 
                               Max(minY, Min(v.Y, maxY)));
        }
        public static Vector2 Clamped(this Vector2 v, Vector2 min, Vector2 max)
        {
            return new Vector2(Max(min.X, Min(v.X, max.X)),
                               Max(min.Y, Min(v.Y, max.Y)));
        }

        // Angle normalized to [0, 360] degrees
        public static float NormalizedAngle(this float angle)
        {
            float result = angle;
            if (result >= 360f)
            {
                do   { result -= 360f; }
                while (result >= 360f);
            }
            else if (result < 0f)
            {
                do   { result += 360f; }
                while (result < 0f);
            }
            return result;
        }

        // Basic Linear Interpolation
        public static float LerpTo(this float minValue, float maxValue, float amount)
        {
            return minValue + (maxValue - minValue) * amount;
        }

        public static Vector3 LerpTo(this Vector3 start, Vector3 end, float amount)
        {
            return new Vector3( start.X + (end.X - start.X) * amount,
                                start.Y + (end.Y - start.Y) * amount,
                                start.Z + (end.Z - start.Z) * amount );
        }

        public static Color LerpTo(this Color start, Color end, float amount)
        {
            return new Color((byte)(start.R + (end.R - start.R) * amount),
                             (byte)(start.G + (end.G - start.G) * amount),
                             (byte)(start.B + (end.B - start.B) * amount));
        }

        // This will smoothstep "fromValue" towards "targetValue"
        // @warning "fromValue" WILL CHANGE
        // @return The new "fromValue"
        public static float SmoothStep(this float fromValue, float targetValue, float amount)
        {
            float clamped = amount.Clamped(0f, 1f);
            return fromValue.LerpTo(targetValue, clamped*clamped * (3f - 2f * clamped));
        }

        public static float SmoothStep(ref float fromValue, float targetValue, float amount)
        {
            float clamped = amount.Clamped(0f, 1f);
            fromValue = fromValue.LerpTo(targetValue, clamped * clamped * (3f - 2f * clamped));
            return fromValue;
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

        public static float Distance(this Vector2 a, ref Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Sqrt(dx * dx + dy * dy);
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


        public static float Dot(this Vector2 a, Vector2 b) => a.X*b.X + a.Y*b.Y;
        public static float Dot(this Vector3 a, Vector3 b) => a.X*b.X + a.Y*b.Y + a.Z*b.Z;


        public static Vector2 Normalized(this Vector2 v)
        {
            float len = (float)Sqrt(v.X*v.X + v.Y*v.Y);
            return len > 0.0000001f ? new Vector2(v.X / len, v.Y / len) : new Vector2();
        }

        public static Vector2 Normalized(this Vector2 v, float newMagnitude)
        {
            float len = (float)Sqrt(v.X*v.X + v.Y*v.Y) / newMagnitude;
            return len > 0.0000001f ? new Vector2(v.X / len, v.Y / len) : new Vector2();
        }

        public static Vector3 Normalized(this Vector3 v)
        {
            float len = (float)Sqrt(v.X*v.X + v.Y*v.Y + v.Z*v.Z);
            return len > 0.0000001f ? new Vector3(v.X / len, v.Y / len, v.Z / len) : new Vector3();
        }

        // True if this given position is within the radius of Circle [center,radius]
        public static bool InRadius(this Vector2 position, Vector2 center, float radius)
            => position.SqDist(center) <= radius*radius;
        public static bool InRadius(this Vector3 position, Vector3 center, float radius)
            => position.SqDist(center) <= radius*radius;
        public static bool InRadius(this Vector3 position, Vector2 center, float radius)
            => position.SqDist(center.ToVec3()) <= radius*radius;

        // Reverse of WithinRadius, returns true if position is outside of Circle [center,radius]
        public static bool OutsideRadius(this Vector2 position, Vector2 center, float radius)
            => position.SqDist(center) > radius*radius;
        public static bool OutsideRadius(this Vector3 position, Vector3 center, float radius)
            => position.SqDist(center) > radius*radius;


        // Returns true if Frustrum either partially or fully contains this 2D circle
        public static bool Contains(this BoundingFrustum frustrum, Vector2 center, float radius)
        {
            return frustrum.Contains(new BoundingSphere(new Vector3(center, 0f), radius))
                != ContainmentType.Disjoint; // Disjoint: no intersection at all
        }
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

        // Creates a new Rectangle from this Vector2, where Rectangle X, Y are the Vector2 X, Y
        public static Rectangle ToRect(this Vector2 a, int width, int height)
            => new Rectangle((int)a.X, (int)a.Y, width, height);

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

        public static Vector2 Size(this Texture2D texture) => new Vector2(texture.Width, texture.Height);

        public static Color Alpha(this Color color, float newAlpha) => new Color(color, newAlpha);

        public static bool HitTest(this Rectangle r, Vector2 pos)
        {
            return pos.X > r.X && pos.Y > r.Y && pos.X < r.X + r.Width && pos.Y < r.Y + r.Height;
        }
        public static bool HitTest(this Rectangle r, int x, int y)
        {
            return x > r.X && y > r.Y && x < r.X + r.Width && y < r.Y + r.Height;
        }

        public static Point Pos(this Rectangle r) => new Point(r.X, r.Y);
        public static Vector2 PosVec(this Rectangle r) => new Vector2(r.X, r.Y);
        public static Vector2 Center(this Rectangle r) => new Vector2(r.X + r.Width*0.5f, r.Y + r.Height*0.5f);

        public static Rectangle Bevel(this Rectangle r, int bevel)
            => new Rectangle(r.X - bevel, r.Y - bevel, r.Width + bevel*2, r.Height + bevel*2);

        public static bool IsDiagonalTo(this Point a, Point b) => Abs(b.X - a.X) > 0 && Abs(b.Y - a.Y) > 0;

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
        // Same as Vector2.RightVector; Z axis is not modified
        public static Vector3 RightVector(this Vector3 direction)
        {
            return new Vector3(-direction.Y, direction.X, direction.Z);
        }
        // Same as Vector3.RightVector; but Z axis is set manually
        public static Vector3 RightVector(this Vector3 direction, float z)
        {
            return new Vector3(-direction.Y, direction.X, z);
        }

        // Converts rotation radians into a 2D direction vector
        public static Vector2 RadiansToDirection(this float radians)
        {
            return new Vector2((float)Sin(radians), -(float)Cos(radians));
        }
        // Converts rotation radians into a 3D direction vector, with Z = 0
        public static Vector3 RadiansToDirection3D(this float radians)
        {
            return new Vector3((float)Sin(radians), -(float)Cos(radians), 0f);
        }

        // Converts an angle value to a 2D direction vector
        public static Vector2 AngleToDirection(this float degrees)
        {
            double rads = degrees * (PI / 180.0);
            return new Vector2((float)Sin(rads), -(float)Cos(rads));
        }
        // Converts an angle value to a 3D direction vector, with Z = 0
        public static Vector3 AngleToDirection3D(this float degrees)
        {
            double rads = degrees * (PI / 180.0);
            return new Vector3((float)Sin(rads), -(float)Cos(rads), 0f);
        }

        public static Vector2 FindVectorBehindTarget(this GameplayObject ship, float distance)
        {
            Vector2 forward = new Vector2((float)Sin(ship.Rotation), -(float)Cos(ship.Rotation));
            forward = Vector2.Normalize(forward);
            return ship.Position - (forward * distance);
        }

        public static Vector2 FindStrafeVectorFromTarget(this GameplayObject ship, float distance, int degrees)
        {
            float rads = ToRadians(degrees);
            Vector2 strafeVector = new Vector2((float)Sin(ship.Rotation + rads), -(float)Cos(ship.Rotation + rads));
            strafeVector = Vector2.Normalize(strafeVector);
            return ship.Position + (strafeVector * distance);
        }


        public static Vector2 DirectionToTarget(this Vector2 origin, Vector2 target)
        {
            float dx = target.X - origin.X;
            float dy = target.Y - origin.Y;
            float len = (float)Sqrt(dx*dx + dy*dy);
            return new Vector2(dx / len, dy / len);
        }

        public static Vector2 Acceleration(this Vector2 startVel, Vector2 endVel, float deltaTime)
        {
            Vector2 deltaV = (endVel - startVel);
            if (deltaV.X.AlmostEqual(0f, 0.001f) && deltaV.Y.AlmostEqual(0f, 0.001f))
                return Vector2.Zero;
            return deltaV / deltaTime;
        }

        public static Vector2 PredictImpact(this Ship ourShip, GameplayObject target)
        {
            return new ImpactPredictor(ourShip, target).Predict();
        }

        public static Vector2 PredictImpact(this Projectile proj, GameplayObject target)
        {
            return new ImpactPredictor(proj, target).Predict();
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
                    (a1 * c1 -  b1 * c2) / det,
                    (a1 * c2 - -b1 * c1) / det);
            }
            return center;
        }

        public static Vector2 NearestPointOnFiniteLine(this Vector2 pnt,  Vector2 start, Vector2 end)
        {
            Vector2 line = (end - start);
            float len = line.Length();
            line.Normalize();

            Vector2 v = pnt - start;
            float d = Vector2.Dot(v, line);
            d = Clamped(d, 0f, len);
            return start + line * d;
        }

        // does this wide RAY collide with our Circle?
        public static bool RayHitTestCircle(this Vector2 center, float radius, Vector2 rayStart, Vector2 rayEnd, float rayRadius)
        {
            float a1 = rayEnd.Y - rayStart.Y;
            float b1 = rayStart.X - rayEnd.X;
            float c1 = (rayEnd.Y - rayStart.Y) * rayStart.X + (rayStart.X - rayEnd.X) * rayStart.Y;
            float c2 = -b1 * center.X + a1 * center.Y;
            float det = a1*a1 + b1*b1;
            if (det > 0.0f)
            {
                float r2 = radius + rayRadius;
                float dx = center.X - ((a1 * c1 - b1 * c2) / det);
                float dy = center.Y - ((a1 * c2 - -b1 * c1) / det);
                return dx * dx + dy * dy <= r2 * r2;
            }
            return true; // ray intersects center?
        }

        // returns distance from rayStart to the point of intersection, OR 0 if no intersect
        public static float RayCircleIntersect(this Vector2 center, float radius, Vector2 rayStart, Vector2 rayEnd)
        {
            Vector2 d = rayEnd - rayStart;
            Vector2 f = rayStart - center;

            float a = d.Dot(d);
            float b = 2 * f.Dot(d);
            float c = f.Dot(f) - radius * radius;

            float discriminant = b*b - 4*a*c;
            if (discriminant < 0f)
                return 0f; // no solutions, complete miss

            discriminant = (float)Sqrt(discriminant);

            float t1 = (-b - discriminant) / (2*a);
            if (0f <= t1 && t1 <= 1f)
                return t1 * d.Length(); // Impale, Poke

            float t2 = (discriminant - b) / (2*a);
            if (0f <= t2 && t2 <= 1f)
                return t2 * d.Length(); // ExitWound

            // no intersection: FallShort, Past, CompletelyInside
            return 0f;
        }
      
        /// <summary>Attempt to intersect two line segments.</summary>
        /// <param name="a">Start of line AB.</param>
        /// <param name="b">End of line AB.</param>
        /// <param name="c">Start of line CD.</param>
        /// <param name="d">End of line CD.</param>
        /// <param name="hit">The point of intersection if within the line segments, or empty..</param>
        /// <returns><c>true</c> if the line segments intersect, otherwise <c>false</c>.</returns>
        public static bool TryLineIntersect(Point a, Point b, Point c, Point d, out Point hit)
        {
            int dxA = b.X - a.X;
            int dyA = b.Y - a.Y;
            int dxD = d.X - c.X;
            int dyD = d.Y - c.Y;

            // t = (q − p) × s / (r × s)
            // u = (q − p) × r / (r × s)
            int denom = dxA*dyD - dyA*dxD;
            if (denom == 0)
            {
                // lines are collinear or parallel
                hit = default(Point);
                return false;
            }
            int dxC = c.X - a.X;
            int dyC = c.Y - a.Y;
            float t = (dxC * dyD - dyC * dxD) / (float)denom;
            float u = (dxC * dyA - dyC * dxA) / (float)denom;

            if (t < 0 || t > 1 || u < 0 || u > 1)
            {
                // line segments do not intersect within their ranges
                hit = default(Point);
                return false;
            }
            hit = new Point(a.X + (int)(dxA * t), 
                            a.Y + (int)(dyA * t));
            return true;
        }


        // Liang-Barsky line clipping algorithm
        // @note This algorithm relies on Y down !! It will not work with Y up
        // Takes an axis aligned bounding rect (0..boundsWidth)x(0..boundsHeight)
        // and two points that form a line [start, end]
        // If result is true (which means line [start,end] intersects bounds) then
        // output is [clippedStart, clippedEnd]. Otherwise no result is written.
        // http://www.skytopia.com/project/articles/compsci/clipping.html
        public static bool ClipLineWithBounds(
            int boundsWidth, int boundsHeight, // Define the x/y clipping values for the border.
            Point start, Point end,            // Define the start and end points of the line.
            ref Point clippedStart, ref Point clippedEnd)  // The clipped points
        {
            float t0 = 0f, t1 = 1f;
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            int p = 0, q = 0;
            int lastX = boundsWidth - 1, lastY = boundsHeight - 1;
            for (int edge = 0; edge < 4; ++edge)
            {
                switch (edge) // Traverse through left, right, bottom, top edges.
                {
                    case 0: p = -dx; q = start.X;         break; // left  edge check
                    case 1: p =  dx; q = lastX - start.X; break; // right edge check
                    case 2: p =  dy; q = lastY - start.Y; break; // bottom edge check
                    case 3: p = -dy; q = start.Y;         break; // top edge check
                }
                if (p == 0 && q < 0)
                    return false;   // (parallel line outside)
                float r = q / (float)p;
                if (p < 0)
                {
                    if (r > t1) return false; // line will clip too far, we're out of bounds
                    if (r > t0) t0 = r;       // clip line from start towards end
                }
                else if (p > 0)
                {
                    if (r < t0) return false; // line will clip too far, we're out of bounds
                    if (r < t1) t1 = r;       // clip line from end towards start
                }
            }
            clippedStart.X = Max(0, Min((int)(start.X + t0 * dx), lastX));
            clippedStart.Y = Max(0, Min((int)(start.Y + t0 * dy), lastY));
            clippedEnd.X   = Max(0, Min((int)(start.X + t1 * dx), lastX));
            clippedEnd.Y   = Max(0, Min((int)(start.Y + t1 * dy), lastY));
            return true;
        }

        // Liang-Barsky line clipping algorithm
        // @note This algorithm relies on Y down !! It will not work with Y up
        // Takes an axis aligned bounding rect (0..boundsWidth)x(0..boundsHeight)
        // and two points that form a line [start, end]
        // If result is true (which means line [start,end] intersects bounds) then
        // output is [clippedStart, clippedEnd]. Otherwise no result is written.
        // http://www.skytopia.com/project/articles/compsci/clipping.html
        public static bool ClipLineWithBounds(
            float boundsWidth, float boundsHeight,     // Define the x/y clipping values for the border.
            Vector2 start, Vector2 end,            // Define the start and end points of the line.
            ref Vector2 clippedStart, ref Vector2 clippedEnd)  // The clipped points
        {
            float t0 = 0f, t1 = 1f;
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float p = 0, q = 0;
            for (int edge = 0; edge < 4; ++edge)
            {
                switch (edge) // Traverse through left, right, bottom, top edges.
                {
                    case 0: p = -dx; q = start.X; break; // left  edge check
                    case 1: p =  dx; q = boundsWidth  - start.X; break; // right edge check
                    case 2: p =  dy; q = boundsHeight - start.Y; break; // bottom edge check
                    case 3: p = -dy; q = start.Y; break; // top edge check
                }
                if (q < 0f && p.AlmostEqual(0f))
                    return false;   // (parallel line outside)
                float r = q / p;
                if (p < 0)
                {
                    if (r > t1) return false; // line will clip too far, we're out of bounds
                    if (r > t0) t0 = r;       // clip line from start towards end
                }
                else if (p > 0)
                {
                    if (r < t0) return false; // line will clip too far, we're out of bounds
                    if (r < t1) t1 = r;       // clip line from end towards start
                }
            }
            clippedStart.X = Max(0, Min(start.X + t0 * dx, boundsWidth  - 0.1f));
            clippedStart.Y = Max(0, Min(start.Y + t0 * dy, boundsHeight - 0.1f));
            clippedEnd.X   = Max(0, Min(start.X + t1 * dx, boundsWidth  - 0.1f));
            clippedEnd.Y   = Max(0, Min(start.Y + t1 * dy, boundsHeight - 0.1f));
            return true;
        }

        public static Vector2 OffSetTo(this Vector2 center, Vector2 target, float distance)
        {
            return center + center.DirectionToTarget(target) * distance;
        }

        public static Vector2 RandomOffset(this Vector2 center, float distance)
        {
            return center + RandomMath.RandomDirection() * distance;
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

        public static Vector2 GenerateRandomPointOnCircle(this Vector2 center, float radius)
        {
            float randomAngle = RandomMath.RandomBetween(0f, 360f);
            return center.PointFromAngle(randomAngle, radius);
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
            right = new Vector2(-forward.Y, forward.X);
            return (float)Acos(target.Dot(forward));
        }

        public static float Facing(this Vector2 facingTo, Vector2 right)
        {
            return Vector2.Normalize(facingTo).Dot(right) > 0f ? 1f : -1f;
        }

        // takes self and rotates it around the center pivot by some radians
        public static Vector2 RotateAroundPoint(this Vector2 self, Vector2 center, float radians)
        {
            float s = (float)Sin(radians);
            float c = (float)Cos(radians);
            return new Vector2(c * (self.X - center.X) - s * (self.Y - center.Y) + center.X,
                               s * (self.X - center.X) + c * (self.Y - center.Y) + center.Y);
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

        public static Point ToGridPoint(this Vector2 vector, float reducer =16f)
        {
            vector.ToGridXY(out int x, out int y, reducer);
            return new Point(x, y);
        }

        public static void ToGridXY(this Vector2 vector, out int x, out int y, float reducer = 16f)
        {
            float xround = vector.X > 0 ? .5f : -.5f;
            float yround = vector.Y > 0 ? .5f : -.5f;
            x = (int)((vector.X / reducer) + xround);
            y = (int)((vector.Y / reducer) + yround);            
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
            return -0.000001f <= delta && delta <= 0.000001f;
        }
        public static bool AlmostEqual(this float a, float b, float tolerance)
        {
            float delta = a - b;
            return -tolerance <= delta && delta <= tolerance;
        }

        /// <summary>Returns true if a less than b or almost equal</summary>
        public static bool LessOrEqual(this float a, float b)
        {
            return a < b || AlmostEqual(a, b);
        }

        /// <summary>Returns true if a greater than b or almost equal</summary>
        public static bool GreaterOrEqual(this float a, float b)
        {
            return a > b || AlmostEqual(a, b);
        }

        /// <summary>Returns true if x is inside the range of [min .. max]</summary>
        public static bool InRange(this float x, float min, float max)
        {
            return (min - 0.000001f) <= x && x <= (max + 0.000001f);
        }

        public static Vector2 ProjectTo2D(this Viewport viewport, Vector3 source, ref Matrix projection, ref Matrix view)
        {
            Matrix.Multiply(ref view, ref projection, out Matrix viewProjection);
            Vector3.Transform(ref source, ref viewProjection, out Vector3 clipSpacePoint);
            float len = source.X*viewProjection.M14 + source.Y*viewProjection.M24 + source.Z*viewProjection.M34 + viewProjection.M44;
            if (!len.AlmostEqual(1f)) // normalize
                clipSpacePoint /= len;
            return new Vector2((clipSpacePoint.X + 1.0f)  * 0.5f * viewport.Width  + viewport.X,
                               (-clipSpacePoint.Y + 1.0f) * 0.5f * viewport.Height + viewport.Y);
        }

        public static Vector3 UnprojectToWorld(this Viewport viewport, int screenX, int screenY, float depth, 
                                               ref Matrix projection, ref Matrix view)
        {
            Matrix.Multiply(ref view, ref projection, out Matrix viewProjection);
            Matrix.Invert(ref viewProjection, out Matrix invViewProj);

            var source = new Vector3(
                (screenX - viewport.X)  / (viewport.Width * 2.0f) - 1.0f,
                (screenY - viewport.Y)  / (viewport.Height * 2.0f) - 1.0f,
                (depth - viewport.MinDepth) / (viewport.MaxDepth - viewport.MinDepth));

            Vector3.Transform(ref source, ref invViewProj, out Vector3 worldPos);
            float len = source.X*invViewProj.M14 + source.Y*invViewProj.M24 + source.Z*invViewProj.M34 + invViewProj.M44;
            if (!len.AlmostEqual(1f))
                worldPos /= len;
            return worldPos;
        }

        public static float Max3(float a, float b, float c) => Max(a, Max(b, c));
    }
}
