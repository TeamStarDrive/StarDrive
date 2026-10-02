using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    /// <summary>
    /// The governor weighs tax and per-colonist credits at any population, so a small colony builds a money
    /// building once its score clears the threshold. Below a billion the credits weight follows the real
    /// population, reaching the 2 billion floor at a billion with no jump. Flat income does not depend on
    /// population, so it still waits for a billion colonists.
    /// </summary>
    [TestClass]
    public class GovernorMoneyPriorityTests : StarDriveTest
    {
        readonly Planet Colony;

        public GovernorMoneyPriorityTests()
        {
            CreateUniverseAndPlayerEmpire();
            Colony = AddHomeWorldToEmpire(new Vector2(2000), Player);
            Colony.CType = Planet.ColonyType.Core;
            Player.data.TaxRate = 0.25f;

            // the capital is a money building, so a colony of nothing else has no room for another
            string notMoney = ResourceManager.BuildingsDict.Values
                .First(b => !b.IsMoneyBuilding && !b.IsBiospheres && !b.IsMilitary && !b.CanBuildAnywhere).Name;
            Colony.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(ResourceManager.CreateBuilding(Colony, notMoney), Colony);
            Assert.IsTrue(Colony.TotalHabitableTiles > 0 && Colony.MoneyBuildingRatio < 1,
                $"setup: the colony must have tiles and room for money buildings: {Colony.TotalHabitableTiles} {Colony.MoneyBuildingRatio}");
        }

        (float Tax, float Credits, float BuildingIncome) MoneyPrioritiesAt(float billion)
        {
            Colony.Population = billion * 1000f;
            Assert.IsTrue(Colony.Population < Colony.MaxPopulation, $"setup: the colony must have room to grow: {Colony.MaxPopulation}");
            return Colony.TestMoneyPriorities();
        }

        [TestMethod]
        public void AColonyUnderABillionWeighsTaxAndPerColonistCredits()
        {
            var (tax, credits, _) = MoneyPrioritiesAt(0.5f);
            AssertGreaterThan(tax, 0f, "a colony of half a billion weighs tax buildings");
            AssertGreaterThan(credits, 0f, "a colony of half a billion weighs per-colonist credit buildings");
        }

        [TestMethod]
        public void BelowABillionTheCreditsWeightFollowsTheRealPopulation()
        {
            float quarter = MoneyPrioritiesAt(0.25f).Credits;
            float half    = MoneyPrioritiesAt(0.5f).Credits;
            AssertEqual(0.01f, 4f, half / quarter, "twice the colonists, twice as full: four times the weight");

            float justUnder = MoneyPrioritiesAt(0.99f).Credits;
            float atOne     = MoneyPrioritiesAt(1f).Credits;
            AssertEqual(0.03f, 1f, justUnder / atOne, "no jump in the weight at a billion");
        }

        [TestMethod]
        public void FlatIncomeWaitsForABillionColonists()
        {
            AssertEqual(0f, MoneyPrioritiesAt(0.9f).BuildingIncome, "flat income is not weighed below a billion colonists");
            AssertGreaterThan(MoneyPrioritiesAt(1f).BuildingIncome, 0f, "flat income is weighed from a billion colonists");
        }
    }
}
