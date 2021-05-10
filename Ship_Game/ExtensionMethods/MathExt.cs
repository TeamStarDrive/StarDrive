using System;
using System.Diagnostics.Contracts;
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

    public struct Range
    {
        public float Min;
        public float Max;
        public Range(float minMax)
        {
            Min = Max = minMax;
        }
        public Range(float min, float max)
        {
            Min = min; Max = max;
        }
        [Pure] public float Generate()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return Min == Max ? Min : RandomMath.RandomBetween(Min, Max);
        }
        public override string ToString() => $"Range [{Min}, {Max}]";
    }

    // Added by RedFox
    // Note about StarDrive coordinate system:
    //   +X is right on screen
    //   +Y is down on screen
    //   +Z is out of the screen
    public static class MathExt
    {
        // clamp a value between [min, max]: min <= value <= max
        public static float Clamped(this float value, float min, float max)
        {
            if (value <= (min+0.000001f)) return min;
            if (value >= (max-0.000001f)) return max;
            return value;
        }

        public static double Clamped(this double value, double min, double max)
        {
            if (value <= (min+0.000001)) return min;
            if (value >= (max-0.000001)) return max;
            return value;
        }

        public static int Clamped(this int value, int min, int max)
        {
            return Max(min, Min(value, max));
        }

        /// <summary>
        /// Constrain lower end of value
        /// </summary>
        public static float LowerBound(this float value, float min)
        {
            return Max(min, value);
        }
        public static int LowerBound(this int value, int min)
        {
            return Max(min, value);
        }
        public static double LowerBound(this double value, float min)
        {
            return Max(min, value);
        }

        /// <summary>
        /// Constrain upper end of value
        /// </summary>
        public static float UpperBound(this float value, float max)
        {
            return Min(max, value);
        }
        public static int UpperBound(this int value, int max)
        {
            return Min(max, value);
        }


        // This is a common pattern in the codebase, there is some amount
        // and we wish to subtract another value from it, but not beyond 0
        public static void Consume(ref float fromAmount, ref float toConsume)
        {
            if (fromAmount <= 0f || toConsume <= 0f) return; // nothing to consume
            if     (fromAmount >= toConsume) { fromAmount -= toConsume;  toConsume  = 0f; }
            else if (fromAmount < toConsume) { toConsume  -= fromAmount; fromAmount = 0f; }
        }

        // We wish to subtract percentage of a value and return that while updating the value as well
        public static float ConsumePercent(ref float fromAmount, float percent)
        {
            if (fromAmount.LessOrEqual(0)) return 0; // nothing to consume
            float consumed = fromAmount * percent;
            fromAmount -= consumed;
            return consumed;
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
        // ex: 0f.LerpTo(100f, 0.75f) => 75f
        // ex: 100f.LerpTo(0f, 0.75f) => 25f
        public static float LerpTo(this float start, float end, float amount)
        {
            return start + (end - start) * amount;
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


        // Returns true if Frustum either partially or fully contains this 2D circle
        public static bool Contains(this BoundingFrustum frustum, Vector2 center, float radius)
        {
            return frustum.Contains(new BoundingSphere(new Vector3(center, 0f), radius))
                != ContainmentType.Disjoint; // Disjoint: no intersection at all
        }
        public static bool Contains(this BoundingFrustum frustum, Vector3 center, float radius)
        {
            return frustum.Contains(new BoundingSphere(center, radius))
                != ContainmentType.Disjoint; // Disjoint: no intersection at all
        }

        /// <summary>
        /// Example: Color halfOpaqueBlack = Color.Black.Alpha(0.5f);
        /// </summary>
        /// <returns>Copy of this Color with a new alpha value</returns>
        public static Color Alpha(this Color color, float newAlpha)
            => new Color(color, newAlpha);

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
        public static Vector2 Size(this Rectangle r) => new Vector2(r.Width, r.Height);
        public static Vector2 Center(this Rectangle r) => new Vector2(r.X + r.Width*0.5f, r.Y + r.Height*0.5f);
        public static int CenterX(this Rectangle r) => r.X + r.Width/2;
        public static int CenterY(this Rectangle r) => r.Y + r.Height/2;
        public static int Area(this Rectangle r) => r.Width * r.Height;

        public static float CenterTextX(this Rectangle r, in LocalizedText text)
            => CenterTextX(r, text, Fonts.Arial12Bold);

        public static float CenterTextX(this Rectangle r, in LocalizedText text, Graphics.Font font)
            => r.X + r.Width*0.5f - font.TextWidth(text)*0.5f;


        // Example: r.RelativeX(0.5) == r.CenterX()
        //          r.RelativeX(1.0) == r.Right
        public static int RelativeX(this Rectangle r, float percent) => r.X + (int)(r.Width*percent);
        public static int RelativeY(this Rectangle r, float percent) => r.Y + (int)(r.Height*percent);

        public static Vector2 RelPos(this Rectangle r, float relX, float relY)
            => new Vector2(RelativeX(r, relX), RelativeY(r, relY));

        public static Rectangle Bevel(this Rectangle r, int bevel)
            => new Rectangle(r.X - bevel, r.Y - bevel, r.Width + bevel*2, r.Height + bevel*2);

        public static Rectangle Bevel(this Rectangle r, int bevelX, int bevelY)
            => new Rectangle(r.X - bevelX, r.Y - bevelY, r.Width + bevelX*2, r.Height + bevelY*2);

        public static Rectangle Widen(this Rectangle r, int widen)
            => new Rectangle(r.X - widen, r.Y, r.Width + widen*2, r.Height);

        public static Rectangle Move(this Rectangle r, int dx, int dy)
            => new Rectangle(r.X + dx, r.Y + dy, r.Width, r.Height);

        // Cut a chunk off the top of the rectangle
        public static Rectangle CutTop(this Rectangle r, int amount)
            => new Rectangle(r.X, r.Y + amount, r.Width, r.Height - amount);

        public static Rectangle ScaledBy(this Rectangle r, float scale)
        {
            if (scale.AlmostEqual(1f))
                return r;
            float extrude = scale - 1f;
            int extrudeX = (int)(r.Width*extrude);
            int extrudeY = (int)(r.Height*extrude);
            return new Rectangle(r.X - extrudeX, 
                                 r.Y - extrudeY, 
                                 r.Width  + extrudeX*2,
                                 r.Height + extrudeY*2);
        }

        public static bool IsDiagonalTo(this Point a, Point b) => Abs(b.X - a.X) > 0 && Abs(b.Y - a.Y) > 0;

        // Rotates an existing direction vector by another direction vector
        // For this we convert to radians, yielding:
        // newAngle = angle1 + angle2
        public static Vector2 RotateDirection(this Vector2 direction, Vector2 relativeDirection)
        {
            return (direction.ToRadians() + relativeDirection.ToRadians()).RadiansToDirection();
        }

        public static Vector2 FindStrafeVectorFromTarget(this GameplayObject ship, float distance, Vector2 direction)
        {
            Vector2 strafe = ship.Rotation.RadiansToDirection() + direction;
            strafe = strafe.Normalized();
            return ship.Position + (strafe * distance);
        }

        public static Vector3 DirectionToTarget(this Vector3 origin, Vector3 target)
        {
            float dx = target.X - origin.X;
            float dy = target.Y - origin.Y;
            float dz = target.Z - origin.Z;
            float len = (float)Sqrt(dx*dx + dy*dy + dz*dz);
            if (len.AlmostZero())
                return new Vector3(0f, -1f, 0f); // UP
            return new Vector3(dx / len, dy / len, dz / len);
        }

        // this will give values != 1 sometimes resulting in slightly incorrect values
        public static Vector2 DirectionToTarget(this Vector2 origin, Vector2 target)
        {
            float dx = target.X - origin.X;
            float dy = target.Y - origin.Y;
            double len = Sqrt(Pow(dx,2) + Pow(dy,2));
            if (((float)len).AlmostZero())
                return Vectors.Up; // UP
            Vector2 dir = Vector2.Normalize(new Vector2((float)(dx / len), (float)(dy / len)));
            if (System.Diagnostics.Debugger.IsAttached)
            {
                float check = (float)Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                if (check != 1)
                    Log.Error("DirectionToTarget unit vector was not equal 1. Bad unit vector will result in incorrect calculations");
            }
            return dir;
        }

        public static Vector2 PredictImpact(this Ship ourShip, GameplayObject target)
        {
            return new ImpactPredictor(ourShip, target)
                .Predict(ourShip.CanUseAdvancedTargeting);
        }

        public static Vector2 PredictImpact(this Projectile proj, GameplayObject target)
        {
            return new ImpactPredictor(proj, target)
                .Predict(proj.Weapon.CanUseAdvancedTargeting);
        }

        /**
         * Finds the closest point to `center` on a line AB
         * Can be used for collision detection
         * @param center The reference point to find closest point to
         * @param a Start point of the line
         * @param b End point of the line
         * @return closest point to `center` on line A--x-->B
         */
        public static Vector2 FindClosestPointOnLine(this Vector2 pos, Vector2 a, Vector2 b)
        {
            // https://stackoverflow.com/questions/47481774/getting-point-on-line-segment-that-is-closest-to-another-point
            Vector2 ab = b - a;
            Vector2 ap = pos - a;
            // project pos onto the line segment ab
            float abSqLen = (ab.X*ab.X + ab.Y*ab.Y);
            float t = (ap.X*ab.X + ap.Y*ab.Y) / abSqLen; // ap.Dot(ab) / sqLen(ab)

            // clamp t to line segment:
            if      (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;

            return a + t*ab; // recreate the line using clamped projection
        }

        // does this wide RAY collide with our Circle?
        public static bool RayHitTestCircle(this Vector2 center, float radius, Vector2 rayStart, Vector2 rayEnd, float rayRadius)
        {
            Vector2 closest = FindClosestPointOnLine(center, rayStart, rayEnd);
            float r2 = radius + rayRadius;
            float dx = center.X - closest.X;
            float dy = center.Y - closest.Y;
            return (dx*dx + dy*dy) <= r2*r2;
        }

        // @return TRUE and out distance from rayStart to the point of intersection,
        //      OR FALSE if no intersect
        // IF [start->end] is inside the circle, returns abs distance from start to edge of the circle
        public static bool RayCircleIntersect(this Vector2 center, float radius,
            Vector2 rayStart, Vector2 rayEnd, out float distanceFromStart)
        {
            float dx = rayEnd.X - rayStart.X;
            float dy = rayEnd.Y - rayStart.Y;
            float a = dx*dx + dy*dy;
            if (a <= 0.000001f) // No real solutions
            { distanceFromStart = float.NaN; return false; }

            float dcx = rayStart.X - center.X;
            float dcy = rayStart.Y - center.Y;
            float c = (dcx*dcx) + (dcy*dcy) - radius*radius;

            // nasty edge case, the line segment starts inside the circle
            // NEED to handle for proper collision detection
            if (c < 0f)  // collision happened immediately
            { distanceFromStart = 1f; return true; }

            float b = 2f * (dx*dcx + dy*dcy);
            float det = b*b - 4f*a*c;
            if (det < 0f) // No real solutions
            { distanceFromStart = float.NaN; return false; }

            // Two solutions
            det = (float)Sqrt(det);
            float t2 = (-b - det) / (2f * a); // near intersect
            float t1 = (-b + det) / (2f * a); // far intersect

            if (t1 < 0f && t2 < 0f) // It's behind us
            { distanceFromStart = float.NaN; return false; }

            float t = Min(Abs(t2), Abs(t1));
            // [-1, 0] we are inside
            // [0, +1] circle is in front of us
            if (-1f <= t && t <= 1f)
            {
                distanceFromStart = t * (float)Sqrt(dx*dx + dy*dy); // vector length
                return true;
            }

            distanceFromStart = float.NaN;
            return false;
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
                hit = default;
                return false;
            }
            int dxC = c.X - a.X;
            int dyC = c.Y - a.Y;
            float t = (dxC * dyD - dyC * dxD) / (float)denom;
            float u = (dxC * dyA - dyC * dxA) / (float)denom;

            if (t < 0 || t > 1 || u < 0 || u > 1)
            {
                // line segments do not intersect within their ranges
                hit = default;
                return false;
            }
            hit = new Point(a.X + (int)(dxA * t), 
                            a.Y + (int)(dyA * t));
            return true;
        }


        /// <summary>Attempt to intersect two line segments.</summary>
        /// <param name="a">Start of line AB.</param>
        /// <param name="b">End of line AB.</param>
        /// <param name="c">Start of line CD.</param>
        /// <param name="d">End of line CD.</param>
        /// <param name="hit">The point of intersection if within the line segments, or empty..</param>
        /// <returns><c>true</c> if the line segments intersect, otherwise <c>false</c>.</returns>
        public static bool TryLineIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 hit)
        {
            float dxA = b.X - a.X;
            float dyA = b.Y - a.Y;
            float dxD = d.X - c.X;
            float dyD = d.Y - c.Y;

            // t = (q − p) × s / (r × s)
            // u = (q − p) × r / (r × s)
            float denom = dxA*dyD - dyA*dxD;
            if (denom.AlmostZero())
            {
                // lines are collinear or parallel
                hit = default;
                return false;
            }
            float dxC = c.X - a.X;
            float dyC = c.Y - a.Y;
            float t = (dxC * dyD - dyC * dxD) / denom;
            float u = (dxC * dyA - dyC * dxA) / denom;

            if (t < 0 || t > 1 || u < 0 || u > 1)
            {
                // line segments do not intersect within their ranges
                hit = default;
                return false;
            }
            hit = new Vector2(a.X + (dxA * t), 
                              a.Y + (dyA * t));
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

        // Given 2 rectangles, returns out the intersecting rectangle area, or returns false if no intersection
        public static bool GetIntersectingRect(this Rectangle a, in Rectangle b, out Rectangle intersection)
        {
            int leftX   = Max(a.X, b.X);
            int rightX  = Min(a.X + a.Width, b.X + b.Width);
            int topY    = Max(a.Y, b.Y);
            int bottomY = Min(a.Y + a.Height, b.Y + b.Height);

            if (leftX < rightX && topY < bottomY)
            {
                intersection = new Rectangle(leftX, topY, rightX-leftX, bottomY-topY);
                return true;
            }
            intersection = Rectangle.Empty;
            return false;
        }

        public static Vector2 OffsetTowards(this Vector2 center, Vector2 target, float distance)
        {
            return center + center.DirectionToTarget(target) * distance;
        }

        // Gives a random point from a vector within a specified distance
        public static Vector2 RandomOffsetAndDistance(Vector2 center, float distance)
        {
            return center + RandomMath.RandomDirection() * RandomMath.RandomBetween(0, distance);
        }

        // Generates a new point on a circular radius from position
        // Input angle is given in degrees
        public static Vector2 PointFromAngle(this Vector2 center, float degrees, float circleRadius)
        {
            return center + degrees.AngleToDirection() * circleRadius;
        }

        // Generates a new point on a circular radius from position
        // Input angle is given in radians
        public static Vector2 PointFromRadians(this Vector2 center, float radians, float circleRadius)
        {
            return center + radians.RadiansToDirection() * circleRadius;
        }

        public static Vector2 GenerateRandomPointOnCircle(this Vector2 center, float radius)
        {
            float randomAngle = RandomMath.RandomBetween(0f, 360f);
            return center.PointFromAngle(randomAngle, radius);
        }

        public static Vector2 GenerateRandomPointInsideCircle(this Vector2 center, float radius)
        {
            float randomRadius = RandomMath.RandomBetween(0f, radius);
            return center.GenerateRandomPointOnCircle(randomRadius);
        }

        public static Vector2 PointOnCircle(float degrees, float circleRadius)
        {
            return degrees.AngleToDirection() * circleRadius;
        }

        public const float DefaultTolerance = 0.000001f;

        // Returns true if a is almost equal to b, within float epsilon error margin
        public static bool AlmostEqual(this float a, float b)
        {
            float delta = a - b;
            return -DefaultTolerance <= delta && delta <= DefaultTolerance;
        }
        public static bool AlmostEqual(this float a, float b, float tolerance)
        {
            float delta = a - b;
            return -tolerance <= delta && delta <= tolerance;
        }
        public static bool NotEqual(this float a, float b)
        {
            float delta = a - b;
            return delta < -DefaultTolerance || DefaultTolerance <= delta;
        }


        public static bool AlmostZero(this float a)
        {
            return -DefaultTolerance <= a && a <= DefaultTolerance;
        }

        public static bool NotZero(this float a)
        {
            return a < -DefaultTolerance || DefaultTolerance < a;
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

        /// <summary>Returns true if a greater than b and not almost equal</summary>
        public static bool Greater(this float a, float b)
        {
            return a > b && !AlmostEqual(a, b);
        }

        /// <summary>Returns true if a less than b and not almost equal</summary>
        public static bool Less(this float a, float b)
        {
            return a < b && !AlmostEqual(a, b);
        }

        /// <summary>Returns true if x is inside the range of [min .. max]</summary>
        public static bool InRange(this float x, float min, float max)
        {
            return (min - 0.000001f) <= x && x <= (max + 0.000001f);
        }

        public static float Max3(float a, float b, float c) => Max(a, Max(b, c));

        // compute the next highest power of 2 of 32-bit v
        public static int RoundPowerOf2(this int value)
        {
            int v = value; 
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        public static int RoundUpToMultipleOf(this int value, int multipleOf)
        {
            int rem = value % multipleOf;
            if (rem != 0)
                value += multipleOf - rem;
            return value;
        }

        public static int RoundDownToMultipleOf(this int value, int multipleOf)
        {
            int rem = value % multipleOf;
            if (rem != 0)
                return value - rem;
            return value;
        }

        // For example: 75.5f.RoundUpTo(40)  -->  80
        public static int RoundUpTo(this float value, int multipleOf)
        {
            return (int)Ceiling(value / multipleOf) * multipleOf;
        }

        // For example: 75.5f.RoundTo10()  -->  80
        public static int RoundTo10(this float value)
        {
            return (int)Ceiling(value * 0.1f) * 10;
        }

        // For example: 75.5f.RoundDownTo(40)  -->  40
        public static int RoundDownTo(this float value, int multipleOf)
        {
            return (int)Floor(value / multipleOf) * multipleOf;
        }

        // For example: 75.5f.RoundTo10()  -->  70
        public static int RoundDownTo10(this float value)
        {
            return (int)Floor(value * 0.1f) * 10;
        }

        public static Vector2 RoundTo10(this Vector2 v)
        {
            return new Vector2(v.X.RoundTo10(), v.Y.RoundTo10());
        }

        public static float RoundToFractionOf10(this float value)
        {
            return (float)Round(value, 1);
        }

        public static float RoundToFractionOf100(this float value)
        {
            return (float)Round(value, 2);
        }
    }
}
