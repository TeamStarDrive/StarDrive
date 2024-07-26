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
        {
            Budgets = LoadBudgetSettings(empire);
            Total = Budgets.Values.Sum();
        }

        public float GetBudgetFor(BudgetAreas area) => Budgets.TryGetValue(area, out float budget) ? budget / Total : 0;

        Map<BudgetAreas, float> LoadBudgetSettings(Empire empire)
        {
            Map<BudgetAreas, float> budgets = new();
            Array<BudgetSettings> budgetSettings = YamlParser.DeserializeArray<BudgetSettings>("Budgets.yaml");
            // Apply setting to "ALL" first. Apply specific empire settings next
            budgetSettings.Sort(i => i.PortraitName.Equals("All", StringComparison.InvariantCultureIgnoreCase));
            foreach (var budget in budgetSettings)
            {
                bool isAll = budget.PortraitName.Equals("All", StringComparison.InvariantCultureIgnoreCase);
                bool isUs = budget.PortraitName.Equals(empire?.Name, StringComparison.InvariantCultureIgnoreCase);
                if (isAll || isUs)
                {
                    // When using new espionage system, normal game difficuly will not have spy budget.
                    // They will use only the "free" budget
                    // Also - "Spy" area budget is used for both systems
                    foreach (var area in budget.Budgets)
                    {
                        if (area.Key == BudgetAreas.Spy && empire.Universe.P.UseLegacyEspionage)
                            budgets[area.Key] = empire.Universe.P.Difficulty == GameDifficulty.Normal ? 0 : area.Value;
                        else if (area.Key == BudgetAreas.Espionage && !empire.Universe.P.UseLegacyEspionage)
                            budgets[BudgetAreas.Spy] = area.Value;
                        else
                            budgets[area.Key] = area.Value;
                    }

                    if (!budgets.TryGetValue(BudgetAreas.Spy, out _))
                        budgets[BudgetAreas.Spy] = 0;  // support mods with no Espionage in their yaml
                }
            }

            return budgets;
        }

        [StarDataType]
        public class BudgetSettings
        {
            [StarData] public readonly string PortraitName;
            [StarData] public readonly Map<BudgetAreas, float> Budgets;
        }
    }
}