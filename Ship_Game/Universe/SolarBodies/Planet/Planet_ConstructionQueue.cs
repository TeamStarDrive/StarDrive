using Ship_Game.AI;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;

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
        public bool CivilianBuildingInTheWorks => ConstructionQueue.Any(b => b.isBuilding && !b.IsMilitary);
        public bool MilitaryBaseInTheWorks     => ConstructionQueue.Any(b => b.isBuilding && !b.Building.AllowInfantry);

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

        public bool BuildingsHereCanBeBuiltAnywhere  => !BuildingList.Any(b => !b.CanBuildAnywhere);
        public bool PlayerAddedFirstConstructionItem => ConstructionQueue.Count > 0 && ConstructionQueue[0].IsPlayerAdded;

        // exists on planet OR in queue
        public bool BuildingBuiltOrQueued(Building b) => BuildingBuilt(b.BID) || BuildingInQueue(b.BID);
        public bool BuildingBuiltOrQueued(int bid) => BuildingBuilt(bid) || BuildingInQueue(bid);

        public int TurnsUntilQueueCompleted(float extraItemCost = 0, float priority = 1.00f)
        {
            float totalProdNeeded = TotalProdNeededInQueue() + extraItemCost;
            if (totalProdNeeded.AlmostZero())
                return 0;

            priority = priority.Clamped(0, 1);

            // issue in this calculation is that we dont know the future of the production stores.
            // we assume we will have all of it for the build queue but that is unpredictable as it can
            // also be exporting that production and then these calculation would not apply.
            // for the purposes of calculating the best planet to build on
            // it could be changed to just totalProdNeeded / Prod.NetMaxPotential which would give
            // a reliable baseline that isnt dependent on the unknown future of the production stores.

            // max production useable per turn when we have production stored.
            float maxProductionWithInfra    = MaxProductionToQueue.LowerBound(0.01f);
            // turns to use all stored production with just infra
            float turnsWithInfra            = ProdHere / InfraStructure.LowerBound(0.01f);
            // modify the number of turns that can use all production.
            float prioritizedTurns          = turnsWithInfra * priority;
            // turns in queue using just infra * max production that can be added per turn with stores.
            float totalProdWithInfra        = prioritizedTurns * maxProductionWithInfra;
            // how much is left to build after all production is gone.
            float prodNeededAfterStorageEnd = totalProdNeeded - totalProdWithInfra;

            if (prodNeededAfterStorageEnd <=0) // we can produce all queue with max prod and storage
                return (int)(totalProdNeeded / maxProductionWithInfra);

            // there is no more production stored. How long to build without it.
            float potentialProduction = Prod.NetMaxPotential.LowerBound(0.01f);
            float turnsWithoutInfra   = prodNeededAfterStorageEnd / potentialProduction;

            return (int)(turnsWithInfra + turnsWithoutInfra);
        }

        // @return Total numbers before ship will be finished if
        //         inserted to the end of the queue.
        public int TurnsUntilQueueComplete(float cost, bool forTroop, float priority)
        {
            if (!forTroop && !HasSpacePort || forTroop && (!CanBuildInfantry || ConstructionQueue.Count(q => q.isTroop) >= 2))
                return 9999; // impossible

            float effectiveCost = forTroop ? cost : (cost * ShipBuildingModifier).LowerBound(0);
            effectiveCost      += TotalShipCostInRefitGoals();
            int total           = TurnsUntilQueueCompleted(effectiveCost, priority); // FB - this is just an estimation
            return total.UpperBound(9999);
        }

        public float TotalProdNeededInQueue()
        {
            return ConstructionQueue.Sum(qi => qi.ProductionNeeded);
        }

        float TotalShipCostInRefitGoals()
        {
            var refitGoals = Owner.GetEmpireAI().Goals
                .Filter(g => (g.type == GoalType.Refit || g.type == GoalType.RefitOrbital) && g.PlanetBuildingAt == this);

            if (refitGoals.Length == 0)
                return 0;

            float cost = 0;
            for (int i = 0; i < refitGoals.Length; i++)
            {
                Goal goal = refitGoals[i];
                if (goal.ToBuildUID.NotEmpty())
                {
                    var newShip = ResourceManager.GetShipTemplate(goal.ToBuildUID, false);
                    if (goal.OldShip != null && newShip != null)
                        cost += goal.OldShip.RefitCost(newShip) * ShipBuildingModifier;
                }
            }

            return cost.LowerBound(0);
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

        public bool IsColonyShipInQueue()       => FirstShipRoleInQueue(ShipData.RoleName.colony) != null;
        public bool IsColonyShipInQueue(Goal g) => ConstructionQueue.Any(q => q.isShip && q.Goal == g);

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

        public bool HasColonyShipFirstInQueue()
        {
            return ConstructionQueue.Count > 0 && ConstructionQueue[0].Goal?.type == GoalType.Colonize;
        }
    }
}
