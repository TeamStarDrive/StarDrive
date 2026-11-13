using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Fleets
{
    public sealed partial class Fleet
    {
        // the ships sent at an enemy group split when the fleet's free ships have half again its strength
        const float SplitAtStrength = 1.5f;

        readonly record struct FlankLeg(Ship Ship, Vector2 Point);

        // an attack on one enemy group from its sides and rear, kept while the group stays near where it was planned
        sealed class FlankPlan
        {
            public Vector2 Center;
            public Vector2 Toward; // from the fleet to the group, as planned
            public readonly Array<FlankLeg> Legs = new(); // once free again the ships come in straight

            public bool Sent(Ship ship)
            {
                for (int i = 0; i < Legs.Count; ++i)
                    if (Legs[i].Ship == ship)
                        return true;
                return false;
            }
        }

        readonly Array<FlankPlan> FlankPlans = new();
        MilitaryTask FlankPlansTask;

        static bool CanFlank(Ship ship) => ship.Weapons.Count > 0 || ship.Carrier.HasActiveHangars;

        float GroupStrength(ThreatCluster group) => group.Strength * Owner.GetFleetStrEmpireMultiplier(group.Loyalty);

        void StrongestGroupFirst(ThreatCluster[] groups)
        {
            int strongest = 0;
            for (int i = 1; i < groups.Length; ++i)
                if (GroupStrength(groups[i]) > GroupStrength(groups[strongest]))
                    strongest = i;
            ThreatCluster first = groups[strongest];
            for (int i = strongest; i > 0; --i)
                groups[i] = groups[i - 1];
            groups[0] = first;
        }

        static bool IsPlannedAround(FlankPlan plan, ThreatCluster group)
        {
            return group.Position.InRadius(plan.Center, ThreatMatrix.RivalClusterJoinRadius);
        }

        // a new task starts afresh, and a plan goes once no group is near where it was made
        void PruneFlankPlans(MilitaryTask task, ThreatCluster[] groups)
        {
            if (task != FlankPlansTask)
            {
                FlankPlans.Clear();
                FlankPlansTask = task;
            }

            for (int i = FlankPlans.Count - 1; i >= 0; --i)
            {
                FlankPlan plan = FlankPlans[i];
                bool groupNear = false;
                for (int g = 0; g < groups.Length && !groupNear; ++g)
                    groupNear = IsPlannedAround(plan, groups[g]);
                if (!groupNear)
                {
                    FlankPlans.RemoveAt(i);
                    continue;
                }
                for (int l = plan.Legs.Count - 1; l >= 0; --l)
                    if (!plan.Legs[l].Ship.Active)
                        plan.Legs.RemoveAt(l);
            }
        }

        FlankPlan FlankPlanFor(ThreatCluster group, Vector2 toward)
        {
            for (int i = 0; i < FlankPlans.Count; ++i)
                if (IsPlannedAround(FlankPlans[i], group))
                    return FlankPlans[i];

            var plan = new FlankPlan { Center = group.Position, Toward = toward };
            FlankPlans.Add(plan);
            return plan;
        }

        // a ship still locked on its way to the point it was sent to, while it can fly there
        protected override bool KeepsItsMove(Ship ship)
        {
            return !ship.EnginesKnockedOut && PlanOfLegUnderWay(ship) != null;
        }

        // the plan whose leg the ship is still locked on, if any
        FlankPlan PlanOfLegUnderWay(Ship ship)
        {
            if (!ship.AI.HasPriorityOrder)
                return null;
            for (int i = 0; i < FlankPlans.Count; ++i)
            {
                Array<FlankLeg> legs = FlankPlans[i].Legs;
                for (int l = 0; l < legs.Count; ++l)
                    if (legs[l].Ship == ship && ship.AI.MovePosition.InRadius(legs[l].Point, 1f))
                        return FlankPlans[i];
            }
            return null;
        }

        // a ship an enemy's inhibitor holds out of warp on its way round gives up the leg and comes in head-on
        internal void DropFlankLegHeldOutOfWarp(Ship ship)
        {
            if (PlanOfLegUnderWay(ship) is { } plan)
                ship.AI.OrderAttackMoveTo(plan.Center + GetPositionFromDirection(ship, plan.Toward));
        }

        void SendHeadOn(Ship ship, ThreatCluster group, Vector2 facing)
        {
            ship.AI.OrderAttackMoveTo(group.Position + GetPositionFromDirection(ship, facing));
        }

        // Screen, Center and the command ship head-on, Left and Right to the sides; from Brutal the long range ships to the sides and the fastest third of the side ships to the rear; at Insane a side in reach of an enemy planet's guns joins the other
        void SendAtGroup(Array<Ship> sent, ThreatCluster group, ThreatCluster[] groups, bool split)
        {
            Vector2 facing = AveragePosition().DirectionToTarget(group.Position);
            if (!split)
            {
                for (int i = 0; i < sent.Count; ++i)
                    SendHeadOn(sent[i], group, facing);
                return;
            }

            FlankPlan plan = FlankPlanFor(group, facing);
            GameDifficulty difficulty = Owner.Universe.P.Difficulty;
            var left = new Array<Ship>();
            var right = new Array<Ship>();
            var longRange = new Array<Ship>();
            for (int i = 0; i < sent.Count; ++i)
            {
                Ship ship = sent[i];
                if (ship == CommandShip || plan.Sent(ship) || !CanFlank(ship) || ship.AI.HeldByEnemyInhibitor)
                    SendHeadOn(ship, group, facing);
                else if (LeftShips.ContainsRef(ship))
                    left.Add(ship);
                else if (RightShips.ContainsRef(ship))
                    right.Add(ship);
                else if (difficulty >= GameDifficulty.Brutal && CenterShips.ContainsRef(ship) && AverageFleetAttributes.LongRange(ship))
                    longRange.Add(ship);
                else
                    SendHeadOn(ship, group, facing);
            }

            var rear = new Array<Ship>();
            if (difficulty >= GameDifficulty.Brutal)
            {
                var sides = new Array<Ship>(left);
                sides.AddRange(right);
                for (int n = (left.Count + right.Count) / 3; n > 0; --n)
                {
                    Ship fastest = sides.FindMax(s => s.MaxSTLSpeed);
                    sides.RemoveRef(fastest);
                    left.RemoveRef(fastest);
                    right.RemoveRef(fastest);
                    rear.Add(fastest);
                }
            }

            for (int i = 0; i < longRange.Count; ++i)
                (i % 2 == 0 ? left : right).Add(longRange[i]);

            Vector2 leftSide = plan.Toward.LeftVector();
            bool leftBarred = false, rightBarred = false;
            if (difficulty == GameDifficulty.Insane)
            {
                float nearest = Math.Max(group.Radius + FlankGeometry.WarpExitRange, InhibitorField(group));
                bool leftInReach = InReachOfPlanetGuns(group.System, group.Position + leftSide * nearest);
                bool rightInReach = InReachOfPlanetGuns(group.System, group.Position - leftSide * nearest);
                leftBarred = leftInReach && !rightInReach;
                rightBarred = rightInReach && !leftInReach;
                if (leftBarred)
                {
                    right.AddRange(left);
                    left.Clear();
                }
                else if (rightBarred)
                {
                    left.AddRange(right);
                    right.Clear();
                }
            }

            var keepouts = new Array<FlankGeometry.Keepout>(groups.Length);
            for (int i = 0; i < groups.Length; ++i)
                keepouts.Add(new(groups[i].Position, groups[i].Radius, InhibitorField(groups[i])));

            for (int i = 0; i < left.Count; ++i)
                SendToSideOrHeadOn(left[i], plan, group, keepouts, leftSide, longRange.ContainsRef(left[i]), facing);
            for (int i = 0; i < right.Count; ++i)
                SendToSideOrHeadOn(right[i], plan, group, keepouts, -leftSide, longRange.ContainsRef(right[i]), facing);
            for (int i = 0; i < rear.Count; ++i)
                if (!TrySendToRear(rear[i], plan, group, keepouts, leftBarred, rightBarred))
                    SendHeadOn(rear[i], group, facing);
        }

        // the field round a known group in which its inhibitors would hold this fleet's ships out of warp
        float InhibitorField(ThreatCluster group)
        {
            return group.Loyalty.WillInhibit(Owner) ? FlankGeometry.InhibitorField(group.Radius, group.InhibitionRadius) : 0f;
        }

        // how far a flanker keeps from the enemy group's edge
        static float FlankRange(Ship ship, bool longRange)
        {
            return Math.Max(FlankGeometry.WarpExitRange, longRange ? ship.WeaponsMaxRange : ship.DesiredCombatRange);
        }

        // ships of one side spread across the way out by their formation places, never nearer the group
        static Vector2 SpreadByFormation(Vector2 point, Vector2 outward, Ship ship)
        {
            Vector2 offset = ship.RelativeFleetOffset;
            return point + outward.RightVector() * offset.X.Clamped(-ShipAI.MaxGroupSpread, ShipAI.MaxGroupSpread)
                         + outward * Math.Abs(offset.Y).UpperBound(ShipAI.MaxGroupSpread);
        }

        static bool IsBadFlankExit(Ship ship, Vector2 position, SolarSystem system)
        {
            return system != null && ship.AI.IsBadFlankExit(position, system);
        }

        bool InReachOfPlanetGuns(SolarSystem system, Vector2 position)
        {
            if (system == null)
                return false;
            foreach (Planet planet in system.PlanetList)
                if (planet.Owner != null && planet.Owner.IsEmpireAttackable(Owner) && position.InRadius(planet.Position, planet.GunReach()))
                    return true;
            return false;
        }

        // locked until it gets there, so the ship does not turn to fight on the way
        static void OrderFlankLeg(Ship ship, Vector2 point, ThreatCluster group, bool queued)
        {
            MoveOrder order = queued ? MoveOrder.Regular | MoveOrder.AddWayPoint : MoveOrder.Regular;
            ship.AI.OrderMoveTo(point, point.DirectionToTarget(group.Position), order);
        }

        // spread by the ship's place in the formation, and out of gravity wells and radiation that would hold or hurt it
        FlankGeometry.FlankSearch SearchFor(Ship ship, ThreatCluster group, Array<FlankGeometry.Keepout> keepouts, float range)
        {
            SolarSystem system = group.System;
            return new(ship.Position, group.Position, keepouts, range,
                       (found, outward) => SpreadByFormation(found, outward, ship), at => IsBadFlankExit(ship, at, system));
        }

        void SendToSideOrHeadOn(Ship ship, FlankPlan plan, ThreatCluster group, Array<FlankGeometry.Keepout> keepouts,
                                Vector2 side, bool longRange, Vector2 facing)
        {
            if (SearchFor(ship, group, keepouts, FlankRange(ship, longRange)).TrySide(side, out Vector2 point))
            {
                OrderFlankLeg(ship, point, group, queued: false);
                plan.Legs.Add(new(ship, point));
            }
            else
            {
                SendHeadOn(ship, group, facing);
            }
        }

        // round the side nearer the ship unless the guns bar it
        bool TrySendToRear(Ship ship, FlankPlan plan, ThreatCluster group, Array<FlankGeometry.Keepout> keepouts,
                           bool leftBarred, bool rightBarred)
        {
            Vector2 left = plan.Toward.LeftVector();
            bool onLeft = (ship.Position - group.Position).Dot(left) >= 0f ? !leftBarred : rightBarred;
            if (!SearchFor(ship, group, keepouts, FlankRange(ship, longRange: false))
                    .TryRear(onLeft ? left : -left, plan.Toward, out Vector2 waypoint, out Vector2 point, out bool straight))
                return false;

            if (!straight)
                OrderFlankLeg(ship, waypoint, group, queued: false);
            OrderFlankLeg(ship, point, group, queued: !straight);
            plan.Legs.Add(new(ship, point));
            return true;
        }
    }
}
