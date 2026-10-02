using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets
{
    [TestClass]
    public class GovernorScrapGuardsTests : StarDriveTest
    {
        readonly Planet P;

        public GovernorScrapGuardsTests()
        {
            CreateUniverseAndPlayerEmpire();
            P = AddHomeWorldToEmpire(new Vector2(1000), Enemy);
            Enemy.data.TaxRate = 0.25f;
        }

        Building Make(float maintenance, float income = 0, float creditsPerColonist = 0, float taxPercentage = 0,
                      float flatFood = 0, float flatProd = 0, float fertility = 0, int storage = 0, bool anywhere = false,
                      Planet on = null)
        {
            string name = ResourceManager.BuildingsDict.Values.First(t => !t.IsBiospheres && !t.IsMilitary && t.Scrappable
                && !t.IsSpacePort && !t.BuildOnlyOnce && t.PlusTerraformPoints <= 0 && !t.IsCapitalOrOutpost).Name;
            Building b = ResourceManager.CreateBuilding(on ?? P, name);
            b.Maintenance = maintenance;
            b.Income = income;
            b.CreditsPerColonist = creditsPerColonist;
            b.PlusTaxPercentage = taxPercentage;
            b.PlusFlatFoodAmount = flatFood;
            b.PlusFoodPerColonist = 0;
            b.PlusFlatProductionAmount = flatProd;
            b.PlusProdPerColonist = 0;
            b.PlusProdPerRichness = 0;
            b.StorageAdded = storage;
            b.MaxFertilityOnBuild = fertility;
            b.IncreaseRichness = 0;
            b.CanBuildAnywhere = anywhere;
            return b;
        }

        Building Place(Building b, Planet on = null)
        {
            on ??= P;
            PlanetGridSquare tile = on.TilesList.First(t => t.NoBuildingOnTile && !t.VolcanoHere);
            tile.SetHabitable(!b.CanBuildAnywhere);
            tile.PlaceBuilding(b, on);
            on.UpdateIncomes();
            return b;
        }

        Building Place(float maintenance, float income = 0, float flatFood = 0, float flatProd = 0, int storage = 0, bool anywhere = false)
            => Place(Make(maintenance, income, flatFood: flatFood, flatProd: flatProd, storage: storage, anywhere: anywhere));

        bool ReplaceMayTake(Building b, Planet on = null)
        {
            on ??= P;
            return on.SuitableForScrap(b, overBudget: false, on.Storage.MostGoodsInStorage, scrapZeroMaintenance: true, replacing: true);
        }

        bool OverBudgetMayScrap(Building b)
            => P.SuitableForScrap(b, overBudget: true, P.Storage.MostGoodsInStorage, scrapZeroMaintenance: false, replacing: false);

        void AddBlueprints(string planned)
        {
            var template = new BlueprintsTemplate("test", false, null, new HashSet<string> { planned }, Planet.ColonyType.Colony);
            P.AddBlueprints(template, Enemy);
        }

        [TestMethod]
        public void TheNetCostOfABuildingIsTheRevenueItAddsOrTakesAway()
        {
            Building b = Make(maintenance: 2f, income: 1f, creditsPerColonist: 0.3f, taxPercentage: 0.4f);
            P.UpdateIncomes();
            float without = P.Money.GrossRevenue;
            float planned = P.Money.NetCostOf(b);

            Place(b);
            float with = P.Money.GrossRevenue;
            float standing = P.Money.NetCostOf(b, standing: true);

            float expected = b.ActualMaintenance(P) - (with - without);
            AssertEqual(0.0001f, expected, planned, "a planned building is priced on the revenue it would add");
            AssertEqual(0.0001f, expected, standing, "a standing building is priced on the revenue its removal would lose");
        }

        [TestMethod]
        public void AnAiColonyKeepsABuildingThatPaysForItself()
        {
            Building resort = Place(maintenance: 0.5f, income: 5f);
            Assert.IsTrue(P.Money.NetCostOf(resort, standing: true) < 0, "setup: at 25% tax the building must pay for itself");

            Assert.IsFalse(ReplaceMayTake(resort), "the governor offered a building that pays for itself for replacement");
            Assert.IsFalse(OverBudgetMayScrap(resort), "an over budget governor scrapped a building that pays for itself");
        }

        [TestMethod]
        public void APlayerOnAManualTaxRateJudgesAMoneyBuildingAtNoLessThanTheStartingTaxRate()
        {
            Planet home = AddHomeWorldToEmpire(new Vector2(5000), Player);
            Assert.IsFalse(Player.AutoTaxes, "setup: the player sets the tax rate by hand");
            Player.data.TaxRate = 0.01f;
            Building resort = Place(Make(maintenance: 0.5f, income: 5f, on: home), home);
            Assert.IsTrue(home.Money.NetCostOf(resort, standing: true) > 0, "setup: at 1% tax the building must not pay for itself");

            Assert.IsFalse(ReplaceMayTake(resort, home), "a low manual tax rate exposed a building that pays at normal taxes");
        }

        [TestMethod]
        public void ABuildingThatPaysOnlyAboveTheStartingTaxRateIsKeptWhileTaxesAreThatHigh()
        {
            Building b = Place(maintenance: 1f, income: 5f);
            float upkeepPerPoint = b.ActualMaintenance(P);
            float revenueAtStart = upkeepPerPoint - P.Money.NetCostOf(b, standing: true);
            Enemy.data.TaxRate = 0.6f;
            P.UpdateIncomes();
            float revenueAt60 = upkeepPerPoint - P.Money.NetCostOf(b, standing: true);
            b.Maintenance = (revenueAtStart + revenueAt60) * 0.5f / upkeepPerPoint;

            Assert.IsTrue(b.ActualMaintenance(P) > revenueAtStart, "setup: at the starting tax rate the building must not pay for itself");
            Assert.IsTrue(P.Money.NetCostOf(b, standing: true) < 0, "setup: at 60% tax the building must pay for itself");

            Assert.IsFalse(ReplaceMayTake(b), "the starting tax rate is a floor, not the rate a building is judged at");
        }

        [TestMethod]
        public void AnAiColonyJudgesAMoneyBuildingAtNoLessThanTheStartingTaxRate()
        {
            Enemy.data.TaxRate = 0f;
            Building resort = Place(maintenance: 0.5f, income: 5f);
            Assert.IsTrue(P.Money.NetCostOf(resort, standing: true) > 0, "setup: at 0% tax the building earns nothing");

            Assert.IsFalse(ReplaceMayTake(resort), "an AI colony offered a building that pays at normal taxes for replacement");
        }

        [TestMethod]
        public void AnAiMoneyBuildingThatDoesNotPayAtTheStartingTaxRateCanBeReplaced()
        {
            Enemy.data.TaxRate = 0f;
            Building b = Place(maintenance: 5f, income: 5f);
            Assert.IsTrue(P.Money.NetCostOf(b, standing: true, EmpireData.StartingTaxRate) > 0,
                          "setup: at the starting tax rate the building must not pay for itself");

            Assert.IsTrue(ReplaceMayTake(b), "an AI colony locked in a building that does not pay even at normal taxes");
        }

        [TestMethod]
        public void AnOverBudgetColonyDoesNotScrapABuildingThatCostsNothing()
        {
            Building free = Place(maintenance: 0f);
            Assert.IsFalse(OverBudgetMayScrap(free), "scrapping a building with no upkeep does nothing for the budget");
        }

        [TestMethod]
        public void AFarmTheColonyLivesOnIsKept()
        {
            Assert.IsTrue(P.NonCybernetic, "setup: the food guard reads food on an organic colony");
            P.SetBaseFertility(2f, 2f);
            Building farm = Place(maintenance: 1f, flatFood: 1000f);
            Assert.IsTrue(P.Fertility > 1.5f, "setup: the colony needs a fertility well above 1");

            Assert.IsFalse(ReplaceMayTake(farm), "the governor offered the farm the colony lives on for replacement");
        }

        [TestMethod]
        public void AFarmIsJudgedWithoutTheFertilityItGives()
        {
            Assert.IsTrue(P.NonCybernetic, "setup: the food guard reads food on an organic colony");
            P.SetBaseFertility(2f, 2f);
            P.UpdateIncomes();
            float surplus = P.Food.NetMaxPotential;
            Assert.IsTrue(surplus > 1f, "setup: the colony needs a food surplus of its own");

            Building farm = Place(Make(maintenance: 1f, flatFood: surplus, fertility: 1.5f));
            Assert.IsFalse(ReplaceMayTake(farm), "the governor offered a farm whose fertility feeds the colony for replacement");
        }

        [TestMethod]
        public void AMineACyberneticColonyLivesOnIsKept()
        {
            Enemy.data.Traits.Cybernetic = 1;
            P.MineralRichness = 2f;
            Building mine = Place(maintenance: 1f, flatProd: 1000f);
            Assert.IsTrue(P.IsCybernetic, "setup: the guard reads production on a cybernetic colony");

            Assert.IsFalse(ReplaceMayTake(mine), "the governor offered the mine a cybernetic colony lives on for replacement");
        }

        [TestMethod]
        public void ABarrenColonyCanLetGoOfASpareFarm()
        {
            Assert.IsTrue(P.NonCybernetic, "setup: the food guard reads food on an organic colony");
            P.SetBaseFertility(0f, 0f);
            Place(maintenance: 1f, flatFood: 1000f);
            Building spare = Place(maintenance: 1f, flatFood: 1f);

            Assert.IsTrue(ReplaceMayTake(spare), "a spare farm on a barren colony with food to spare was locked in place");
        }

        [TestMethod]
        public void ReplacingDoesNotThrowAwayStoredGoods()
        {
            Building silo = Place(maintenance: 1f, storage: 500);
            P.FoodHere = P.Storage.Max;
            P.ProdHere = P.Storage.Max;

            Assert.IsFalse(ReplaceMayTake(silo), "the governor offered storage that is holding goods for replacement");
        }

        [TestMethod]
        public void ReplacingLeavesABuildAnywhereBuildingOnGroundNothingElseCanUse()
        {
            Building b = Place(maintenance: 1f, anywhere: true);
            Assert.IsFalse(ReplaceMayTake(b), "a replacement could not be placed on the uninhabitable tile this one frees");
        }

        [TestMethod]
        public void BlueprintsStillClearAnUnplannedBuildingThatPaysForItself()
        {
            Building resort = Place(maintenance: 0.5f, income: 5f);
            AddBlueprints(ResourceManager.BuildingsDict.Values.First(t => t.Name != resort.Name).Name);

            Assert.IsTrue(ReplaceMayTake(resort), "blueprints ask for the plan, so a building outside it goes whatever it earns");
        }

        [TestMethod]
        public void APlannedBuildingThatPaysForItselfSurvivesOverBudget()
        {
            Building resort = Place(maintenance: 0.5f, income: 5f);
            AddBlueprints(resort.Name);

            Assert.IsFalse(OverBudgetMayScrap(resort), "an over budget governor scrapped a planned building that pays for itself");
            Assert.IsFalse(ReplaceMayTake(resort), "the governor offered a planned building for replacement");
        }
    }
}
