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
        [StarData] public int PercentAchievable { get; private set; }
        [StarData] BlueprintsTemplate Template;

        public string Name => Template.Name;
        public string LinkedBlueprintsName => Template.LinkTo ?? "";
        public bool Exclusive => Template.Exclusive; // Build only these buildings and remove the rest
        HashSet<string> PlannedBuildings => Template.PlannedBuildings;
        public ColonyType ColonyType => Template.ColonyType;
        bool Completed => PercentCompleted == 100;

        public bool SkipUnresearched => Template.SkipUnresearched;

        public bool IsAchievableCompleted
        {
            get
            {
                int standing = ResearchedBuildingsStanding(P.Buildings.ToArray(), out int researched);
                return standing == researched;
            }
        }

        bool IsHalfAchievableCompleted
        {
            get
            {
                int standing = ResearchedBuildingsStanding(P.Buildings.ToArray(), out int researched);
                return standing * 2 >= researched;
            }
        }

        public bool IsRequired(Building b) => PlannedBuildings.Contains(b.Name);
        public bool IsNotRequired(Building b) => !IsRequired(b);

        public bool OkToBuildTerraformers => PlannedBuildings.Count < P.TileArea / 2 ? IsAchievableCompleted : IsHalfAchievableCompleted;

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
            if (CountCompletion())
                ChangeTemplateIfLinked();
        }

        bool CountCompletion()
        {
            Building[] built = P.Buildings.ToArray();
            int totalRequiredBuilt = built.Count(IsRequired);
            float completion = (float)totalRequiredBuilt / PlannedBuildings.Count;
            int percentCompleted = (int)(completion * 100);
            if (percentCompleted == 100 || !SkipUnresearched)
            {
                PercentCompleted = percentCompleted;
                return Completed;
            }

            int standing = ResearchedBuildingsStanding(built, out int researched);
            PercentCompleted = researched > 0 ? standing * 100 / researched : 0;
            return researched > 0 && standing == researched;
        }

        int ResearchedBuildingsStanding(Building[] built, out int researched)
        {
            researched = 0;
            int standing = 0;
            foreach (string name in PlannedBuildings)
            {
                if (!Owner.IsBuildingUnlocked(name))
                    continue;

                researched++;
                if (built.Any(b => b.Name == name))
                    standing++;
            }

            return standing;
        }

        public void UpdatePercentAchievable()
        {
            var unlockedBuildings = Owner.GetUnlockedBuildings();
            int totalPlannedBuildings = PlannedBuildings.Count;
            int totalCanBuild = unlockedBuildings.Count(IsRequired);
            PercentAchievable = (int)(100 * (float)totalCanBuild / totalPlannedBuildings);
        }

        void ChangeTemplateIfLinked()
        {
            var visited = new HashSet<string> { Name };
            while (ResourceManager.TryGetBlueprints(LinkedBlueprintsName, out BlueprintsTemplate template)
                   && visited.Add(template.Name))
            {
                Template = template;
                ChangeColonyType();
                RefreshPlannedBuildingsWeCanBuild(P.GetBuildingsCanBuild());
                bool done = CountCompletion();
                UpdatePercentAchievable();
                if (!done)
                    break;
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

        public bool ShouldScrapNonRequiredBuilding()
        {
            return Exclusive ? ContainsNotRequiredBuildings() 
                             : P.FreeHabitableTiles == 0 
                                && P.GetBuildingsCanBuild().Any(IsRequired) 
                                && ContainsNotRequiredBuildings();

            bool ContainsNotRequiredBuildings()
            {
                foreach (Building b in P.Buildings)
                {
                    if (b.IsSuitableForBlueprints && IsNotRequired(b))
                        return true;
                }

                return false;
            }
        }
    }
}