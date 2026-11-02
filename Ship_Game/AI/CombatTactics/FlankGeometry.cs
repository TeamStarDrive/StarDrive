using System;
using System.Collections.Generic;
using SDGraphics;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.CombatTactics;

// points for coming at an enemy group from its sides or rear, leaving warp no closer than WarpExitRange to any of its ships
public static class FlankGeometry
{
    public const float WarpExitRange = 7500f;

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

    // how far out from the center along the direction the nearest point is that keeps the range from every ship
    public static float ClearDistance(Vector2 center, Vector2 direction, IReadOnlyList<Ship> ships, float range)
    {
        float distance = 0f;
        for (bool pushed = true; pushed;)
        {
            pushed = false;
            for (int i = 0; i < ships.Count; ++i)
            {
                Vector2 offset = center - ships[i].Position;
                float along = offset.Dot(direction);
                float root = along*along - offset.SqLen() + range*range;
                if (root <= 0f)
                    continue;
                float half = (float)Math.Sqrt(root);
                if (-along - half < distance && distance < half - along) // inside this ship's range on the way out
                {
                    distance = half - along;
                    pushed = true;
                }
            }
        }
        return distance;
    }

    // whether a straight leg passes no closer than the range to any ship
    public static bool IsLegClear(Vector2 from, Vector2 to, IReadOnlyList<Ship> ships, float range)
    {
        for (int i = 0; i < ships.Count; ++i)
        {
            Vector2 pos = ships[i].Position;
            Vector2 closest = from.SqDist(to) > 0f ? pos.FindClosestPointOnLine(from, to) : from;
            if (closest.SqDist(pos) < range*range)
                return false;
        }
        return true;
    }
}
