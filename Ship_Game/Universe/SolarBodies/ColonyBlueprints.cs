using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using static Ship_Game.Planet;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class ColonyBlueprints
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Planet P;
        [StarData] public Array<Building> PlannedBuildingsWeCanBuild { get; private set; }
        [StarData] int PercentCompleted;
        [StarData] BlueprintsTemplate Template;

        public string Name => Template.Name;
        public string LinkedBlueprintsName => Template.LinkTo;
        public bool Exclusive => Template.Exclusive; // Build only these buildings and remove the rest
        HashSet<string> PlannedBuildings => Template.PlannedBuildings;
        ColonyType CtColonyType => Template.ColonyType;
        public bool Completed => PercentCompleted == 100;


        public bool IsRequired(Building b) => PlannedBuildings.Contains(b.Name);
        public bool IsNotRequired(Building b) => !IsRequired(b);

        public ColonyBlueprints(BlueprintsTemplate template, Planet planet, Empire owner)
        {
            Template = template;
            Owner = owner;
            P = planet;
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

        public void RefreshTemplate(BlueprintsTemplate template)
        {
            Template = template;
        }
    }
}