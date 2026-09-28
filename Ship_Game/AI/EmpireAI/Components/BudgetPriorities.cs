using System;
using System.Linq;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.AI.Components
{
    public class BudgetPriorities
    {
        readonly float Total;
        readonly Map<BudgetAreas, float> Budgets;

        public enum BudgetAreas
        {
            Defense,
            SSP,
            Build,
            Spy,
            Colony,
            Savings,
            Terraform,
            Espionage
        }

        public int Count()
        {
            int populatedAreas = 0;
            foreach (var item in Budgets)
            {
                if (item.Value > 0)
                    populatedAreas++;
            }
            return populatedAreas;
        }

        public BudgetPriorities(Empire empire)
            : this(empire, YamlParser.DeserializeArray<BudgetSettings>("Budgets.yaml"))
        {
        }

        internal BudgetPriorities(Empire empire, Array<BudgetSettings> budgetSettings)
        {
            Budgets = LoadBudgetSettings(empire, budgetSettings);
            Total = Budgets.Values.Sum();
        }

        public float GetBudgetFor(BudgetAreas area) => Budgets.TryGetValue(area, out float budget) ? budget / Total : 0;

        static Map<BudgetAreas, float> LoadBudgetSettings(Empire empire, Array<BudgetSettings> budgetSettings)
        {
            Map<BudgetAreas, float> budgets = new();
            foreach (BudgetSettings settings in budgetSettings)
            {
                if (!settings.PortraitName.Equals("All", StringComparison.InvariantCultureIgnoreCase))
                {
                    Log.Warning($"Budgets.yaml: only the All block is read, '{settings.PortraitName}' is ignored");
                    continue;
                }

                foreach (var area in settings.Budgets)
                {
                    if (area.Key != BudgetAreas.Espionage)
                        budgets[area.Key] = area.Value;
                }

                if (!empire.isPlayer && empire.NewEspionageEnabled)
                {
                    float espionage = settings.Budgets.TryGetValue(BudgetAreas.Espionage, out float weight) ? weight : 1;
                    budgets[BudgetAreas.Spy] = empire.Universe.P.Difficulty == GameDifficulty.Normal ? 0 : espionage;
                }
            }

            return budgets;
        }

        [StarDataType]
        public class BudgetSettings
        {
            [StarData] public readonly string PortraitName;
            [StarData] public readonly Map<BudgetAreas, float> Budgets;

            public BudgetSettings()
            {
            }

            public BudgetSettings(string portraitName, Map<BudgetAreas, float> budgets)
            {
                PortraitName = portraitName;
                Budgets = budgets;
            }
        }
    }
}