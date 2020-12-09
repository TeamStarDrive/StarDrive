using System;
using Ship_Game.AI;
using Ship_Game.Ships;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class RearmShipFromPlanet : Goal
    {
        public const string ID = "RearmShipFromPlanet";
        public override string UID => ID;

        public RearmShipFromPlanet() : base(GoalType.RearmShipFromPlanet)
        {
            Steps = new Func<GoalStep>[]
            {
                LaunchSupplyShip,
                TransferOrdnance,
                LandBackOnPlanet
            };
        }

        public RearmShipFromPlanet(Ship shipToRearm, Planet planet, Empire owner) : this()
        {
            TargetShip       = shipToRearm;
            empire           = owner;
            PlanetBuildingAt = planet;

            Evaluate();
        }

        public RearmShipFromPlanet(Ship shipToRearm, Ship existingSupplyShip, Planet planet, Empire owner) : this()
        {
            TargetShip       = shipToRearm;
            empire           = owner;
            PlanetBuildingAt = planet;
            SupplyShip       = existingSupplyShip;

            Evaluate();
        }

        GoalStep LaunchSupplyShip()
        {
            // If not null then a new goal was assigned for a an existing supply ship with ordnance left
            if (SupplyShip == null) 
            {
                string supplyShipName = empire.GetSupplyShuttleName();
                SupplyShip = Ship.CreateShipAtPoint(supplyShipName, empire, PlanetBuildingAt.Center
                    .GenerateRandomPointInsideCircle(PlanetBuildingAt.ObjectRadius + 500));

                if (SupplyShip == null)
                    return GoalStep.GoalFailed;
            }

            SupplyShip.AI.AddSupplyShipGoal(TargetShip, ShipAI.Plan.RearmShipFromPlanet);
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

            if (SupplyShip.Center.InRadius(TargetShip.Center, TargetShip.Radius + 500f))
            {
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

            if (SupplyShip.Center.InRadius(PlanetBuildingAt.Center, PlanetBuildingAt.ObjectRadius + 500f))
            {
                SupplyShip.QueueTotalRemoval();
                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }

        void ScuttleShip()
        {
            SupplyShip.ScuttleTimer = 1;
            SupplyShip.AI.ClearOrders(AIState.Scuttle, priority: true);
            SupplyShip.QueueTotalRemoval();
        }

        bool PlanetNotOurs => PlanetBuildingAt.Owner != empire;
        bool SupplyAlive   => SupplyShip != null && SupplyShip.Active; // todo also returning home
        bool TargetValid   => TargetShip != null
                              && (TargetShip.loyalty == empire || TargetShip.loyalty.IsAlliedWith(empire))
                              && TargetShip.IsSuitableForPlanetaryRearm()
                              && TargetShip.System == PlanetBuildingAt.ParentSystem;

        Ship SupplyShip
        {
            get => FinishedShip;
            set => FinishedShip = value;
        }

        bool DivertSupplyShip()
        {
            if (SupplyShip.OrdnancePercent > 0.05f && PlanetBuildingAt.TryGetShipsNeedRearm(out Ship[] shipList, empire))
            {
                // Divert supply
                empire.GetEmpireAI().AddPlanetaryRearmGoal(shipList[0], PlanetBuildingAt, SupplyShip);
                return true;
            }

            return false;
        }

    }
}
