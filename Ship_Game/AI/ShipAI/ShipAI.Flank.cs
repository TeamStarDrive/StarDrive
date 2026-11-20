using System;
using SDGraphics;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        // a fleet ship's one way round an enemy group to its place on the group's side or rear, set when its fleet sends it
        internal sealed class FleetFlankRound
        {
            public enum Stage { Waiting, Flying, Done }

            public readonly Fleet.FlankPlan Plan;
            public readonly Vector2 Place; // from the group's center toward the ship's place
            public Stage At;
            public Vector2 Center; // the group's center as this ship last saw it
            public float Radius;
            public float Way; // 1 or -1, the way round
            public float AngleLeft;
            public float TimeLeft;

            public FleetFlankRound(Fleet.FlankPlan plan, Vector2 place)
            {
                Plan = plan;
                Place = place;
            }
        }

        internal FleetFlankRound FleetRound;

        // a fleet ship flying its round, or with its round ready to start at a target still by the group it goes round, keeps to it rather than cutting the target off
        bool RoundAheadAt(Ship target) => FleetRound is { Plan: { Over: false } plan } round && plan.Fleet == Owner.Fleet
            && (round.At == FleetFlankRound.Stage.Flying
                || round.At == FleetFlankRound.Stage.Waiting && target.Position.InRadius(plan.Center, ThreatMatrix.RivalClusterJoinRadius));

        // how far ahead on its circle a ship going round a group steers
        const float FlankRoundLead = RadMath.PI / 12f;

        // how much longer than expected a round may take before the ship gives up and fights where it is
        const float FlankRoundPatience = 1.5f;

        // how near its place, along its circle, a ship going round a group is there
        const float FlankRoundArrival = 1500f;

        // a fleet ship sent to its enemy group's side or rear goes round the group once, at top speed and fighting on the way, from where a target of that group first comes in reach to its place; knocked out engines end it
        bool FlyFlankRound(Ship target, bool inReach, FixedSimTime timeStep)
        {
            if (FleetRound is not { } round)
                return false;

            if (round.Plan.Over || round.Plan.Fleet != Owner.Fleet)
            {
                FleetRound = null;
                return false;
            }

            if (round.At == FleetFlankRound.Stage.Waiting && (!inReach || !StartFlankRound(round, target)))
                return false;
            if (round.At != FleetFlankRound.Stage.Flying)
                return false;

            round.TimeLeft -= timeStep.FixedTime;
            if (round.TimeLeft <= 0f || Owner.EnginesKnockedOut || !TryGetEnemyGroupCenter(round.Center, out Vector2 center))
            {
                round.At = FleetFlankRound.Stage.Done;
                return false;
            }

            Vector2 at = (Owner.Position - center).Normalized();
            float angleLeft = AngleRound(at, round.Place, round.Way);
            if (angleLeft * round.Radius <= FlankRoundArrival || angleLeft > round.AngleLeft + RadMath.PI)
            {
                round.At = FleetFlankRound.Stage.Done;
                return false;
            }

            round.Center = center;
            round.AngleLeft = angleLeft;
            float lead = Math.Min(FlankRoundLead, angleLeft);
            Vector2 aim = center + Turned(at, round.Way * lead) * round.Radius;
            Owner.HyperspaceReturn();
            SubLightContinuousMoveInDirection(Owner.Position.DirectionToTarget(aim), timeStep, Owner.MaxSTLSpeed);
            return true;
        }

        // round the planned group once the target is near where the fleet last knew it, at the distance the ship has from its center, the shorter way unless that passes the barred side
        bool StartFlankRound(FleetFlankRound round, Ship target)
        {
            Vector2 planned = round.Plan.Center;
            if (!target.Position.InRadius(planned, ThreatMatrix.RivalClusterJoinRadius) || !TryGetEnemyGroupCenter(planned, out Vector2 center))
                return false;

            round.At = FleetFlankRound.Stage.Done;
            if (Owner.MaxSTLSpeed <= 0f)
                return false;

            Vector2 toShip = Owner.Position - center;
            float radius = toShip.Length();
            if (radius <= 0f)
                return false;

            Vector2 at = toShip / radius;
            Vector2 barred = round.Plan.BarredSide;
            float way = AngleRound(at, round.Place, 1f) <= RadMath.PI ? 1f : -1f;
            if (barred != Vector2.Zero && AngleRound(at, barred, way) < AngleRound(at, round.Place, way))
                way = -way;

            float angleLeft = AngleRound(at, round.Place, way);
            if (angleLeft * radius <= FlankRoundArrival)
                return false;

            round.Center = center;
            round.Radius = radius;
            round.Way = way;
            round.AngleLeft = angleLeft;
            round.TimeLeft = FlankRoundPatience * radius * angleLeft / Owner.MaxSTLSpeed;
            round.At = FleetFlankRound.Stage.Flying;
            return true;
        }

        // the center of the enemy ships this ship sees near the point, fighters and ships at warp left out
        bool TryGetEnemyGroupCenter(Vector2 near, out Vector2 center)
        {
            Vector2 sum = Vector2.Zero;
            int count = 0;
            Ship[] enemies = PotentialTargets;
            for (int i = 0; i < enemies.Length; ++i)
            {
                Ship enemy = enemies[i];
                if (enemy.Active && !enemy.Dying && !enemy.IsHangarShip && !enemy.IsInWarp
                    && enemy.Position.InRadius(near, ThreatMatrix.RivalClusterJoinRadius))
                {
                    sum += enemy.Position;
                    ++count;
                }
            }
            center = count > 0 ? sum / count : near;
            return count > 0;
        }

        // the angle from one direction to another going round the given way, from zero to two pi
        static float AngleRound(Vector2 from, Vector2 to, float way)
        {
            float angle = (float)Math.Atan2(from.X * to.Y - from.Y * to.X, from.Dot(to)) * way;
            return angle >= 0f ? angle : angle + RadMath.TwoPI;
        }

        static Vector2 Turned(Vector2 direction, float radians)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);
            return new Vector2(direction.X * cos - direction.Y * sin, direction.X * sin + direction.Y * cos);
        }
    }
}
