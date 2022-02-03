using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI.Compnonents;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class BudgetTests : StarDriveTest
    {
        void CreatePlanets(int extraPlanets)
        {
            CreateUniverseAndPlayerEmpire("Cordrazine");
            AddDummyPlanet(2, 2, 4);
            AddDummyPlanet(1.9f, 1.9f, 4);
            AddDummyPlanet(1.7f, 1.7f, 4);
            for (int x = 0; x < 5; x++)
                AddDummyPlanet(0.1f, 0.1f, 1).ParentSystem.SetExploredBy(Enemy);
            AddHomeWorldToEmpire(Player).ParentSystem.SetExploredBy(Enemy);
            AddHomeWorldToEmpire(Enemy, new Vector2(2000)).ParentSystem.Position = new Vector2(2000);
            UState.Objects.UpdateLists(true);
            AddHomeWorldToEmpire(Enemy);
            for (int x = 0; x < extraPlanets; x++)
                AddDummyPlanetToEmpire(Enemy, 1, 1, 1);
        }

        [TestMethod]
        public void TestBudgetLoad()
        {
            BudgetPriorities budget = new BudgetPriorities(Enemy);
            var budgetAreas = Enum.GetValues(typeof(BudgetPriorities.BudgetAreas));
            foreach (BudgetPriorities.BudgetAreas area in budgetAreas)
            {
                bool found = budget.GetBudgetFor(area) > 0;
                Assert.IsTrue(found, $"{area} not found in budget");
            }
        }

        [TestMethod]
        public void TestTreasuryIsSetToExpectedValues()
        {
            CreatePlanets(extraPlanets: 5);
            var budget = new BudgetPriorities(Enemy);
            int budgetAreas = Enum.GetNames(typeof(BudgetPriorities.BudgetAreas)).Length;

            Assert.IsTrue(budget.Count() == budgetAreas);

            var eAI = Enemy.GetEmpireAI();

            var colonyShip = SpawnShip("Colony Ship", Enemy, Vector2.Zero);
            Enemy.UpdateEmpirePlanets();
            Enemy.UpdateNetPlanetIncomes();
            Enemy.GetEmpireAI().RunEconomicPlanner();

            foreach (var planet in UState.Planets)
            {
                if (planet.Owner != Enemy)
                {
                    float maxPotential = Enemy.MaximumStableIncome;
                    float previousBudget = eAI.ProjectedMoney;
                    planet.Colonize(colonyShip);
                    Enemy.UpdateEmpirePlanets();
                    Enemy.UpdateNetPlanetIncomes();
                    float planetRevenue = planet.Money.PotentialRevenue;
                    Assert.IsTrue(Enemy.MaximumStableIncome.AlmostEqual(maxPotential + planetRevenue, 1f), "MaxStableIncome value was unexpected");
                    eAI.RunEconomicPlanner();
                    float expectedIncrease = planetRevenue * Enemy.data.treasuryGoal * 200;
                    float actualValue = eAI.ProjectedMoney;
                    Assert.IsTrue(actualValue.AlmostEqual(previousBudget + expectedIncrease, 1f), "Projected Money value was unexpected");
                }
            }
        }

        [TestMethod]
        public void TestTaxes()
        {
            CreatePlanets(extraPlanets: 0);

            Enemy.data.TaxRate = 1;
            Enemy.UpdateEmpirePlanets();
            Enemy.UpdateNetPlanetIncomes();
            Enemy.GetEmpireAI().RunEconomicPlanner();
            Assert.IsTrue(Enemy.data.TaxRate < 1, $"Tax Rate should be less than 100% was {Enemy.data.TaxRate * 100}%");

            Enemy.Money = Enemy.GetEmpireAI().ProjectedMoney * 10;
            Enemy.GetEmpireAI().RunEconomicPlanner();
            Assert.IsTrue(Enemy.data.TaxRate <= 0.00001, $"Tax Rate should be zero was {Enemy.data.TaxRate * 100}%");
        }
    }
}