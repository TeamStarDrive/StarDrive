using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI.Components;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class BudgetTests : StarDriveTest
    {
        void CreatePlanets(int extraPlanets)
        {
            CreateUniverseAndPlayerEmpire("Cordrazine");
            AddDummyPlanet(new Vector2(1000), 2, 2, 4);
            AddDummyPlanet(new Vector2(1000), 1.9f, 1.9f, 4);
            AddDummyPlanet(new Vector2(1000), 1.7f, 1.7f, 4);
            for (int x = 0; x < 5; x++)
                AddDummyPlanet(new Vector2(1000), 0.1f, 0.1f, 1).System.SetExploredBy(Enemy);
            AddHomeWorldToEmpire(new Vector2(1000), Player).System.SetExploredBy(Enemy);
            AddHomeWorldToEmpire(new Vector2(2000), Enemy, new Vector2(3000));
            UState.Objects.UpdateLists();
            AddHomeWorldToEmpire(new Vector2(1000), Enemy);
            for (int x = 0; x < extraPlanets; x++)
                AddDummyPlanetToEmpire(new Vector2(1000), Enemy);
        }

        Planet CreateEmpireAndHomeWorld()
        {
            CreateUniverseAndPlayerEmpire("Cordrazine");
            return AddHomeWorldToEmpire(new Vector2(1000), Player);
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

            var eAI = Enemy.AI;

            var colonyShip = SpawnShip("Colony Ship", Enemy, Vector2.Zero);
            Enemy.UpdateEmpirePlanets(FixedSimTime.One);
            Enemy.UpdateNetPlanetIncomes();
            Enemy.AI.RunEconomicPlanner();

            foreach (var planet in UState.Planets)
            {
                if (planet.Owner != Enemy)
                {
                    float maxPotential = Enemy.MaximumStableIncome;
                    float previousBudget = eAI.ProjectedMoney;
                    planet.Colonize(colonyShip);
                    Enemy.UpdateEmpirePlanets(FixedSimTime.One);
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
            Enemy.UpdateEmpirePlanets(FixedSimTime.One);
            Enemy.UpdateNetPlanetIncomes();
            Enemy.AI.RunEconomicPlanner();
            Assert.IsTrue(Enemy.data.TaxRate < 1, $"Tax Rate should be less than 100% was {Enemy.data.TaxRate * 100}%");

            Enemy.Money = Enemy.AI.ProjectedMoney * 10;
            Enemy.AI.RunEconomicPlanner();
            Assert.IsTrue(Enemy.data.TaxRate <= 0.00001, $"Tax Rate should be zero was {Enemy.data.TaxRate * 100}%");
        }

        [TestMethod]
        public void TestTerraformerBudget()
        {
            Planet homeworld = CreateEmpireAndHomeWorld();
            Assert.IsTrue(homeworld.TilesList.Any(t => !t.Habitable), "Homeworld should contain at list 1 uninhabitable tile");
            if (!homeworld.TilesList.Any(t => t.Terraformable))
            {
                PlanetGridSquare tile = homeworld.TilesList.Filter(t => !t.Habitable).First();
                tile.Terraformable = true;
            }

            Player.AddMoney(2000);
            Player.UpdateNetPlanetIncomes();
            Player.AI.RunEconomicPlanner();
            float budget = Player.AI.TerraformBudget = 4; // fake budget for testing
            Building terraformer = ResourceManager.GetBuildingTemplate(Building.TerraformerId);
            float terraformerMaint = terraformer.ActualMaintenance(homeworld);

            // The budget from empire should be at least twice the maint of a terraformer for this test to pass
            // If its not, maybe terraformer maint was increase in xml or something was changed with budgets.xml
            AssertGreaterThan(budget, terraformerMaint * 2,
                $"Terraformer budget form empire {budget} is lower than terraformer maintenance {terraformerMaint}");

            int numTerraformableTiles = homeworld.TilesList.Count(t => t.CanTerraform);
            if (numTerraformableTiles == 0)
            {
                Log.Info($"Tiles which can be terraformed was 0, adding one terraformable tile");
                PlanetGridSquare tile = homeworld.TilesList.Find(t => !t.Habitable);
                tile.Terraformable = true;
            }
            else
            {
                Log.Info($"Terraformable tiles: {numTerraformableTiles}");
            }

            Player.GovernPlanets();
            Player.AutoBuildTerraformers = true;
            Player.UnlockEmpireBuilding(terraformer.Name);
            Player.data.Traits.TerraformingLevel = 3;
            Player.GovernPlanets();

            // We need 2 Terraformers for this test and it should get a minimum of 2
            AssertGreaterThan(homeworld.TerraformerLimit, 1);
            // The budget the planet gets must be like the maint of the terraformer
            // It will be increased differentially when terraformers are built
            AssertEqual(homeworld.TerraformBudget, terraformerMaint);
            Assert.IsTrue(homeworld.TerraformerInTheWorks, "Planet should be building a Terraformer now");
            UState.Debug = true; // to get the debug rush
            homeworld.Construction.RushProduction(0, 10000, rushButton: true);
            Assert.IsTrue(homeworld.TerraformersHere == 1, "Planet should have a built Terraformer");
            Player.GovernPlanets();

            // The budget the planet should now be the maint of 2 terraformers
            AssertEqual(homeworld.TerraformBudget, terraformerMaint*2);
        }
    }
}