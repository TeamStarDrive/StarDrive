using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    /// <summary>
    /// Issue #321: raising the tax rate made governors build biospheres and lowering it made
    /// them scrap the same biospheres, so infrastructure churned with every budget swing.
    /// The tax rate is gone from the decision. A biosphere is now built either to open ground
    /// for a building the colony wants, or because the population it adds can pay its own
    /// upkeep when taxed at BiospherePaybackShare.
    /// </summary>
    [TestClass]
    public class BiosphereDecisionTests : StarDriveTest
    {
        readonly Planet Colony;
        readonly Building Bio;

        public BiosphereDecisionTests()
        {
            CreateUniverseAndPlayerEmpire();
            Colony = AddHomeWorldToEmpire(new Vector2(1000), Player);
            Bio = ResourceManager.GetBuildingTemplate(Building.BiospheresId);
            Colony.UpdateIncomes();
        }

        // Size the biosphere so its upkeep is exactly `ratio` of what the population it adds
        // would earn taxed at 100%. Derived from the live economy rather than assumed, so a
        // content change to upkeep or credits per colonist cannot quietly retune these cases.
        //
        // PopPerBiosphere is the ONLY population a biosphere adds: UpdateMaxPopulation excludes
        // biospheres from PopulationBonus, so the template's MaxPopIncrease contributes nothing.
        void SetBiosphereUpkeepAsShareOfItsPopulationsIncome(float ratio)
        {
            float upkeep = Bio.ActualMaintenance(Colony);
            float perColonist = Colony.Money.IncomePerColonist * Colony.Money.TaxRateMultiplier;
            Assert.IsTrue(upkeep > 0f && perColonist > 0f, "test needs a biosphere upkeep and a taxable income");

            float popPerBiosphere = upkeep / (ratio * 0.001f * perColonist);
            Colony.BasePopPerTile = popPerBiosphere * 2f / Empire.RacialEnvModifer(Colony.Category, Player);
            AssertEqual(0.01f, popPerBiosphere, Colony.PopPerBiosphere(Player), "test setup failed to size the biosphere");
        }

        void SetPopulationRatio(float ratio)
        {
            Colony.Population = Colony.MaxPopulation * ratio;
            Colony.UpdatePlanetStatsByRecalculation();
        }

        // A completed biosphere sets the tile's Biosphere flag and makes it habitable; the
        // Building itself goes into the planet-wide list with no tile of its own.
        void AddFreeBiospheres(int count, bool playerAdded = false)
        {
            int expected = Colony.NumFreeBiospheres + count;
            for (int i = 0; i < count; ++i)
            {
                PlanetGridSquare tile = Colony.TilesList.Find(
                    t => !t.Habitable && !t.VolcanoHere && !t.LavaHere && t.NoBuildingOnTile);
                Assert.IsNotNull(tile, "test planet ran out of uninhabitable tiles for a biosphere");

                Building bio = ResourceManager.CreateBuilding(Colony, Building.BiospheresId);
                bio.IsPlayerAdded = playerAdded;
                tile.PlaceBuilding(bio, Colony); // this registers the building with the planet too
            }

            Colony.UpdateMaxPopulation();
            Colony.UpdatePlanetStatsByRecalculation();
            AssertEqual(expected, Colony.NumFreeBiospheres, "test setup failed to create free biospheres");
        }

        [TestMethod]
        public void ABiosphereIsRefusedWhenItsOwnPopulationCannotPayForIt()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(1.0f);
            SetPopulationRatio(0.9f);

            // The upkeep equals the whole full rate income, so only 0.6 of it may be spent and
            // the roof costs more than the people under it can carry. Counting the template's
            // dead MaxPopIncrease would inflate the income term and wrongly pass this.
            Assert.IsFalse(Colony.BiosphereCarriesItsPopulation(Bio),
                "a biosphere whose population cannot pay its upkeep must not be built for population");
        }

        [TestMethod]
        public void ABiosphereIsBuiltWhenItsOwnPopulationCarriesIt()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f);
            SetPopulationRatio(0.9f);

            Assert.IsTrue(Colony.BiosphereCarriesItsPopulation(Bio),
                "a biosphere the added population pays for several times over must be built");
        }

        [TestMethod]
        public void AColonyWellUnderItsCapDoesNotWantMoreRoom()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f); // pays for itself easily, but nobody needs the room
            SetPopulationRatio(0.5f);

            Assert.IsFalse(Colony.BiosphereCarriesItsPopulation(Bio),
                "population pressure is what asks for more room, affordability only permits it");
        }

        [TestMethod]
        public void AnOverPopulatedColonyStillWantsMoreRoom()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f);
            SetPopulationRatio(1.1f);

            // Past the cap a colony is shrinking, not growing, so any rule that asks whether the
            // colony is growing refuses a biosphere exactly where the roof is most needed.
            Assert.IsTrue(Colony.BiosphereCarriesItsPopulation(Bio),
                "a colony past its population cap must still be allowed more room");
        }

        [TestMethod]
        public void TheDecisionDoesNotMoveWithTheEmpireTaxRate()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f);
            SetPopulationRatio(0.9f);

            Player.data.TaxRate = 0.1f;
            Colony.UpdateIncomes();
            bool atLowTax = Colony.BiosphereCarriesItsPopulation(Bio);

            Player.data.TaxRate = 1.0f;
            Colony.UpdateIncomes();
            bool atHighTax = Colony.BiosphereCarriesItsPopulation(Bio);

            Assert.AreEqual(atLowTax, atHighTax,
                "issue #321: the empire tax rate must not decide whether a biosphere is built");
        }

        [TestMethod]
        public void TheLastFreeBiosphereIsKeptWhileThereIsStillSomethingToBuild()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f);
            AddFreeBiospheres(1);
            SetPopulationRatio(0.1f); // far under the cap, so the roof itself is dead weight

            Assert.IsFalse(Scrap(budget: -0.05f, haveSomethingToBuild: true),
                "scrapping the last free biosphere strands the building the colony wants to put on it");
        }

        [TestMethod]
        public void AFreeBiosphereGoesWhenNothingNeedsTheGroundOrTheRoom()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f);
            AddFreeBiospheres(1);
            SetPopulationRatio(0.1f);

            Assert.IsTrue(Scrap(budget: -0.05f, haveSomethingToBuild: false),
                "a free biosphere nobody needs and the colony cannot pay for is dead weight");
        }

        [TestMethod]
        public void APopulatedColonyKeepsItsBiospheresEvenWithNothingToBuild()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f);
            AddFreeBiospheres(1);
            SetPopulationRatio(0.9f); // the population is living under that roof

            Assert.IsFalse(Scrap(budget: 0f, haveSomethingToBuild: false),
                "a biosphere the population is using must not be scrapped for having no building on it");
        }

        [TestMethod]
        public void ABudgetInTheBlackKeepsEveryFreeBiosphere()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f);
            AddFreeBiospheres(4, playerAdded: true);
            SetPopulationRatio(0.1f); // a batch just went up, the population has not caught up yet

            // Issue #313: the player ordered these by hand and gave the colony a budget that pays
            // for them. That budget arrives with their upkeep already deducted, so anything from
            // zero up means the colony is carrying them and none of them is dead weight.
            Assert.IsFalse(Scrap(budget: 0f, haveSomethingToBuild: false),
                "a colony still in the black is paying for its biospheres, so it keeps them");
        }

        [TestMethod]
        public void SurplusBiospheresGoWhenTheColonyCannotPayForThem()
        {
            SetBiosphereUpkeepAsShareOfItsPopulationsIncome(0.3f);
            AddFreeBiospheres(4);
            SetPopulationRatio(0.1f);

            Assert.IsTrue(Scrap(budget: -0.05f, haveSomethingToBuild: false),
                "a colony in the red still trims the roof nobody is under");
        }

        [TestMethod]
        public void ScrappingABiosphereSpendsTheGovernorsBeforeThePlayers()
        {
            AddFreeBiospheres(1, playerAdded: true);
            AddFreeBiospheres(1);
            AssertEqual(1, Colony.CountBuildings(b => b.IsBiospheres && b.IsPlayerAdded), "test setup");

            // A biosphere has no tile of its own - the Building sits in a planet-wide list - so
            // removing one for a tile picks from the pool. It must not pick the player's while a
            // governor's is available.
            PlanetGridSquare tile = Colony.TilesList.Find(t => t.Biosphere);
            Colony.DestroyBioSpheres(tile, false);

            AssertEqual(1, Colony.CountBuildings(b => b.IsBiospheres && b.IsPlayerAdded),
                "the governor spent a player-built biosphere while one of its own was standing");
            AssertEqual(0, Colony.CountBuildings(b => b.IsBiospheres && !b.IsPlayerAdded),
                "the governor-built biosphere should have been the one removed");
        }

        bool Scrap(float budget, bool haveSomethingToBuild)
            => Colony.ShouldScrapFreeBiosphere(budget, haveSomethingToBuild);
    }
}
