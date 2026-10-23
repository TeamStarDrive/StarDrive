using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    // a platform, station or orbital shipyard scrapped where it stands: it stops at once, comes apart over its turns, and once it is gone its refund goes into the planet it orbits when that planet is its empire's, and otherwise leaves in a builder ship for its colonies
    [StarDataType]
    public class DismantleOrbital : Goal
    {
        public const float SlotsPerTurn = 50f;
        public const int MinTurns = 3;
        public const int MaxTurns = 20;
        public const float RefundShare = 0.5f;

        [StarData] public sealed override Ship OldShip { get; set; }
        [StarData] int TurnsLeft;

        [StarDataConstructor]
        public DismantleOrbital(Empire owner) : base(GoalType.DismantleOrbital, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                Start,
                Dismantle
            };
        }

        public DismantleOrbital(Ship orbital, Empire owner) : this(owner)
        {
            OldShip = orbital;
        }

        // what Scrap takes apart where it stands; a subspace projector vanishes at once, and a salvage platform comes apart with its work
        public static bool DismantledInPlace(Ship ship)
            => (ship.IsPlatformOrStation || ship.IsShipyard) && !ship.IsSubspaceProjector && !ship.IsSalvagePlatform;

        // 1 turn per 50 slots, from 3 to 20, whatever the pace
        public static int Turns(Ship orbital) => ((int)Math.Ceiling(orbital.SurfaceArea / SlotsPerTurn)).Clamped(MinTurns, MaxTurns);

        // Scrap again brings it back while its place is still free for it: its planet its empire's or no one's, with room among its orbitals, and a mining station's ops still its empire's; a salvage station's work ended with its orders
        public static bool CanCallOff(Ship orbital)
        {
            if (orbital.IsSalvageStation || orbital.IsMiningStation && orbital.GetTether() is { IsMineable: true } mined && mined.Mining.Owner != orbital.Loyalty)
                return false;
            Planet planet = orbital.GetTether();
            return planet == null || (planet.Owner == null || planet.Owner == orbital.Loyalty) && !planet.IsOutOfOrbitalsLimit(orbital.ShipData);
        }

        // on the sim thread: the Scrap button again calls it off where it can be, the orbital whole and at work again
        public static void CallOff(Ship orbital)
        {
            if (CanCallOff(orbital) && orbital.Loyalty.AI.FindGoal(g => g is DismantleOrbital && g.OldShip == orbital) is { } goal)
                orbital.Loyalty.AI.RemoveGoal(goal);
        }

        // the empire's orbitals coming apart at the planet keep their places there
        public static bool AnyAt(Empire empire, Planet planet, Vector2 position, float radius)
        {
            var goals = empire.AI.Goals;
            for (int i = 0; i < goals.Count; ++i)
                if (goals[i] is DismantleOrbital { OldShip: { Active: true, Dismantling: true } orbital }
                    && orbital.GetTether() == planet && position.InRadius(orbital.Position, radius))
                    return true;
            return false;
        }

        // called off, or its empire's goals cleared, the orbital this took apart is whole and at work again; its change of hands makes one in other hands whole
        public override void OnRemoved()
        {
            if (Step > 0 && OldShip is { Active: true, Dismantling: true } orbital && orbital.Loyalty == Owner)
            {
                orbital.StopDismantling();
                orbital.AI.ClearOrders();
            }
        }

        GoalStep Start()
        {
            if (OldShip is not { Active: true, Dying: false } orbital || orbital.Loyalty != Owner
                || orbital.Dismantling || orbital.ScuttleTimer >= 0f || !DismantledInPlace(orbital))
                return GoalStep.GoalFailed;

            TurnsLeft = Turns(orbital);
            orbital.AI.ClearOrders(AIState.Scrap, priority: true);
            orbital.StartDismantling(TurnsLeft);
            return GoalStep.GoToNextStep;
        }

        // once its turns are over it is gone, its troops leaving for a planet first; one dying or counting down to its scuttling gives nothing
        GoalStep Dismantle()
        {
            if (OldShip is not { Active: true, Dismantling: true } orbital || orbital.Loyalty != Owner)
                return GoalStep.GoalFailed;
            if (orbital.Dying || orbital.ScuttleTimer >= 0f)
                return GoalStep.TryAgain;
            if (--TurnsLeft > 0)
                return GoalStep.TryAgain;

            float refund = Refund(orbital);
            if (orbital.GetTether() is { } planet && planet.Owner == Owner)
                Deposit(Owner, planet, refund);
            else
                DeliverRefund.Launch(Owner, orbital.Position, refund);
            orbital.DisengageExcessTroops(orbital.TroopCount);
            orbital.FinishDismantling();
            orbital.OnDismantled();
            return GoalStep.GoalComplete;
        }

        // half of what it costs to build, a salvage station all of the cost its order stored as it was built, less for its damage, and the production aboard in full
        float Refund(Ship orbital)
        {
            float cost = orbital.GetCost(Owner) * RefundShare;
            if (orbital.IsSalvageStation)
                cost = Owner.AI.FindGoal(g => g is SalvageOps && g.TargetShip == orbital) is SalvageOps { StationCost: > 0f } order
                     ? order.StationCost : orbital.GetCost(Owner);
            return cost * orbital.HealthPercent + orbital.GetProduction();
        }

        public static float Room(Planet planet) => (planet.Storage.Max - planet.ProdHere).LowerBound(0f);

        // as much as its store has room for, the rest paid with the turn's income at the empire's tax rate
        public static void Deposit(Empire empire, Planet planet, float refund)
        {
            float taken = refund.UpperBound(Room(planet));
            planet.ProdHere += taken;
            empire.AddExcessCargoMoney((refund - taken) * empire.data.TaxRate);
        }
    }
}
