using System;
using System.Linq;
using Ship_Game.Gameplay;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Commands
{
    class FleetRequisition : Goal
    {
        public FleetRequisition(ArtificialIntelligence.ShipGoal goal, ArtificialIntelligence ai)
        {
            return;
            FleetDataNode node = ai.Owner.fleet.DataNodes.First(thenode => thenode.Ship == ai.Owner);
            beingBuilt = ResourceManager.ShipsDict[goal.VariableString];
            GoalName = "FleetRequisition";
            Step = 1;
            beingBuilt.fleet = ai.Owner.fleet;
            beingBuilt.RelativeFleetOffset = node.FleetOffset;
            SetFleet(ai.Owner.fleet);
            SetPlanetWhereBuilding(ai.OrbitTarget);            
        }

        public override void Evaluate()
        {
            if (Held)
                return;
            DoFleetRequisition();
        }

        public override void ReportShipComplete(Ship ship)
        {
            beingBuilt = ship;
            ++this.Step;
        }

        // Edited by EVWeb
        private void DoFleetRequisition()
        {
            switch (Step)
            {
                case 0:
                    Planet planet1 = (Planet)null;
                    Array<Planet> list = new Array<Planet>();
                    foreach (Planet planet2 in empire.GetPlanets())
                    {
                        if (planet2.HasShipyard)
                            list.Add(planet2);
                    }
                    int num1 = 9999999;
                    int x = 0;
                    foreach (Planet planet2 in list.OrderBy(planet =>
                    {
                        float weight = -6;                      // so shipyard modifier works properly
                        switch (planet.colonyType)
                        {
                            case Planet.ColonyType.Core:
                                weight -= 4;
                                break;
                            case Planet.ColonyType.Colony:
                                break;
                            case Planet.ColonyType.Industrial:
                                weight -= 2;
                                break;
                            case Planet.ColonyType.Research:
                                weight += 6;
                                break;
                            case Planet.ColonyType.Agricultural:
                                weight += 6;
                                break;
                            case Planet.ColonyType.Military:
                                weight -= 2;
                                break;
                            case Planet.ColonyType.TradeHub:
                                weight -= 2;
                                break;
                        }
                        //weight -= planet.ConstructionQueue.Count;

                        weight -= planet.developmentLevel;          // minus because order by goes smallest to largest, not other way round
                        weight -= planet.MineralRichness;
                        weight /= planet.ShipBuildingModifier;      // planets with shipyards in orbit should be higher in list

                        return weight;
                    }))
                    {
                        if (x < empire.GetPlanets().Count * .2)     // We already checked for whether or not this is a shipyard planet
                        {
                            int num2 = 0;
                            x++;
                            foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                                num2 += (int)(queueItem.Cost - queueItem.productionTowards);


                            num2 += (int)(beingBuilt.GetCost(empire) - planet2.ProductionHere);   // Include the cost of the ship to be built on EVERY planet, not just ones with nothing to build
                            num2 = (int)(num2 * planet2.ShipBuildingModifier / (planet2.GovernorOn ? planet2.GetMaxProductionPotential() : planet2.NetProductionPerTurn));         // Apply ship cost reduction to everything just so planets with shipyards in orbit are more likely to be picked, also, if on manual control, don't assume you can get max production
                            if (num2 < num1)
                            {
                                num1 = num2;
                                planet1 = planet2;
                            }
                        }
                    }
                    if (planet1 == null)
                        break;
                    PlanetBuildingAt = planet1;
                    planet1.ConstructionQueue.Add(new QueueItem()
                    {
                        isShip = true,
                        QueueNumber = planet1.ConstructionQueue.Count,
                        sData = beingBuilt.GetShipData(),
                        Goal = this,
                        Cost = beingBuilt.GetCost(empire),
                        NotifyOnEmpty = false
                    });
                    ++Step;
                    break;
                case 2:
                    if (fleet != null)
                    {
                        foreach (FleetDataNode current in fleet.DataNodes)
                        {
                            if (current.GoalGUID != guid) continue;
                            if (fleet.Ships.Count == 0)
                                fleet.Position += new Vector2(RandomMath.RandomBetween(-3000f, 3000f), RandomMath.RandomBetween(-3000f, 3000f));

                            var ship = beingBuilt.Clone();
                            current.Ship = ship;
                            fleet.AddShip(ship);
                            current.GoalGUID = Guid.Empty;

                            ship.fleet = fleet;
                            ship.RelativeFleetOffset = current.FleetOffset;
                            ship.AI.OrderMoveToFleetPosition(fleet.Position + ship.FleetOffset, ship.fleet.Facing, new Vector2(0.0f, -1f), true, fleet.Speed, fleet);
                        }
                        break;
                    }
                    empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
            }
        }
    }
}
