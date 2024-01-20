using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class ColonyBlueprints
    {
        [StarData] public readonly string Name;
        [StarData] readonly Empire Owner;
        [StarData] readonly Planet P;
        [StarData] public readonly string LinkedBlueprintsName = ""; // Switch to the linked blueprints when this is completed
        [StarData] public readonly bool Exclusive; // Build only these buildings and remove the rest
        [StarData] readonly HashSet<string> PlannedBuildings;
        [StarData] public Array<Building> PlannedBuildingsWeCanBuild { get; private set; }
        [StarData] int PercentCompleted;

        public bool Completed => PercentCompleted == 100;

        public bool IsRequired(Building b) => PlannedBuildings.Contains(b.Name);
        public bool IsNotRequired(Building b) => !IsRequired(b);

        public ColonyBlueprints(string name, Empire owner, Planet planet,
            HashSet<string> plannedBuildings, string linkedBlueprintName, bool exclusive)
        {
            Name = name;
            Owner = owner;
            P = planet;
            PlannedBuildings = plannedBuildings;
            UpdateCompletion();
            LinkedBlueprintsName = linkedBlueprintName;
            Exclusive = exclusive;
        }

        public void UpdateCompletion()
        {
            int totalBuiltBuildings = P.Buildings.Length;
            float completion = 0;
            if (totalBuiltBuildings > 0)
            {
                int numBuildingsbuilt = 0;
                foreach (Building b in P.Buildings)
                {
                    if (PlannedBuildings.Contains(b.Name))
                        numBuildingsbuilt++;
                }

                completion = numBuildingsbuilt / totalBuiltBuildings;
            }

            PercentCompleted = (int)(completion * 100);
        }

        public void RefreshPlannedBuildingsWeCanBuild(IReadOnlyList<Building> buildingCanBuild)
        {
            PlannedBuildingsWeCanBuild.Clear();
            if (!P.HasOutpost && !P.HasCapital)
                return;

            for (int i = 0; i < buildingCanBuild.Count; i++)
            {
                Building building = buildingCanBuild[i];
                if (PlannedBuildings.Contains(building.Name))
                    PlannedBuildingsWeCanBuild.Add(building);
            }
        }

        public bool BuildingSuitableForScrap(Building b)
        {
            return Exclusive && Completed && !IsRequired(b);
        }

        public bool ShouldScrapNonRequiredBuilding()
        {
            if (Exclusive && (Completed || PlannedBuildings.Count > 0))
            {
                foreach (Building b in P.Buildings)
                {
                    if (b.IsSuitableForBlueprints && IsNotRequired(b))
                        return true;
                }
            }

            return false;
        }
    }
}