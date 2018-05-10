﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class BuildDefensiveShips : Goal
    {
        public const string ID = "BuildDefensiveShips";
        public override string UID => ID;

        public BuildDefensiveShips() : base(GoalType.BuildShips)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildDefensiveShipsAt,
                WaitMainGoalCompletion,
                OrderBuiltShipToDefend
            };
        }

        private GoalStep FindPlanetToBuildDefensiveShipsAt()
        {
            if (beingBuilt == null)
                beingBuilt = ResourceManager.ShipsDict[ToBuildUID];
            Planet planet1 = null;
            Array<Planet> list = new Array<Planet>();
            foreach (Planet planet2 in empire.GetPlanets())
            {
                if (planet2.HasShipyard)
                    list.Add(planet2);
            }
            int num1 = 9999999;
            foreach (Planet planet2 in list)
            {
                if (planet2.ParentSystem.combatTimer > 0f)  //fbedard
                {
                    int num2 = 0;
                    foreach (QueueItem queueItem in planet2.ConstructionQueue)
                        num2 += (int)(((double)queueItem.Cost - (double)queueItem.productionTowards) / (double)planet2.NetProductionPerTurn);
                    if (planet2.ConstructionQueue.Count == 0)
                        num2 = (int)(((double)this.beingBuilt.GetCost(this.empire) - (double)planet2.ProductionHere) / (double)planet2.NetProductionPerTurn);
                    if (num2 < num1)
                    {
                        num1 = num2;
                        planet1 = planet2;
                    }
                }
            }
            if (planet1 == null)
                foreach (Planet planet2 in list)
                {
                    int num2 = 0;
                    foreach (QueueItem queueItem in planet2.ConstructionQueue)
                        num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.GetMaxProductionPotential());
                    if (planet2.ConstructionQueue.Count == 0)
                        num2 = (int)((this.beingBuilt.GetCost(this.empire) - planet2.ProductionHere) / planet2.GetMaxProductionPotential());
                    if (num2 < num1)
                    {
                        num1 = num2;
                        planet1 = planet2;
                    }
                }
            if (planet1 == null)
                return GoalStep.TryAgain;

            this.PlanetBuildingAt = planet1;
            planet1.ConstructionQueue.Add(new QueueItem()
            {
                isShip = true,
                QueueNumber = planet1.ConstructionQueue.Count,
                sData = beingBuilt.shipData,
                Goal = this,
                Cost = beingBuilt.GetCost(empire)
            });
            return GoalStep.GoToNextStep;
        }

        private GoalStep OrderBuiltShipToDefend()
        {
            beingBuilt.DoDefense();
            return GoalStep.GoalComplete;
        }

    }
}
