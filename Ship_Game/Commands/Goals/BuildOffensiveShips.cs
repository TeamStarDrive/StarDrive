using System;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildOffensiveShips : BuildShipsGoalBase
    {
        public const string ID = "BuildOffensiveShips";
        public override string UID => ID;

        public BuildOffensiveShips() : base(GoalType.BuildOffensiveShips)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                MainGoalKeepRushingProductionOfOurShip,
                OrderShipToAwaitOrders
            };
        }

        public BuildOffensiveShips(string shipType, Empire e) : this()
        {
            ToBuildUID = shipType;
            empire = e;
            Evaluate();
        }

        GoalStep FindPlanetToBuildAt()
        {
            return FindPlanetToBuildAt(SpacePortType.Any);
        }

        GoalStep MainGoalKeepRushingProductionOfOurShip()
        {
            if (MainGoalCompleted) // @todo Refactor this messy logic
                return GoalStep.GoToNextStep;

            if (PlanetBuildingAt == null || PlanetBuildingAt.NotConstructing)
                return GoalStep.RestartGoal;

            if (empire.IsMilitarists 
                && PlanetBuildingAt.ConstructionQueue[0].Goal == this
                && PlanetBuildingAt.Storage.ProdRatio > 0.75f 
                && empire.data.TaxRate < 0.25f) 
            {
                float rush = 10f.UpperBound(PlanetBuildingAt.ProdHere);
                PlanetBuildingAt.Construction.RushProduction(0, rush, rush: true);
            }
            return GoalStep.TryAgain;
        }

        GoalStep OrderShipToAwaitOrders()
        {
            if (FinishedShip == null)
            {
                Log.Warning($"BeingBuilt was null in {type} completion");
                return GoalStep.GoalFailed;
            }

            Planet planetToOrbit = empire.GetOrbitPlanetAfterBuild(PlanetBuildingAt);
            FinishedShip.AI.OrderToOrbit(planetToOrbit);

            return GoalStep.GoalComplete;
        }

        public struct ShipInfo
        {
            public float Upkeep { get;}
            public ShipData.RoleName Role { get;}

            public ShipInfo(Goal goal) : this(goal as BuildOffensiveShips){ }

            public ShipInfo(BuildOffensiveShips goal)
            {
                if (goal.GetShipTemplate(goal.ToBuildUID, out Ship template))
                {
                    Role = template.DesignRole;
                    Upkeep = template.GetMaintCost();
                }
                else
                {
                    Role = ShipData.RoleName.disabled;
                    Upkeep = 0;
                }
            }
        }
    }
}

