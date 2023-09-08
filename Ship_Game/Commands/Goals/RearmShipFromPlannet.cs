using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    class RearmShipFromPlanet : Goal
    {
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public sealed override Ship TargetShip { get; set; }

        [StarDataConstructor]
        public RearmShipFromPlanet(Empire owner) : base(GoalType.RearmShipFromPlanet, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                LaunchSupplyShip,
                TransferOrdnance,
                LandBackOnPlanet
            };
        }

        public RearmShipFromPlanet(Ship shipToRearm, Planet planet, Empire owner) : this(owner)
        {
            TargetShip = shipToRearm;
            PlanetBuildingAt = planet;
        }

        public RearmShipFromPlanet(Ship shipToRearm, Ship existingSupplyShip, Planet planet, Empire owner)
            : this(owner)
        {
            TargetShip = shipToRearm;
            SupplyShip = existingSupplyShip;
            PlanetBuildingAt = planet;
        }

        GoalStep LaunchSupplyShip()
        {
            // If not null then a new goal was assigned for a an existing supply ship with ordnance left
            if (SupplyShip == null) 
            {
                string supplyShipName = Owner.GetSupplyShuttleName();
                SupplyShip = Ship.CreateShipNearPlanet(TargetShip.Universe, supplyShipName, Owner, PlanetBuildingAt, doOrbit: false);

                if (SupplyShip == null)
                    return GoalStep.GoalFailed;
            }

            SupplyShip.AI.AddSupplyShipGoal(TargetShip, ShipAI.Plan.RearmShipFromPlanet);
            TargetShip.Supply.ChangeIncomingOrdnance(SupplyShip.Ordinance);
            return GoalStep.GoToNextStep;
        }

        GoalStep TransferOrdnance()
        {
            if (!SupplyAlive)
                return GoalStep.GoalFailed;

            if (!TargetValid)
            {
                if (DivertSupplyShip())
                    return GoalStep.GoalComplete;

                SupplyShip.AI.OrderSupplyShipLand(PlanetBuildingAt);
                return GoalStep.GoToNextStep;
            }

            if (SupplyShip.AI.State != AIState.Ferrying) // Avoid  player control
            {
                SupplyShip.AI.AddSupplyShipGoal(TargetShip, ShipAI.Plan.RearmShipFromPlanet);
                return GoalStep.GoalFailed;
            }

            if (SupplyShip.Position.InRadius(TargetShip.Position, TargetShip.Radius + 500f))
            {
                TargetShip.Supply.ChangeIncomingOrdnance(-SupplyShip.Ordinance);
                float leftOverOrdnance  = TargetShip.ChangeOrdnance(FinishedShip.Ordinance);
                float ordnanceDelivered = SupplyShip.Ordinance - leftOverOrdnance;
                SupplyShip.ChangeOrdnance(-ordnanceDelivered);

                if (DivertSupplyShip())
                    return GoalStep.GoalComplete;

                SupplyShip.AI.OrderSupplyShipLand(PlanetBuildingAt);
                return GoalStep.GoToNextStep;
            }

            return GoalStep.TryAgain;
        }

        GoalStep LandBackOnPlanet()
        {
            if (!SupplyAlive)
                return GoalStep.GoalFailed;

            if (SupplyShip.AI.State != AIState.SupplyReturnHome)
                SupplyShip.AI.OrderSupplyShipLand(PlanetBuildingAt); // Avoid  player control

            if (PlanetNotOurs)
            {
                ScuttleShip();
                return GoalStep.GoalComplete;
            }

            if (SupplyShip.Position.InRadius(PlanetBuildingAt.Position, PlanetBuildingAt.Radius + 500f))
            {
                SupplyShip.QueueTotalRemoval();
                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }

        void ScuttleShip()
        {
            TargetShip?.Supply.ChangeIncomingOrdnance(-SupplyShip.Ordinance);
            SupplyShip.ScuttleTimer = 1;
            SupplyShip.AI.ClearOrders(AIState.Scuttle, priority: true);
            SupplyShip.QueueTotalRemoval();
        }

        bool PlanetNotOurs => PlanetBuildingAt.Owner != Owner;
        bool SupplyAlive   => SupplyShip != null && SupplyShip.Active; // todo also returning home
        bool TargetValid   => TargetShip != null
                              && (TargetShip.Loyalty == Owner || TargetShip.Loyalty.IsAlliedWith(Owner))
                              && TargetShip.IsSuitableForPlanetaryRearm()
                              && (TargetShip.System == PlanetBuildingAt.System || TargetShip.IsPlatformOrStation);



        Ship SupplyShip
        {
            get => FinishedShip;
            set => FinishedShip = value;
        }

        bool DivertSupplyShip()
        {
            TargetShip?.Supply.ChangeIncomingOrdnance(-SupplyShip.Ordinance);
            if (SupplyShip.OrdnancePercent > 0.05f && PlanetBuildingAt.TryGetShipsNeedRearm(out Ship[] shipList, Owner))
            {
                // Divert supply
                Owner.AI.AddPlanetaryRearmGoal(shipList[0], PlanetBuildingAt, SupplyShip);
                return true;
            }

            return false;
        }

    }
}
