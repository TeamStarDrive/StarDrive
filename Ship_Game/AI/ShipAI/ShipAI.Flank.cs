using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.CombatTactics;
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

        // once a second, a fleet ship on its way round an enemy group gives up the leg when an enemy's inhibitor holds it out of warp
        public void UpdateFlankLegHeldOutOfWarp()
        {
            if (HasPriorityOrder && HeldByEnemyInhibitor && Owner.Fleet is { } fleet)
                fleet.DropFlankLegHeldOutOfWarp(Owner);
        }

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
            return new(Owner.Position, center, enemies, range, (found, _) => found, at => IsBadFlankExit(at, system));
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
    }
}
