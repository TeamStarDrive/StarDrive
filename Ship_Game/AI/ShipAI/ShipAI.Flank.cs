using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.CombatTactics;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        enum FlankRole { HeadOn, Left, Right, Rear }

        // by a guard's place in id order among its empire's ships around it
        static readonly FlankRole[] FlankRoles = { FlankRole.HeadOn, FlankRole.Left, FlankRole.Right, FlankRole.HeadOn, FlankRole.Rear };

        const float FlankArrivalRadius = 1500f;

        // an AI guard's warp exit points on the enemy's side or rear, set as it enters combat and on each new target
        internal bool IsFlanking { get; private set; }
        internal bool HasFlankWaypoint { get; private set; }
        internal Vector2 FlankWaypoint { get; private set; }
        internal Vector2 FlankPoint { get; private set; }
        bool HasFlankGroup;
        Vector2 FlankGroupCenter;

        static bool CanFlank(Ship ship) => ship.Fleet == null && !ship.IsHangarShip && !ship.Stats.IsStationary
                                           && (ship.Weapons.Count > 0 || ship.Carrier.HasActiveHangars);

        // an enemy ship's inhibitor holds this ship out of warp, so it cannot go round anyone
        internal bool HeldByEnemyInhibitor => Owner.Inhibited && Owner.InhibitionSource == Ship.InhibitionType.EnemyShip;

        // the field round an enemy ship in which its inhibitor would hold this guard out of warp
        float InhibitorFieldOf(Ship enemy)
        {
            return enemy.Loyalty.WillInhibit(Owner.Loyalty) ? FlankGeometry.InhibitorField(0f, enemy.InhibitionRadius) : 0f;
        }

        FlankRole FlankRoleAmongFriends()
        {
            int place = 0;
            foreach (Ship friend in FriendliesNearby)
                if (friend.Active && friend.Id < Owner.Id && CanFlank(friend))
                    ++place;
            return FlankRoles[place % FlankRoles.Length];
        }

        // a gravity well holds all but the Remnants out of warp, and a star's radiation hurts all but the immune
        internal bool IsBadFlankExit(Vector2 position, SolarSystem system)
        {
            if (!Owner.IsImmuneToStarRadiation && !system.InSafeDistanceFromRadiation(position))
                return true;
            Empire us = Owner.Loyalty;
            return !us.WeAreRemnants
                && system.IdentifyGravityWell(us, position, Owner.Universe.Influence.GetInfluenceStatus(us, position) == InfluenceStatus.Friendly) != null;
        }

        FlankGeometry.FlankSearch FlankSearchFor(Vector2 center, Array<FlankGeometry.Keepout> enemies, float range, SolarSystem system)
        {
            return new(Owner.Position, center, enemies, range, at => IsBadFlankExit(at, system));
        }

        // a new target in combat drops the flank under way for one around it, unless it is in the group the flank was planned around
        void PlanFlankForNewTarget()
        {
            if (Owner.InCombat && !(HasFlankGroup && Target != null && Target.Position.InRadius(FlankGroupCenter, ThreatMatrix.RivalClusterJoinRadius)))
                PlanFlank();
        }

        // sides from Hard, the rear from Brutal; the rest come in head-on
        internal void PlanFlank()
        {
            IsFlanking = false;
            HasFlankWaypoint = false;
            HasFlankGroup = false;
            GameDifficulty difficulty = Owner.Universe.P.Difficulty;
            if (!FlankGeometry.Flanks(Owner.Loyalty) || difficulty == GameDifficulty.Normal || Target is not { } target || !CanFlank(Owner)
                || HeldByEnemyInhibitor)
                return;

            UpdateChaseLimits();
            if (GuardedSystem is not { } system)
                return;

            FlankRole role = FlankRoleAmongFriends();
            if (role == FlankRole.HeadOn || role == FlankRole.Rear && difficulty < GameDifficulty.Brutal)
                return;

            float range = Math.Max(FlankGeometry.WarpExitRange, Owner.DesiredCombatRange);
            if (target.Position.InRadius(Owner.Position, range))
                return;

            // fighters are left out
            var enemies = new Array<FlankGeometry.Keepout>();
            var group = new Array<Ship>();
            foreach (Ship enemy in PotentialTargets)
            {
                if (!enemy.Active || enemy.Dying || enemy.IsHangarShip || enemy.IsInWarp)
                    continue;
                enemies.Add(new(enemy.Position, 0f, InhibitorFieldOf(enemy)));
                if (enemy.Position.InRadius(target.Position, ThreatMatrix.RivalClusterJoinRadius))
                    group.Add(enemy);
            }
            if (!FlankGeometry.TryGetGroupFront(group, Owner.Position, out Vector2 center, out Vector2 front))
                return;

            Vector2 left = front.LeftVector();
            // the rear comes round the side nearer the ship
            bool onRight = role == FlankRole.Right || (role == FlankRole.Rear && (Owner.Position - center).Dot(left) < 0f);
            Vector2 side = onRight ? -left : left;
            FlankGeometry.FlankSearch search = FlankSearchFor(center, enemies, range, system);
            Vector2 waypoint = default;
            Vector2 point;
            bool straight = true;
            if (!(role == FlankRole.Rear ? search.TryRear(side, -front, out waypoint, out point, out straight) : search.TrySide(side, out point)))
                return;

            IsFlanking = true;
            HasFlankWaypoint = !straight;
            FlankWaypoint = role == FlankRole.Rear ? waypoint : point;
            FlankPoint = point;
            HasFlankGroup = true;
            FlankGroupCenter = center;
        }

        // flown until the guard reaches its point, the waypoint first; held out of warp, it closes in instead
        bool FlyToFlank(FixedSimTime timeStep)
        {
            if (!IsFlanking)
                return false;

            if (HeldByEnemyInhibitor)
            {
                IsFlanking = false;
                return false;
            }

            if (HasFlankWaypoint && Owner.Position.InRadius(FlankWaypoint, FlankArrivalRadius))
                HasFlankWaypoint = false;

            if (!HasFlankWaypoint && Owner.Position.InRadius(FlankPoint, FlankArrivalRadius))
            {
                IsFlanking = false;
                return false;
            }

            ThrustOrWarpToPos(HasFlankWaypoint ? FlankWaypoint : FlankPoint, timeStep);
            return true;
        }

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

            if (round.Plan.Dropped || round.Plan.Fleet != Owner.Fleet)
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
            if (Owner.engineState == Ship.MoveState.Warp)
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
