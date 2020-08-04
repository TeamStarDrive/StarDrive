﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet
    {
        public void RefreshBuildingsWeCanBuildHere()
        {
            if (Owner == null) 
                return;

            BuildingsCanBuild.Clear();

            // See if it already has a command building or not.
            bool needCommandBuilding = BuildingList.All(b => !b.IsCapitalOrOutpost);

            foreach (KeyValuePair<string, bool> keyValuePair in Owner.GetBDict())
            {
                if (!keyValuePair.Value)
                    continue;
                Building b = ResourceManager.GetBuildingTemplate(keyValuePair.Key);
                // Skip adding + food buildings for cybernetic races
                if (IsCybernetic && !b.ProducesProduction && !b.ProducesResearch && b.ProducesFood)
                    continue;
                // Skip adding command buildings if planet already has one
                if (!needCommandBuilding && b.IsCapitalOrOutpost)
                    continue;
                // Make sure the building isn't already built on this planet
                if (b.Unique && BuildingBuiltOrQueued(b))
                    continue;
                // Hide Biospheres if the entire planet is already habitable
                if (b.IsBiospheres && AllTilesHabitable())
                    continue;
                // If this is a one-per-empire building, make sure it hasn't been built already elsewhere
                // Reusing fountIt bool from above
                if (b.BuildOnlyOnce && IsBuiltOrQueuedWithinEmpire(b))
                    continue;
                // If the building is still a candidate after all that, then add it to the list!
                BuildingsCanBuild.Add(b);
            }
        }

        public bool IsBuiltOrQueuedWithinEmpire(Building b)
        {
            // Check for this unique building across the empire
            foreach (Planet planet in Owner.GetPlanets())
                if (planet.BuildingBuiltOrQueued(b))
                    return true;
            return false;
        }

        bool AllTilesHabitable()
        {
            return TilesList.All(tile => tile.Habitable);
        }

        public bool MilitaryBuildingInTheWorks => ConstructionQueue.Any(b => b.isBuilding && b.IsMilitary);
        public bool CivilianBuildingInTheWorks => ConstructionQueue.Any(b => b.isBuilding && !b.IsMilitary);
        public bool BuildingInTheWorks         => ConstructionQueue.Any(b => b.isBuilding);

        public bool CanBuildInfantry         => BuildingList.Any(b => b.AllowInfantry);
        public bool TroopsInTheWorks         => ConstructionQueue.Any(t => t.isTroop);
        public bool OrbitalsInTheWorks       => ConstructionQueue.Any(b => b.isOrbital || b.sData != null && b.sData.IsShipyard);
        public int NumShipsInTheWorks        => ConstructionQueue.Count(s => s.isShip);
        public int NumOrbitalsInTheWorks     => ConstructionQueue.Count(b => b.isOrbital);
        public int NumTroopsInTheWorks       => ConstructionQueue.Count(t => t.isTroop);
        public int NumShipYardsInTheWorks    => ConstructionQueue.Count(s => s.sData != null && s.sData.IsShipyard);
        public bool BiosphereInTheWorks      => BuildingInQueue(Building.BiospheresId);
        public bool TerraformerInTheWorks    => BuildingInQueue(Building.TerraformerId);
        public bool BuildingBuilt(int bid)   => BuildingList.Any(existing => existing.BID == bid);
        public bool BuildingInQueue(int bid) => ConstructionQueue
                                               .Any(q => q.isBuilding && q.Building.BID == bid);

        // exists on planet OR in queue
        public bool BuildingBuiltOrQueued(Building b) => BuildingBuilt(b.BID) || BuildingInQueue(b.BID);
        public bool BuildingBuiltOrQueued(int bid) => BuildingBuilt(bid) || BuildingInQueue(bid);

        public int TurnsUntilQueueCompleted
        {
            get
            {
                int turns = 0;
                for (int i = 0; i < ConstructionQueue.Count; ++i)
                   turns += ConstructionQueue[i].TurnsUntilComplete;


                float netPerTurn      = EstimatedAverageProduction + InfraStructure;
                float totalProd       = turns * netPerTurn;
                float effectiveProd   = (totalProd - ProdHere).LowerBound(0);
                return (int)(effectiveProd / netPerTurn);
            }
        }

        // @return Total numbers before ship will be finished if
        //         inserted to the end of the queue.
        public int TurnsUntilQueueComplete(float cost, bool forTroop)
        {
            if (!forTroop && !HasSpacePort || forTroop && !CanBuildInfantry)
                return 9999; // impossible

            float effectiveCost = forTroop ? cost : (cost * ShipBuildingModifier).LowerBound(0);
            int itemTurns       = (int)Math.Ceiling(effectiveCost.LowerBound(0) / MaxProduction);
            int total           = itemTurns + TurnsUntilQueueCompleted;
            return total.UpperBound(9999);
        }

        public float TotalCostOfTroopsInQueue()
        {
            return ConstructionQueue.Filter(qi => qi.isTroop).Sum(qi => qi.Cost);
        }

        public float TotalProdNeededInQueue()
        {
            return ConstructionQueue.Sum(qi => qi.ProductionNeeded);
        }

        public Array<Ship> GetAllShipsInQueue() => ShipRolesInQueue(null);

        public bool IsColonyShipInQueue() => FirstShipRoleInQueue(ShipData.RoleName.colony) != null;

        public Array<Ship> ShipRolesInQueue(ShipData.RoleName[] roles)
        {
            var ships = new Array<Ship>();
            foreach (var s in ConstructionQueue)
            {
                if (s.isShip)
                {
                    var ship = ResourceManager.GetShipTemplate(s.sData.Name);
                    if (roles == null || roles.Contains(ship.DesignRole))
                        ships.Add(ship);
                }

            }
            return ships;
        }
        public Ship FirstShipRoleInQueue(ShipData.RoleName role)
        {
            foreach (var s in ConstructionQueue)
            {
                if (s.isShip)
                {
                    var ship = ResourceManager.GetShipTemplate(s.sData.Name);
                    if (ship.DesignRole == role)
                        return ship;
                }

            }
            return null;
        }

        public float MaintenanceCostOfShipsInQueue() => MaintenanceCostOfShipRolesInQueue(null);
        public float MaintenanceCostOfDefensiveOrbitalsInQueue()
        {
            var roles = new[]
            {
                ShipData.RoleName.station,
                ShipData.RoleName.platform
            };
            return MaintenanceCostOfShipRolesInQueue(roles);
        }

        public float MaintenanceCostOfShipRolesInQueue(ShipData.RoleName[] roles)
        {
            float cost =0 ;
            var ships = GetAllShipsInQueue();
            foreach(Ship ship in ships)
            {
                if (roles == null || roles.Contains(ship.DesignRole))
                    cost += ship.GetMaintCost(Owner);
            }
            return cost;
        }
    }
}
