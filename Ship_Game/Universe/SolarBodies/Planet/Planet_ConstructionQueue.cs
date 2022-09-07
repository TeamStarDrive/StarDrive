using Ship_Game.AI;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;

namespace Ship_Game
{
    public partial class Planet
    {
        public void RefreshBuildingsWeCanBuildHere()
        {
            if (Owner == null)
                return;

            var canBuild = new Array<Building>();

            // See if it already has a command building or not.
            bool needCommandBuilding = BuildingList.All(b => !b.IsCapitalOrOutpost);

            foreach (KeyValuePair<string, bool> keyValuePair in Owner.GetBDict())
            {
                if (!keyValuePair.Value)
                    continue;

                // when loading from savegames, unlocked BDict can contain invalid entries
                if (!ResourceManager.GetBuilding(keyValuePair.Key, out Building b))
                    continue;

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
                // Terraformer Limit check
                if (b.IsTerraformer && (!Terraformable || TerraformersHere + ConstructionQueue.Count(i => i.isBuilding && i.Building.IsTerraformer) >= TerraformerLimit))
                    continue;
                // If the building is still a candidate after all that, then add it to the list!
                canBuild.Add(b);
            }
            BuildingsCanBuild = canBuild;
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
        public bool CivilianBuildingInTheWorks => ConstructionQueue.Any(b => b.IsCivilianBuilding);
        public bool MilitaryBaseInTheWorks     => ConstructionQueue.Any(b => b.isBuilding && !b.Building.AllowInfantry);

        public bool CanBuildInfantry         => BuildingList.Any(b => b.AllowInfantry);
        public bool TroopsInTheWorks         => ConstructionQueue.Any(t => t.isTroop);
        public bool OrbitalsInTheWorks       => ConstructionQueue.Any(b => b.isOrbital || b.ShipData?.IsShipyard == true);
        public int NumShipsInTheWorks        => ConstructionQueue.Count(s => s.isShip);
        public int NumOrbitalsInTheWorks     => ConstructionQueue.Count(b => b.isOrbital);
        public int NumTroopsInTheWorks       => ConstructionQueue.Count(t => t.isTroop);
        public int NumShipYardsInTheWorks    => ConstructionQueue.Count(s => s.ShipData?.IsShipyard == true);
        public bool BiosphereInTheWorks      => BuildingInQueue(Building.BiospheresId);
        public bool TerraformerInTheWorks    => BuildingInQueue(Building.TerraformerId);
        public bool BuildingBuilt(int bid)   => BuildingList.Any(existing => existing.BID == bid);
        public bool BuildingInQueue(int bid) => ConstructionQueue
                                               .Any(q => q.isBuilding && q.Building.BID == bid);

        public bool BuildingsHereCanBeBuiltAnywhere  => !BuildingList.Any(b => !b.CanBuildAnywhere);
        public bool PlayerAddedFirstConstructionItem => ConstructionQueue.Count > 0 && ConstructionQueue[0].IsPlayerAdded;

        // exists on planet OR in queue
        public bool BuildingBuiltOrQueued(Building b) => BuildingBuilt(b.BID) || BuildingInQueue(b.BID);
        public bool BuildingBuiltOrQueued(int bid) => BuildingBuilt(bid) || BuildingInQueue(bid);

        int TurnsUntilQueueCompleted(float priority, Array<QueueItem> newItems = null)
        {
            if (newItems == null && ConstructionQueue.Count == 0)
                return 0;

            priority = priority.Clamped(0, 1);
            // Turns to use all stored production with just infra, if exporting, expect only 50% will remain
            float turnsWithInfra = (ExportProd ? ProdHere/2 : ProdHere) / InfraStructure.LowerBound(0.01f);
            // Modify the number of turns that can use all production.
            turnsWithInfra *= priority;
            // Percentage of the pop allocated to production
            float workPercentage = IsCybernetic ? 1 : 1 - Food.Percent;

            // Getting the queue copied and inserting the new items into it to check time to finish
            // This is needed since the planet has dynamic production allocation based on queue items
            var modifiedQueue = ConstructionQueue.ToArrayList();
            if (newItems != null)
                modifiedQueue.AddRange(newItems);

            float totalTurnsToCompleteQueue = 0;
            for (int i = 0; i < modifiedQueue.Count; i++)
            {
                QueueItem qi = modifiedQueue[i];
                // How much production will be created for this item (since some will be diverted to research)
                float productionOutputForItem = workPercentage * EvaluateProductionQueue(qi) * PopulationBillion;
                // How much net production will be applied to the queue item after checking planet's trade state
                float netProdPerTurn = LimitedProductionExpenditure(turnsWithInfra <= 0 ? productionOutputForItem 
                                                                                       : productionOutputForItem + InfraStructure);

                float turnsToCompleteItem = qi.ProductionNeeded / netProdPerTurn.LowerBound(0.01f);
                // Reduce the turns with infra by the turns needed to complete the item so it can be better evaluated next qi
                // We are ignoring excess turns without infra for simplicity
                turnsWithInfra            -= turnsToCompleteItem;
                totalTurnsToCompleteQueue += turnsToCompleteItem;
            }

            return (int)totalTurnsToCompleteQueue;
        }

        // @return Total numbers before ship will be finished if
        // inserted to the end of the queue.
        // if sData is null, then we want a troop
        public int TurnsUntilQueueComplete(float cost, float priority, IShipDesign sData = null)
        {
            bool forTroop = sData == null;
            if (forTroop && !HasSpacePort
                || forTroop && (!CanBuildInfantry || ConstructionQueue.Count(q => q.isTroop) >= 2))
            {
                return 9999;
            }

            int total = TurnsUntilQueueCompleted(priority, CreateItemsForTurnsCompleted(CreateQi())); // FB - this is just an estimation
            return total.UpperBound(9999);

            // Local Method
            QueueItem CreateQi()
            {
                var qi = new QueueItem(this)
                {
                    isShip  = !forTroop,
                    ShipData = sData,
                    isTroop = forTroop,
                    Cost    = forTroop ? cost : cost * ShipBuildingModifier,
                };

                return qi;
            }
        }

        // This creates items based on the new item we want to check completion and
        // Adding all refit goals as a new items to calculate these as well
        Array<QueueItem> CreateItemsForTurnsCompleted(QueueItem newItem)
        {
            Array<QueueItem> items = new Array<QueueItem>();
            if (TryGetQueueItemsFromRefitGoals(out Array<QueueItem> refitItems))
                items.AddRange(refitItems);

            items.Add(newItem);
            return items;

            // Local Method
            bool TryGetQueueItemsFromRefitGoals(out Array<QueueItem> refitQueue)
            {
                refitQueue = new Array<QueueItem>();
                var refitGoals = Owner.GetEmpireAI().Goals
                    .Filter(g => (g.type == GoalType.Refit || g.type == GoalType.RefitOrbital) && g.PlanetBuildingAt == this);

                if (refitGoals.Length == 0)
                    return false;

                for (int i = 0; i < refitGoals.Length; i++)
                {
                    Goal goal  = refitGoals[i];
                    if (goal.ToBuildUID.NotEmpty())
                    {
                        var newShip = ResourceManager.GetShipTemplate(goal.ToBuildUID, false);
                        if (goal.OldShip != null && newShip != null)
                        {
                            var qi = new QueueItem(this)
                            {
                                isShip = true,
                                Cost   = goal.OldShip.RefitCost(newShip) * ShipBuildingModifier,
                                ShipData = newShip.ShipData
                            };

                            refitQueue.Add(qi);
                        }
                    }
                }

                return refitQueue.Count > 0;
            }
        }

        public float TotalProdNeededInQueue()
        {
            return ConstructionQueue.Sum(qi => qi.ProductionNeeded);
        }

        public float MissingProdHereForScrap(Goal[] scrapGoals)
        {
            float effectiveProd         = ProdHere + IncomingProd;
            if (scrapGoals.Length > 0)
            {
                var scrapGoalsTargetingThis = scrapGoals.Filter(g => g.type == GoalType.ScrapShip && g.PlanetBuildingAt == this);
                if (scrapGoalsTargetingThis.Length > 0)
                    effectiveProd += scrapGoalsTargetingThis.Sum(g => g.OldShip?.GetScrapCost() ?? 0);
            }

            return Storage.Max - effectiveProd; // Negative means we have excess prod
        }

        public Array<Ship> GetAllShipsInQueue() => ShipRolesInQueue(null);

        public bool IsColonyShipInQueue()       => FirstShipRoleInQueue(RoleName.colony) != null;
        public bool IsColonyShipInQueue(Goal g) => ConstructionQueue.Any(q => q.isShip && q.Goal == g);

        public Array<Ship> ShipRolesInQueue(RoleName[] roles)
        {
            var ships = new Array<Ship>();
            foreach (var s in ConstructionQueue)
            {
                if (s.isShip)
                {
                    var ship = ResourceManager.GetShipTemplate(s.ShipData.Name);
                    if (roles == null || roles.Contains(ship.DesignRole))
                        ships.Add(ship);
                }

            }
            return ships;
        }
        public Ship FirstShipRoleInQueue(RoleName role)
        {
            foreach (var s in ConstructionQueue)
            {
                if (s.isShip)
                {
                    var ship = ResourceManager.GetShipTemplate(s.ShipData.Name);
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
                RoleName.station,
                RoleName.platform
            };
            return MaintenanceCostOfShipRolesInQueue(roles);
        }

        public float MaintenanceCostOfShipRolesInQueue(RoleName[] roles)
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

        public bool HasColonyShipFirstInQueue()
        {
            return ConstructionQueue.Count > 0 && ConstructionQueue[0].Goal?.type == GoalType.Colonize;
        }
    }
}
