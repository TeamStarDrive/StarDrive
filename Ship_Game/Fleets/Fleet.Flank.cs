using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Fleets
{
    public sealed partial class Fleet
    {
        // the ships sent at an enemy group split when the fleet's free ships have half again its strength
        const float SplitAtStrength = 1.5f;

        // how far past a group's edge the planet guns on each of its sides are looked for
        const float SideReach = 7500f;

        // the sides and rear of one enemy group, kept while the group stays near where it was last known
        internal sealed class FlankPlan
        {
            public readonly Fleet Fleet;
            public Vector2 Center; // where the group was last known
            public readonly Vector2 Toward; // from the fleet to the group, as planned
            public readonly Vector2 BarredSide; // a side in reach of an enemy planet's guns, none for zero
            public bool Dropped;

            public FlankPlan(Fleet fleet, Vector2 center, Vector2 toward, Vector2 barredSide)
            {
                Fleet = fleet;
                Center = center;
                Toward = toward;
                BarredSide = barredSide;
            }

            public bool Sent(Ship ship) => ship.AI.FleetRound?.Plan == this;
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

        void DropFlankPlans()
        {
            for (int i = 0; i < FlankPlans.Count; ++i)
                FlankPlans[i].Dropped = true;
            FlankPlans.Clear();
        }

        // a new task starts afresh, a plan follows the nearest group still near it, and goes once none is
        void PruneFlankPlans(MilitaryTask task, ThreatCluster[] groups)
        {
            if (task != FlankPlansTask)
            {
                DropFlankPlans();
                FlankPlansTask = task;
            }

            for (int i = FlankPlans.Count - 1; i >= 0; --i)
            {
                FlankPlan plan = FlankPlans[i];
                ThreatCluster nearest = null;
                for (int g = 0; g < groups.Length; ++g)
                    if (IsPlannedAround(plan, groups[g])
                        && (nearest == null || groups[g].Position.SqDist(plan.Center) < nearest.Position.SqDist(plan.Center)))
                        nearest = groups[g];

                if (nearest != null)
                {
                    plan.Center = nearest.Position;
                }
                else
                {
                    plan.Dropped = true;
                    FlankPlans.RemoveAt(i);
                }
            }
        }

        FlankPlan FlankPlanFor(ThreatCluster group, Vector2 toward)
        {
            for (int i = 0; i < FlankPlans.Count; ++i)
                if (IsPlannedAround(FlankPlans[i], group))
                    return FlankPlans[i];

            var plan = new FlankPlan(this, group.Position, toward, BarredSide(group, toward));
            FlankPlans.Add(plan);
            return plan;
        }

        // at Insane, the one side of the group an enemy planet's guns reach; none when they reach both or neither
        Vector2 BarredSide(ThreatCluster group, Vector2 toward)
        {
            if (Owner.Universe.P.Difficulty != GameDifficulty.Insane)
                return Vector2.Zero;

            Vector2 left = toward.LeftVector();
            float reach = group.Radius + SideReach;
            bool leftInReach = InReachOfPlanetGuns(group.System, group.Position + left * reach);
            bool rightInReach = InReachOfPlanetGuns(group.System, group.Position - left * reach);
            if (leftInReach == rightInReach)
                return Vector2.Zero;
            return leftInReach ? left : -left;
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

        void SendHeadOn(Ship ship, ThreatCluster group, Vector2 facing)
        {
            ship.AI.OrderAttackMoveTo(group.Position + GetPositionFromDirection(ship, facing));
        }

        // all come in head-on, keeping only a round planned around this group, and when they split each side ship goes round to its place once its target is in reach
        void SendAtGroup(Array<Ship> sent, ThreatCluster group, bool split)
        {
            Vector2 facing = AveragePosition().DirectionToTarget(group.Position);
            for (int i = 0; i < sent.Count; ++i)
            {
                Ship ship = sent[i];
                if (ship.AI.FleetRound is { } round && !IsPlannedAround(round.Plan, group))
                    ship.AI.FleetRound = null;
                SendHeadOn(ship, group, facing);
            }
            if (split)
                PlanRounds(sent, group, facing);
        }

        // Left and Right ships to the side nearer them, from Brutal with the Center's long range ships and the fastest third of the Left and Right ships to the rear; Screen, Center and the command ship stay head-on, and at Insane a side in reach of an enemy planet's guns sends its ships to the other
        void PlanRounds(Array<Ship> sent, ThreatCluster group, Vector2 facing)
        {
            FlankPlan plan = FlankPlanFor(group, facing);
            GameDifficulty difficulty = Owner.Universe.P.Difficulty;
            var sides = new Array<Ship>();
            var longRange = new Array<Ship>();
            for (int i = 0; i < sent.Count; ++i)
            {
                Ship ship = sent[i];
                if (ship == CommandShip || plan.Sent(ship) || !CanFlank(ship))
                    continue;
                if (LeftShips.ContainsRef(ship) || RightShips.ContainsRef(ship))
                    sides.Add(ship);
                else if (difficulty >= GameDifficulty.Brutal && CenterShips.ContainsRef(ship) && AverageFleetAttributes.LongRange(ship))
                    longRange.Add(ship);
            }

            if (difficulty >= GameDifficulty.Brutal)
            {
                for (int n = sides.Count / 3; n > 0; --n)
                {
                    Ship fastest = sides.FindMax(s => s.MaxSTLSpeed);
                    sides.RemoveRef(fastest);
                    SendRound(plan, fastest, plan.Toward);
                }
            }

            sides.AddRange(longRange);
            Vector2 left = plan.Toward.LeftVector();
            for (int i = 0; i < sides.Count; ++i)
            {
                Vector2 side = (sides[i].Position - group.Position).Dot(left) >= 0f ? left : -left;
                if (side.Dot(plan.BarredSide) > 0f)
                    side = -side;
                SendRound(plan, sides[i], side);
            }
        }

        static void SendRound(FlankPlan plan, Ship ship, Vector2 place)
        {
            ship.AI.FleetRound = new ShipAI.FleetFlankRound(plan, place);
        }
    }
}
