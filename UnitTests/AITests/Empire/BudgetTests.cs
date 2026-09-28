using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI.Components;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.UI;
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
            CreatePlanets(1);
            var budgetAreas = Enum.GetValues(typeof(BudgetPriorities.BudgetAreas));

            Enemy.Universe.P.UseLegacyEspionage = true;
            Enemy.Universe.P.Difficulty = GameDifficulty.Normal;
            BudgetPriorities budget = new BudgetPriorities(Enemy);
            TestBudget(Enemy.Universe.P.UseLegacyEspionage, Enemy.Universe.P.Difficulty);

            Enemy.Universe.P.UseLegacyEspionage = false;
            Enemy.Universe.P.Difficulty = GameDifficulty.Hard;
            budget = new BudgetPriorities(Enemy);
            TestBudget(Enemy.Universe.P.UseLegacyEspionage, Enemy.Universe.P.Difficulty);

            Enemy.Universe.P.UseLegacyEspionage = false;
            Enemy.Universe.P.Difficulty = GameDifficulty.Normal;
            budget = new BudgetPriorities(Enemy);
            TestBudget(Enemy.Universe.P.UseLegacyEspionage, Enemy.Universe.P.Difficulty);


            void TestBudget(bool legacy, GameDifficulty difficulty)
            {
                foreach (BudgetPriorities.BudgetAreas area in budgetAreas)
                {
                    if (area != BudgetPriorities.BudgetAreas.Espionage) // espioinage is inserted to spy area
                    {
                        bool found = !legacy && difficulty == GameDifficulty.Normal ? budget.GetBudgetFor(area) >= 0 : budget.GetBudgetFor(area) > 0;
                        Assert.IsTrue(found, $"{area} not found in budget. Legacy Espionage={legacy}, Game Difficulty={difficulty}");
                    }
                }
            }
        }

        [TestMethod]
        public void TestTreasuryIsSetToExpectedValues()
        {
            CreatePlanets(extraPlanets: 5);
            Enemy.Universe.P.UseLegacyEspionage = true;
            var budget = new BudgetPriorities(Enemy);
            int budgetAreas = Enum.GetNames(typeof(BudgetPriorities.BudgetAreas)).Length;
            Assert.IsTrue(budget.Count() == budgetAreas);

            var eAI = Enemy.AI;

            var colonyShip = SpawnShip("Colony Ship", Enemy, Vector2.Zero);
            Enemy.UpdateEmpirePlanets();
            Enemy.UpdateNetPlanetIncomes();
            Enemy.AI.RunEconomicPlanner();

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

        [TestMethod]
        public void GarrisonUpkeepIsChargedForYourOwnTroopsOnly()
        {
            Planet homeworld = CreateEmpireAndHomeWorld();
            Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
            Player.UpdateNetPlanetIncomes();
            float netBefore = Player.NetIncome;
            int garrisonBefore = homeworld.Troops.NumTroopsHere(Player);

            for (int i = 0; i < 5; ++i)
                Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", Player).TryLandTroop(homeworld));
            for (int i = 0; i < 3; ++i)
                Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", Enemy).TryLandTroop(homeworld));
            Player.UpdateNetPlanetIncomes();

            float garrison = (garrisonBefore + 5) * ShipMaintenance.TroopMaint;
            AssertEqual(0.001f, garrison, Player.TroopCostOnPlanets, "the Troop Maint. row counts our troops, not the invaders");
            AssertEqual(0.001f, homeworld.Money.Maintenance, Player.TotalBuildingMaintenance, "the Building Maint. row is buildings only");
            AssertEqual(0.001f, netBefore - 5 * ShipMaintenance.TroopMaint, Player.NetIncome, "our five new troops cost upkeep");
        }

        [TestMethod]
        public void PlayerBudgetWeightsDoNotDependOnTheEspionageSystem()
        {
            CreateEmpireAndHomeWorld();
            UState.P.Difficulty = GameDifficulty.Normal;
            UState.P.UseLegacyEspionage = true;
            var legacy = new BudgetPriorities(Player);
            UState.P.UseLegacyEspionage = false;
            var modern = new BudgetPriorities(Player);

            AssertGreaterThan(legacy.GetBudgetFor(BudgetPriorities.BudgetAreas.Spy), 0f, "setup: the yaml gives Spy a weight");
            AssertEqual(0f, modern.GetBudgetFor(BudgetPriorities.BudgetAreas.Espionage), "the Espionage key is not a player weight");
            foreach (BudgetPriorities.BudgetAreas area in Enum.GetValues(typeof(BudgetPriorities.BudgetAreas)))
                AssertEqual(0.000001f, legacy.GetBudgetFor(area), modern.GetBudgetFor(area),
                    $"the player's {area} share must be the same under both espionage systems");

            AssertEqual(0f, new BudgetPriorities(Enemy).GetBudgetFor(BudgetPriorities.BudgetAreas.Spy),
                "an AI on Normal under the new espionage system still has no spy weight");
        }

        [TestMethod]
        public void TradeUnderACreditCountsTowardTheTradeAverage()
        {
            Planet homeworld = CreateEmpireAndHomeWorld();
            Player.data.Traits.TaxGoods = true;
            Player.data.Traits.Mercantile = 0f;
            Player.data.TaxRate = 0.25f;

            for (int i = 0; i < 8; ++i)
                Player.TaxGoods(1f, homeworld);

            AssertEqual(0.001f, 2f, Player.AllTimeTradeIncome, "eight deliveries earning a quarter credit each");
        }

        [TestMethod]
        public void TheTradeAverageSurvivesSaveAndLoad()
        {
            Planet homeworld = CreateEmpireAndHomeWorld();
            UState.StarDate = 1042.5f;
            Player.data.Traits.TaxGoods = true;
            Player.data.TaxRate = 0.5f;
            for (int turn = 0; turn < 4; ++turn)
            {
                Player.TaxGoods(10f, homeworld);
                Player.DoMoney();
            }
            float average = Player.AverageTradeIncome;
            AssertGreaterThan(average, 0f, "setup: the freighters must have earned something");

            SavedGame save = Universe.Save("UnitTest.TradeAverage", throwOnError: true);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);

            AssertEqual(0.001f, average, loaded.UState.Player.AverageTradeIncome,
                "the lifetime average must not restart when a game is loaded");
        }

        [TestMethod]
        public void TheTradePanelListsATreatySignedThisTurn()
        {
            CreateUniverseAndPlayerEmpire();
            Player.SignTreatyWith(Enemy, TreatyType.Trade);

            var screen = new BudgetScreen(Universe);
            Game.Manager.AddScreenAndLoadContent(screen);
            try
            {
                Assert.IsTrue(HasLabel(screen, $"   {Enemy.data.Traits.Plural}:"),
                    "the trade panel must list every partner its total counts");
            }
            finally
            {
                Game.Manager.RemoveScreen(screen);
            }
        }

        [TestMethod]
        public void AHomeDefenseLaunchChargesTheCreditFeeOnce()
        {
            CreateEmpireAndHomeWorld();
            Ship defender = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            float fee = defender.GetCost(Player) * Player.DifficultyModifiers.CreditsMultiplier;
            AssertGreaterThan(fee, 0f, "setup: the ship must cost something");

            float moneyBefore = Player.Money;
            Player.ChargeCreditsHomeDefense(defender);

            AssertEqual(0.01f, fee, moneyBefore - Player.Money, "a launch pays the difficulty's credit fee on the ship's cost, once");
        }

        [TestMethod]
        public void ScrappingAMilitaryBuildingRefundsHalfItsCreditFee()
        {
            Planet homeworld = CreateEmpireAndHomeWorld();
            string name = ResourceManager.BuildingsDict.Values.First(b => b.IsMilitary && b.Scrappable).Name;
            Building military = ResourceManager.CreateBuilding(homeworld, name);
            homeworld.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(military, homeworld);
            float refund = military.ActualCost(Player) * Player.DifficultyModifiers.CreditsMultiplier * 0.5f;
            AssertGreaterThan(refund, 0f, "setup: the building must cost something");

            float moneyBefore = Player.Money;
            homeworld.ScrapBuilding(military);

            AssertEqual(0.01f, refund, Player.Money - moneyBefore, "half the credit fee comes back, on the same scale as a ship");
        }

        static bool HasLabel(UIElementContainer container, string text)
        {
            foreach (UIElementV2 e in container.GetElements())
            {
                if (e is SplitElement split && (IsLabel(split.First, text) || IsLabel(split.Second, text)))
                    return true;
                if (IsLabel(e, text) || e is UIElementContainer child && HasLabel(child, text))
                    return true;
            }
            return false;
        }

        static bool IsLabel(UIElementV2 e, string text) => e is UILabel label && label.Text.Text == text;
    }
}