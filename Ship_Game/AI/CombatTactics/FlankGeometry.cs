using System;
using System.Collections.Generic;
using SDGraphics;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.CombatTactics;

// points for coming at an enemy group from its sides or rear, leaving warp no closer than WarpExitRange to any enemy
public static class FlankGeometry
{
    public const float WarpExitRange = 7500f;

    // how far outside an enemy's inhibitor field a flanker leaves warp
    public const float InhibitorFieldMargin = 1000f;

    // an enemy to keep a range from: a ship, or a known enemy group with its spread, and the field its inhibitors hold ships out of warp in
    public readonly record struct Keepout(Vector2 Center, float Radius, float Field = 0f)
    {
        // a flanker keeps the range from the edge, and stays out of the field so its warp is not cut short
        public float Reach(float range) => Math.Max(Radius + range, Field);
    }

    // the field round an enemy of the given spread whose inhibitors reach the given radius, none for no inhibitor
    public static float InhibitorField(float spread, float inhibitionRadius)
    {
        return inhibitionRadius > 0f ? spread + inhibitionRadius + InhibitorFieldMargin : 0f;
    }

    // the Remnants and the AI races; the player and the other factions come in head-on
    public static bool Flanks(Empire empire) => empire.WeAreRemnants || !empire.isPlayer && !empire.IsFaction;

    // the group's average position, and its front: the way most of its ships face, else toward the given point; none for no ships
    public static bool TryGetGroupFront(IReadOnlyList<Ship> group, Vector2 from, out Vector2 center, out Vector2 front)
    {
        center = from;
        front = Vector2.Zero;
        if (group.Count == 0)
            return false;

        Vector2 positions = Vector2.Zero;
        Vector2 facings = Vector2.Zero;
        for (int i = 0; i < group.Count; ++i)
        {
            positions += group[i].Position;
            facings += group[i].Direction;
        }
        center = positions / group.Count;
        front = facings.Length() >= group.Count * 0.5f ? facings.Normalized() : center.DirectionToTarget(from);
        return true;
    }

    // how far out from the center along the direction the nearest point is that keeps the range from every keepout
    public static float ClearDistance(Vector2 center, Vector2 direction, IReadOnlyList<Keepout> keepouts, float range)
    {
        float distance = 0f;
        for (bool pushed = true; pushed;)
        {
            pushed = false;
            for (int i = 0; i < keepouts.Count; ++i)
            {
                Keepout keepout = keepouts[i];
                float reach = keepout.Reach(range);
                Vector2 offset = center - keepout.Center;
                float along = offset.Dot(direction);
                float root = along*along - offset.SqLen() + reach*reach;
                if (root <= 0f)
                    continue;
                float half = (float)Math.Sqrt(root);
                if (-along - half < distance && distance < half - along) // inside this keepout's reach on the way out
                {
                    distance = half - along;
                    pushed = true;
                }
            }
        }
        return distance;
    }

    // where a flanker can go round an enemy group: out to twice the clear distance until the way there keeps the range too and the exit is good
    public readonly struct FlankSearch
    {
        readonly Vector2 From;
        readonly Vector2 Center;
        readonly IReadOnlyList<Keepout> Keepouts;
        readonly float Range;
        readonly Func<Vector2, Vector2, Vector2> Place; // the point a flanker takes for one found out along a direction
        readonly Func<Vector2, bool> IsBadExit;

        public FlankSearch(Vector2 from, Vector2 center, IReadOnlyList<Keepout> keepouts, float range,
                           Func<Vector2, Vector2, Vector2> place, Func<Vector2, bool> isBadExit)
        {
            From = from;
            Center = center;
            Keepouts = keepouts;
            Range = range;
            Place = place;
            IsBadExit = isBadExit;
        }

        public bool TrySide(Vector2 side, out Vector2 point)
        {
            float clear = ClearDistance(Center, side, Keepouts, Range);
            for (int step = 0; step <= 4; ++step)
            {
                point = Place(Center + side * (clear * (1f + step * 0.25f)), side);
                if (IsLegClear(From, point, Keepouts, Range) && !IsBadExit(point))
                    return true;
            }
            point = default;
            return false;
        }

        // round the side, or straight when that way is clear
        public bool TryRear(Vector2 side, Vector2 rear, out Vector2 waypoint, out Vector2 point, out bool straight)
        {
            float clear = Math.Max(ClearDistance(Center, side, Keepouts, Range), ClearDistance(Center, rear, Keepouts, Range));
            for (int step = 0; step <= 4; ++step)
            {
                float distance = clear * (1f + step * 0.25f);
                waypoint = Center + side * distance;
                point = Place(Center + rear * distance, rear);
                straight = IsLegClear(From, point, Keepouts, Range);
                if (!straight && (!IsLegClear(From, waypoint, Keepouts, Range)
                                  || !IsLegClear(waypoint, point, Keepouts, Range)
                                  || IsBadExit(waypoint)))
                    continue;

                if (!IsBadExit(point))
                    return true;
            }
            waypoint = default;
            point = default;
            straight = false;
            return false;
        }
    }

    // whether a straight leg passes no closer than the range to any keepout
    public static bool IsLegClear(Vector2 from, Vector2 to, IReadOnlyList<Keepout> keepouts, float range)
    {
        for (int i = 0; i < keepouts.Count; ++i)
        {
            Keepout keepout = keepouts[i];
            float reach = keepout.Reach(range);
            Vector2 closest = from.SqDist(to) > 0f ? keepout.Center.FindClosestPointOnLine(from, to) : from;
            if (closest.SqDist(keepout.Center) < reach*reach)
                return false;
        }
        return true;
    }
}
