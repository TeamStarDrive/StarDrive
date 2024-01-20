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
        [StarData] readonly HashSet<string> PlannedBuildings;
        [StarData] public HashSet<string> PlannedBuildingsWeCanBuild { get; private set; }
        [StarData] int PercentCompleted;

        public bool Completed => PercentCompleted == 100;

        public ColonyBlueprints(string name, Empire owner, Planet planet, HashSet<string> plannedBuildings)
        {
            Name = name;
            Owner = owner;
            P = planet;
            PlannedBuildings = plannedBuildings;
            UpdateCompletion();
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

        public void RefreshPlannedBuildingsWeCanBuild(Array<Building> buildingCanBuild)
        {
            PlannedBuildingsWeCanBuild.Clear();
            if (!P.HasOutpost && !P.HasCapital)
                return;

            for (int i = 0; i < buildingCanBuild.Count; i++)
            {
                Building building = buildingCanBuild[i];
                if (PlannedBuildings.Contains(building.Name))
                    PlannedBuildingsWeCanBuild.Add(building.Name);
            }
        }
    }
}