using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class BuildOffensiveShips : BuildShipsGoalBase
    {
        [StarDataConstructor]
        public BuildOffensiveShips(Empire owner) : base(GoalType.BuildOffensiveShips, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                MainGoalKeepRushingProductionOfOurShip,
                OrderShipToAwaitOrders
            };
        }

        public BuildOffensiveShips(string shipType, Empire owner) : this(owner)
        {
            Build = new(shipType);
        }

        GoalStep FindPlanetToBuildAt()
        {
            return TryBuildShip(SpacePortType.Any);
        }

        GoalStep MainGoalKeepRushingProductionOfOurShip()
        {
            if (MainGoalCompleted) // @todo Refactor this messy logic
                return GoalStep.GoToNextStep;

            if (PlanetBuildingAt == null || PlanetBuildingAt.NotConstructing)
                return GoalStep.RestartGoal;

            float importance = Owner.AI.ThreatLevel;

            if ((importance > 0.5f || Owner.IsMilitarists)
                && PlanetBuildingAt.ConstructionQueue[0]?.Goal == this
                && PlanetBuildingAt.Storage.ProdRatio > 0.75f
                && Owner.AI.SafeToRush) 
            {
                float rush = (10f * (importance + 0.5f)).UpperBound(PlanetBuildingAt.ProdHere);
                PlanetBuildingAt.Construction.RushProduction(0, rush);
            }
            return GoalStep.TryAgain;
        }

        GoalStep OrderShipToAwaitOrders()
        {
            if (FinishedShip == null)
            {
                Log.Warning($"BeingBuilt was null in {Type} completion");
                return GoalStep.GoalFailed;
            }

            Planet planetToOrbit = Owner.GetOrbitPlanetAfterBuild(PlanetBuildingAt);
            FinishedShip.OrderToOrbit(planetToOrbit, clearOrders: true);

            return GoalStep.GoalComplete;
        }

        public struct ShipInfo
        {
            public float Upkeep { get;}
            public RoleName Role { get;}

            public ShipInfo(Empire owner, Goal goal) : this(owner, goal as BuildOffensiveShips)
            {
            }

            public ShipInfo(Empire owner, BuildOffensiveShips g)
            {
                Role = g.Build.Template.Role;
                Upkeep = g.Build.Template.GetMaintenanceCost(owner, 0);
            }
        }
    }
}

