using System;
using System.Linq;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class BuildOffensiveShips : Goal
    {
        public const string ID = "BuildOffensiveShips";
        public override string UID => ID;

        public BuildOffensiveShips() : base(GoalType.BuildShips)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                KeepBeggingForProductionOfOurShipItem,
                OrderShipToAwaitOrders
            };
        }
        public BuildOffensiveShips(string shipType, Empire e) : this()
        {
            ToBuildUID = shipType;
            empire = e;
            beingBuilt = ResourceManager.GetShipTemplate(shipType);
            Evaluate();
        }

        private GoalStep FindPlanetToBuildAt()
        {
            if (beingBuilt == null)
                beingBuilt = ResourceManager.GetShipTemplate(ToBuildUID);
            Planet planet1 = null;
            var list = new Array<Planet>();
            foreach (Planet planet2 in empire.GetPlanets().OrderBy(planet =>
            {
                float weight = 0;
                switch (planet.colonyType)
                {
                    case Planet.ColonyType.Core: weight += 4; break;
                    case Planet.ColonyType.Colony: break;
                    case Planet.ColonyType.Industrial: weight += 2; break;
                    case Planet.ColonyType.Research: weight -= 6; break;
                    case Planet.ColonyType.Agricultural: weight -= 6; break;
                    case Planet.ColonyType.Military: weight += 2; break;
                    case Planet.ColonyType.TradeHub: weight += 2; break;
                }
                weight += planet.Level;
                weight += planet.MineralRichness;
                return weight;
            }))
            {
                if (planet2.HasSpacePort && planet2.colonyType != Planet.ColonyType.Research)
                    list.Add(planet2);
            }
            int num1 = 9999999;
            int x = 0;
            foreach (Planet planet2 in list)
            {
                if (x > empire.GetPlanets().Count * .2f)
                    break;
                int num2 = 0;
                foreach (QueueItem queueItem in planet2.ConstructionQueue)
                    num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.Prod.NetMaxPotential);
                if (planet2.ConstructionQueue.Count == 0)
                    num2 = (int)((beingBuilt.GetCost(empire) - planet2.ProdHere) / planet2.Prod.NetMaxPotential);
                if (num2 < num1)
                {
                    num1 = num2;
                    planet1 = planet2;
                }
            }
            if (planet1 == null)
                return GoalStep.TryAgain;
            PlanetBuildingAt = planet1;
            planet1.ConstructionQueue.Add(new QueueItem(planet1)
            {
                isShip = true,
                QueueNumber = planet1.ConstructionQueue.Count,
                sData = beingBuilt.shipData,
                Goal = this,
                Cost = beingBuilt.GetCost(empire)

            });
            return GoalStep.GoToNextStep;
        }

        private GoalStep KeepBeggingForProductionOfOurShipItem()
        {
            if (MainGoalCompleted) // @todo Refactor this messy logic
                return GoalStep.GoToNextStep;

            if (PlanetBuildingAt == null || PlanetBuildingAt.ConstructionQueue.Count == 0)
                return GoalStep.TryAgain;
            if (PlanetBuildingAt.ConstructionQueue[0].Goal == this)
            {
                if (PlanetBuildingAt.ProdHere > PlanetBuildingAt.Storage.Max * 0.5f)
                    PlanetBuildingAt.ApplyStoredProduction(0);
            }
            return GoalStep.TryAgain;
        }

        private GoalStep OrderShipToAwaitOrders()
        {
            if (beingBuilt == null)
            {
                Log.Warning($"BeingBuilt was null in {type} completion");
                return GoalStep.GoalComplete;
            }
            beingBuilt.AI.State = AIState.AwaitingOrders;
            return GoalStep.GoalComplete;
        }
    }
}
