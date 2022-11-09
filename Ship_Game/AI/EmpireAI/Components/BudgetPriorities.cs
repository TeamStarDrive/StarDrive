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
            Terraform
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
            foreach ( var budget in budgetSettings)
            {
                bool isAll = budget.PortraitName.Equals("All", StringComparison.InvariantCultureIgnoreCase);
                bool isUs = budget.PortraitName.Equals(empire?.Name, StringComparison.InvariantCultureIgnoreCase);
                if (isAll || isUs)
                {
                    foreach (var area in budget.Budgets)
                        budgets[area.Key] = area.Value;
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