using System;
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
        public int NumOrbitalsInTheWorks     => ConstructionQueue.Count(b => b.isOrbital);
        public int NumShipYardsInTheWorks    => ConstructionQueue.Count(s => s.sData != null && s.sData.IsShipyard);
        public bool BiosphereInTheWorks      => BuildingInQueue(Building.BiospheresId);
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
                {
                    turns += ConstructionQueue[i].TurnsUntilComplete;
                }
                return turns;
            }
        }

        // @return Total numbers before ship will be finished if
        //         inserted to the end of the queue.
        public int TurnsUntilQueueComplete(float shipCost)
        {
            if (!HasSpacePort)
                return 9999; // impossible

            int shipTurns = (int)Math.Ceiling((shipCost*ShipBuildingModifier) / Prod.NetMaxPotential);
            int total = shipTurns + TurnsUntilQueueCompleted;
            return Math.Min(999, total);
        }

        public float TotalCostOfTroopsInQueue()
        {
            return ConstructionQueue.Filter(qi => qi.isTroop).Sum(qi => qi.Cost);
        }
    }
}
