using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    // the refund of an orbital taken apart in deep space or at a planet not its empire's, which a builder ship the player cannot order carries to its colonies: it stops near each and sends down what fits, until all of it fits at one or it comes to its last, where it lands and unloads all
    [StarDataType]
    public class DeliverRefund : Goal
    {
        public const int MaxStops = 10;
        public const int ShortList = 5;
        public const float StopDistance = 200f; // from the planet's edge
        public const float ArrivalMargin = 100f; // the planet moves on while the builder waits for the turn

        [StarData] public sealed override Ship OldShip { get; set; } // the builder
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; } // the colony it heads for
        [StarData] public float Load { get; private set; }
        [StarData] public bool Lands { get; private set; }
        [StarData] int Stops;
        [StarData] bool NoRoomLeft;

        Ship Builder => OldShip;

        [StarDataConstructor]
        public DeliverRefund(Empire owner) : base(GoalType.DeliverRefund, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                Deliver,
                Unload
            };
        }

        DeliverRefund(Empire owner, float load) : this(owner)
        {
            Load = load;
        }

        public override Vector2 MovePosition => PlanetBuildingAt?.Position ?? Vector2.Zero;

        // at most 10 stops, or one for each of its empire's colonies when it has fewer
        bool LastStop => NoRoomLeft || Stops >= Math.Min(MaxStops, Owner.NumPlanets);

        // where an orbital came apart, its refund rises in a builder from where it stood, facing its first colony; with no safe colony or no builder design it is paid at once as credits at the tax rate
        public static void Launch(Empire owner, Vector2 from, float refund)
        {
            var delivery = new DeliverRefund(owner, refund) { Stops = 1 };
            string design = owner.GetSupplyShuttleName();
            if (!delivery.PickColony(from) || !ResourceManager.GetShipTemplate(design, out _)
                || Ship.CreateShipAtPoint(owner.Universe, design, owner, from) is not { } builder)
            {
                owner.AddExcessCargoMoney(refund * owner.data.TaxRate);
                return;
            }

            builder.InitLaunch(LaunchPlan.Shipyard, from.AngleToTarget(delivery.PlanetBuildingAt.Position));
            delivery.OldShip = builder;
            delivery.Order(builder);
            owner.AI.AddGoal(delivery);
        }

        // a delivery its empire's goals no longer hold, as one absorbed or fallen loses them, takes its builder and load away
        public override void OnRemoved()
        {
            if (Builder is { Active: true, Dying: false } builder && builder.Loyalty == Owner)
                builder.QueueTotalRemoval();
        }

        // among the five safe colonies with the most room, the nearest, one it stopped at too once its store has room again; with none with room, the nearest safe colony, where it lands; false with no safe colony
        bool PickColony(Vector2 from)
        {
            PlanetBuildingAt = null;
            Planet[] safe = Owner.SafePlanets;
            if (safe.Length == 0)
                return false;

            Goal[] coming = Owner.AI.FindGoals(g => g.Type is GoalType.ScrapShip or GoalType.DeliverRefund);
            Planet[] open = safe.Filter(p => p.MissingProdHereForScrap(coming) > 0f);
            NoRoomLeft = open.Length == 0;
            Planet[] candidates = NoRoomLeft ? safe : open.SortedDescending(p => p.MissingProdHereForScrap(coming)).TakeItems(ShortList);
            PlanetBuildingAt = candidates.FindMin(p => p.Position.SqDist(from));
            return true;
        }

        // all of it fits there, or it is the last stop
        void UpdateLands() => Lands = LastStop || Load <= DismantleOrbital.Room(PlanetBuildingAt);

        void Order(Ship builder)
        {
            UpdateLands();
            builder.AI.OrderDeliverRefund(PlanetBuildingAt, this);
        }

        bool OnOrder(Ship builder)
            => builder.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal order) && order.Plan == ShipAI.Plan.DeliverRefund
               && order.Goal == this && order.TargetPlanet == PlanetBuildingAt;

        // destroyed or boarded, its load is lost; a colony lost or turned unsafe on the way is swapped for another without counting a stop
        GoalStep Deliver()
        {
            if (Builder is not { Active: true, Dying: false } builder || builder.Loyalty != Owner)
                return GoalStep.GoalFailed;
            if (builder.LoyaltyTracker.ChangesFrom(Owner)) // boarded, it changes hands at its next update
                return GoalStep.TryAgain;
            if (builder.IsLanding)
                return GoalStep.GoToNextStep;

            Planet colony = PlanetBuildingAt;
            if (colony?.Owner != Owner || !colony.Safe)
            {
                if (!PickColony(builder.Position))
                    return PayAsCredits(builder);
                Order(builder);
                return GoalStep.TryAgain;
            }

            UpdateLands();
            if (!Lands && builder.Position.InRadius(colony.Position, colony.Radius + StopDistance + ArrivalMargin))
            {
                SendDown(builder, colony);
                ++Stops;
                if (!PickColony(builder.Position))
                    return PayAsCredits(builder);
                Order(builder);
            }
            else if (!OnOrder(builder))
            {
                Order(builder);
            }
            return GoalStep.TryAgain;
        }

        // what the store has room for goes down in shuttles, untaxed
        void SendDown(Ship builder, Planet colony)
        {
            float taken = Load.UpperBound(DismantleOrbital.Room(colony));
            colony.ProdHere += taken;
            Load -= taken;
            builder.SendRefundShuttlesDown(colony, taken);
        }

        // with no safe colony left, what it carries is paid as credits at the tax rate, and the builder leaves
        GoalStep PayAsCredits(Ship builder)
        {
            Owner.AddExcessCargoMoney(Load * Owner.data.TaxRate);
            Load = 0f;
            builder.QueueTotalRemoval();
            return GoalStep.GoalComplete;
        }

        // landed, it unloads all, the excess paid as credits at the tax rate, and all of it on a colony lost during the landing
        GoalStep Unload()
        {
            if (Builder is not { Active: true } builder || builder.Loyalty != Owner)
                return GoalStep.GoalFailed;
            if (builder.LandShip is { Done: false })
                return GoalStep.TryAgain;

            Planet colony = builder.LandShip?.Planet ?? PlanetBuildingAt;
            if (colony?.Owner == Owner)
                DismantleOrbital.Deposit(Owner, colony, Load);
            else
                Owner.AddExcessCargoMoney(Load * Owner.data.TaxRate);
            Load = 0f;
            builder.QueueTotalRemoval();
            return GoalStep.GoalComplete;
        }
    }
}
