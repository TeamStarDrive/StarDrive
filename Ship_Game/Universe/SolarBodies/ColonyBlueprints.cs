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
        [StarData] public int PercentCompleted { get; private set; }
        [StarData] public int PercentAchivable { get; private set; }
        [StarData] BlueprintsTemplate Template;

        public string Name => Template.Name;
        public string LinkedBlueprintsName => Template.LinkTo;
        public bool Exclusive => Template.Exclusive; // Build only these buildings and remove the rest
        HashSet<string> PlannedBuildings => Template.PlannedBuildings;
        ColonyType ColonyType => Template.ColonyType;
        public bool Completed => PercentCompleted == 100;


        public bool IsRequired(Building b) => PlannedBuildings.Contains(b.Name);
        public bool IsNotRequired(Building b) => !IsRequired(b);

        [StarDataConstructor]
        public ColonyBlueprints() { }

        public ColonyBlueprints(BlueprintsTemplate template, Planet planet, Empire owner)
        {
            Owner = owner;
            P = planet;
            PlannedBuildingsWeCanBuild = new();
            ChangeTemplate(template);
        }

        public void ChangeTemplate(BlueprintsTemplate template) 
        {
            Template = template;
            OnTemplateChanged();
        }

        void OnTemplateChanged()
        {
            ChangeColonyType();
            RefreshPlannedBuildingsWeCanBuild(P.GetBuildingsCanBuild());
            UpdateCompletion();
            UpdatePercentAchievable();
        }

        void ChangeColonyType()
        {
            if (ColonyType != ColonyType.Colony)
                P.CType = ColonyType;
        }

        public void UpdateCompletion()
        {
            int totalRequiredBuilt = P.Buildings.ToArray().Count(IsRequired);
            float completion = (float)totalRequiredBuilt / PlannedBuildings.Count;
            PercentCompleted = (int)(completion * 100);

            if (Completed)
                ChangeTemplateIfLinked();
        }

        public void UpdatePercentAchievable()
        {
            var unlockedBuildings = Owner.GetUnlockedBuildings();
            int totalPlannedBuildings = PlannedBuildings.Count;
            int totalCanBuild = unlockedBuildings.Count(IsRequired);
            PercentAchivable = (int)(100 * (float)totalCanBuild / totalPlannedBuildings);
        }

        void ChangeTemplateIfLinked()
        {
            if (LinkedBlueprintsName != null
                && ResourceManager.TryGetBlueprints(LinkedBlueprintsName, out BlueprintsTemplate template))
            {
                ChangeTemplate(template);
                // need to verify non cyclic plan links
            }
        }

        public void RefreshPlannedBuildingsWeCanBuild(IReadOnlyList<Building> buildingCanBuild)
        {
            PlannedBuildingsWeCanBuild.Clear();
            if (!P.HasOutpost && !P.HasCapital)
                return;

            for (int i = 0; i < buildingCanBuild.Count; i++)
            {
                Building building = buildingCanBuild[i];
                if (IsRequired(building))
                    PlannedBuildingsWeCanBuild.Add(building);
            }
        }

        public bool BuildingSuitableForScrap(Building b)
        {
            return !IsRequired(b);
        }

        public bool ShouldScrapNonRequiredBuilding()
        {
            if (Exclusive)
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