using System;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.AI.Compnonents
{
    public class BudgetPriorities
    {
        float Total = 0;

        Map<BudgetAreas, float> Budgets;

        public enum BudgetAreas
        {
            Defense,
            SSP,
            Build,
            Spy,
            Colony,
            Savings
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
            Budgets = new Map<BudgetAreas, float>();
            LoadBudgetSettings(empire);
            Total = 0;

            foreach (var budget in Budgets.Values)
                Total += budget;
        }

        public float GetBudgetFor(BudgetAreas area) => Budgets.TryGetValue(area, out float budget) ? budget / Total : 0;

        public void LoadBudgetSettings(Empire empire)
        {
            Array<BudgetSettings> budgetSettings = YamlParser.DeserializeArray<BudgetSettings>("Budgets.yaml");
            // Apply setting to "ALL" first. Apply specific empire settings next
            budgetSettings.Sort(i => i.PortraitName.Equals("All", StringComparison.InvariantCultureIgnoreCase));
            foreach ( var budget in budgetSettings)
            {
                bool isAll = budget.PortraitName.Equals("All", StringComparison.InvariantCultureIgnoreCase);
                bool isUs = budget.PortraitName.Equals(empire?.Name, StringComparison.InvariantCultureIgnoreCase);
                if (isAll || isUs)
                {
                    foreach (var area in budget.Budgets)
                        Budgets[area.Key] = area.Value;
                }
            }
        }

        [StarDataType]
        public class BudgetSettings
        {
            [StarData] public readonly string PortraitName;
            [StarData] public readonly Map<BudgetAreas, float> Budgets;
        }
    }
}