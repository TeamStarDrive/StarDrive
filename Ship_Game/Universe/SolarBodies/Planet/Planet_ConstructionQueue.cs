using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public partial class Planet
    {
        public void ApplyAllStoredProduction(int index) => SbProduction.ApplyAllStoredProduction(index);
        public bool ApplyStoredProduction(int index) => SbProduction.ApplyStoredProduction(index);
        public void ApplyProductionToQueue(float howMuch, int whichItem) => SbProduction.ApplyProductiontoQueue(howMuch, whichItem);
        public bool TryBiosphereBuild(Building b, QueueItem qi) => SbProduction.TryBiosphereBuild(b, qi);
        public void ApplyProductionTowardsConstruction() => SbProduction.ApplyProductionTowardsConstruction();
        public void AddBuildingToCQ(Building b, bool playerAdded = false) => SbProduction.AddBuildingToCQ(b, playerAdded);

        public void RefreshBuildingsWeCanBuildHere()
        {
            if (Owner == null) return;
            BuildingsCanBuild.Clear();

            //See if it already has a command building or not.
            bool needCommandBuilding = true;
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Capital City" || building.Name == "Outpost")
                {
                    needCommandBuilding = false;
                    break;
                }
            }

            foreach (KeyValuePair<string, bool> keyValuePair in Owner.GetBDict())
            {
                if (!keyValuePair.Value) continue;
                Building building1 = ResourceManager.BuildingsDict[keyValuePair.Key];

                //Skip adding +food buildings for cybernetic races
                if (IsCybernetic && (building1.PlusFlatFoodAmount > 0 || building1.PlusFoodPerColonist > 0)) continue;

                //Skip adding command buildings if planet already has one
                if (!needCommandBuilding && (building1.Name == "Outpost" || building1.Name == "Capital City")) continue;

                bool foundIt = false;

                //Make sure the building isn't already built on this planet
                foreach (Building building2 in BuildingList)
                {
                    if (!building2.Unique) continue;

                    if (building2.Name == building1.Name)
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (foundIt) continue;

                //Make sure the building isn't already being built on this planet
                for (int index = 0; index < ConstructionQueue.Count; ++index)
                {
                    QueueItem queueItem = ConstructionQueue[index];
                    if (queueItem.isBuilding && queueItem.Building.Name == building1.Name && queueItem.Building.Unique)
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (foundIt) continue;

                //Hide Biospheres if the entire planet is already habitable
                if (building1.Name == "Biosphers")
                {
                    bool allHabitable = true;
                    foreach (PlanetGridSquare tile in TilesList)
                    {
                        if (!tile.Habitable)
                        {
                            allHabitable = false;
                            break;
                        }
                    }
                    if (allHabitable) continue;
                }

                //If this is a one-per-empire building, make sure it hasn't been built already elsewhere
                //Reusing fountIt bool from above
                if (building1.BuildOnlyOnce)
                {
                    //Check for this unique building across the empire
                    foreach (Planet planet in Owner.GetPlanets())
                    {
                        //First check built buildings
                        foreach (Building building2 in planet.BuildingList)
                        {
                            if (building2.Name == building1.Name)
                            {
                                foundIt = false;
                                break;
                            }
                        }
                        if (foundIt) break;

                        //Then check production queue
                        foreach (QueueItem queueItem in planet.ConstructionQueue)
                        {
                            if (queueItem.isBuilding && queueItem.Building.Name == building1.Name)
                            {
                                foundIt = true;
                                break;
                            }
                        }
                        if (foundIt) break;
                    }
                    if (foundIt) continue;
                }

                //If the building is still a candidate after all that, then add it to the list!
                BuildingsCanBuild.Add(building1);
            }
        }

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

        public int TotalTurnsInConstruction => ConstructionQueue.Count > 0 ? NumberOfTurnsUntilCompleted(ConstructionQueue.Last) : 0;

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

        public bool BuildingInQueue(string UID)
        {
            for (int index = 0; index < ConstructionQueue.Count; ++index)
            {
                if (ConstructionQueue[index].isBuilding && ConstructionQueue[index].Building.Name == UID)
                    return true;
            }
            return false;
        }

        public bool BuildingExists(string buildingName)
        {
            for (int i = 0; i < BuildingList.Count; ++i)
                if (BuildingList[i].Name == buildingName)
                    return true;
            return BuildingInQueue(buildingName);

        }
        
        public bool CanBuildInfantry()
        {
            for (int i = 0; i < BuildingList.Count; i++)
                if (BuildingList[i].AllowInfantry)
                    return true;
            return false;
        }
    }
}
