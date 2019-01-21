using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;

namespace Ship_Game
{
    public partial class Planet
    {
        public void RefreshBuildingsWeCanBuildHere()
        {
            if (Owner == null) return;
            BuildingsCanBuild.Clear();

            // See if it already has a command building or not.
            bool needCommandBuilding = BuildingList.All(b => !b.IsCapitalOrOutpost);

            foreach (KeyValuePair<string, bool> keyValuePair in Owner.GetBDict())
            {
                if (!keyValuePair.Value)
                    continue;
                Building b = ResourceManager.GetBuildingTemplate(keyValuePair.Key);
                // Skip adding +food buildings for cybernetic races
                if (IsCybernetic && (b.PlusFlatFoodAmount > 0 || b.PlusFoodPerColonist > 0))
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

        public bool CanBuildInfantry         => BuildingList.Any(b => b.AllowInfantry);
        public bool BuildingInTheWorks       => ConstructionQueue.Any(b => b.isBuilding);
        public bool OrbitalsInTheWorks       => ConstructionQueue.Any(b => b.isOrbital || b.sData != null && b.sData.IsShipyard);
        public bool BiosphereInTheWorks      => BuildingInQueue(Building.BiospheresId);
        public int TotalTurnsInConstruction  => ConstructionQueue.Count > 0 ? NumberOfTurnsUntilCompleted(ConstructionQueue.Last) : 0;
        public bool BuildingBuilt(int bid)   => BuildingList.Any(existing => existing.BID == bid);
        public bool BuildingInQueue(int bid) => ConstructionQueue
                                               .Any(q => q.isBuilding && q.Building.BID == bid);


                                            
        // exists on planet OR in queue
        public bool BuildingBuiltOrQueued(Building b) => BuildingBuilt(b.BID) || BuildingInQueue(b.BID);
        public bool BuildingBuiltOrQueued(int bid) => BuildingBuilt(bid) || BuildingInQueue(bid);

        bool FindConstructionBuilding(Goods goods, out QueueItem item)
        {
            foreach (QueueItem it in ConstructionQueue)
            {
                if (it.isBuilding) switch (goods)
                {
                    case Goods.Food:       if (it.Building.ProducesFood)       { item = it; return true; } break;
                    case Goods.Production: if (it.Building.ProducesProduction) { item = it; return true; } break;
                    case Goods.Colonists:  if (it.Building.ProducesPopulation) { item = it; return true; } break;
                }
            }
            item = null;
            return false;
        }

        int NumberOfTurnsUntilCompleted(QueueItem item)
        {
            int turns = 0;
            foreach (QueueItem q in ConstructionQueue)
            {
                turns += q.TurnsUntilComplete;
                if (q == item)
                    break;
            }
            return turns;
        }

        public float TotalCostOfTroopsInQueue()
        {
            return ConstructionQueue.Filter(qi => qi.isTroop).Sum(qi => qi.Cost);
        }
    }
}
