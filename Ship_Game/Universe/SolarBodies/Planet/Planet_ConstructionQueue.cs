using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if (b.Unique && BuildingExists(b))
                    continue;
                // Hide Biospheres if the entire planet is already habitable
                if (b.IsBiospheres && AllTilesHabitable())
                    continue;
                // If this is a one-per-empire building, make sure it hasn't been built already elsewhere
                // Reusing fountIt bool from above
                if (b.BuildOnlyOnce && IsBuiltWithinEmpire(b))
                    continue;
                // If the building is still a candidate after all that, then add it to the list!
                BuildingsCanBuild.Add(b);
            }
        }

        bool IsBuiltWithinEmpire(Building b)
        {
            // Check for this unique building across the empire
            foreach (Planet planet in Owner.GetPlanets())
                if (planet.BuildingExists(b))
                    return true;
            return false;
        }

        bool AllTilesHabitable()
        {
            return TilesList.All(tile => tile.Habitable);
        }

        
        public void ApplyAllStoredProduction(int index)  => SbProduction.ApplyAllStoredProduction(index);
        public bool ApplyStoredProduction(int index)     => SbProduction.ApplyStoredProduction(index);
        public void ApplyProductionTowardsConstruction() => SbProduction.ApplyProductionTowardsConstruction();
        public bool BuildingExists(Building b)           => BuildingExists(b.BID);
        public bool CanBuildInfantry                     => BuildingList.Any(b => b.AllowInfantry);
        public bool BuildingInTheWorks                   => ConstructionQueue.Any(b => b.isBuilding);
        public bool BiosphereInTheWorks                  => BuildingInQueue(Building.BiospheresId);
        public int TotalTurnsInConstruction              => ConstructionQueue.Count > 0 ? NumberOfTurnsUntilCompleted(ConstructionQueue.Last) : 0;

        public bool TryBiosphereBuild(Building b, QueueItem qi)           => SbProduction.TryBiosphereBuild(b, qi);
        public void ApplyProductionToQueue(float howMuch, int whichItem)  => SbProduction.ApplyProductiontoQueue(howMuch, whichItem);
        public void AddBuildingToCQ(Building b, bool playerAdded = false) => SbProduction.AddBuildingToCQ(b, playerAdded);


        public bool BuildingInQueue(int buildingId) => ConstructionQueue.Any(q => q.isBuilding
                                                                                  && q.Building.BID == buildingId);

        public bool BuildingExists(int buildingId)  => BuildingList.Any(existing => existing.BID == buildingId)
                                                      || BuildingInQueue(buildingId);

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
            int totalTurns = 0;
            foreach (QueueItem it in ConstructionQueue)
            {
                totalTurns += it.EstimatedTurnsToComplete;
                if (it == item)
                    break;
            }
            return totalTurns;
        }

        public float GetTotalConstructionQueueMaintenance()
        {
            float count = 0;
            foreach (QueueItem b in ConstructionQueue)
            {
                if (!b.isBuilding) continue;
                count -= b.Building.Maintenance + b.Building.Maintenance * Owner.data.Traits.MaintMod;
            }
            return count;
        }
    }
}
