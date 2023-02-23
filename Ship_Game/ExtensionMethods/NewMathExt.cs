using System;
using System.Diagnostics.Contracts;
using SDGraphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;


namespace Ship_Game.ExtensionMethods
{
    public static class NewMathExt
    {
        public static Vector2 GenerateRandomPointOnCircle(this Vector2 center, float radius)
        {
            float randomAngle = RandomMath.Float(0f, 360f);
            return center.PointFromAngle(randomAngle, radius);
        }

        public static Vector2 GenerateRandomPointInsideCircle(this Vector2 center, float radius)
        {
            float randomRadius = RandomMath.Float(0f, radius);
            return center.GenerateRandomPointOnCircle(randomRadius);
        }

        // Gives a random point from a vector within a specified distance
        public static Vector2 RandomOffsetAndDistance(Vector2 center, float distance)
        {
            return center + RandomMath.RandomDirection() * RandomMath.Float(0, distance);
        }

        [Pure] public static float Generate(this Range r)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return r.Min == r.Max ? r.Min : RandomMath.Float(r.Min, r.Max);
        }

        public static Vector2 FindStrafeVectorFromTarget(this GameObject ship, float distance, Vector2 direction)
        {
            Vector2 strafe = ship.Rotation.RadiansToDirection() + direction;
            strafe = strafe.Normalized();
            return ship.Position + (strafe * distance);
        }

        public static Vector2 PredictImpact(this Ship ourShip, GameObject target)
        {
            return new ImpactPredictor(ourShip, target)
                .Predict(ourShip.CanUseAdvancedTargeting);
        }

        public static Vector2 PredictImpact(this Projectile proj, GameObject target)
        {
            return new ImpactPredictor(proj, target)
                .Predict(proj.Weapon.CanUseAdvancedTargeting);
        }

        public static float CenterTextX(this Rectangle r, in LocalizedText text)
            => CenterTextX(r, text, Fonts.Arial12Bold);

        public static float CenterTextX(this Rectangle r, in LocalizedText text, Graphics.Font font)
            => r.X + r.Width*0.5f - font.TextWidth(text)*0.5f;
    }
}
