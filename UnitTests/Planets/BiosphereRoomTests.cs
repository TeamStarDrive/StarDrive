using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    /// <summary>
    /// Issue #321, Roland's follow-up: a biosphere built to make room for a wanted building has to
    /// go on bare ground, because roofing a tile that already holds a building frees no spot. A
    /// biosphere built for the population it adds may still go over a building.
    /// </summary>
    [TestClass]
    public class BiosphereRoomTests : StarDriveTest
    {
        readonly Planet Colony;
        readonly Building Bio;

        public BiosphereRoomTests()
        {
            CreateUniverseAndPlayerEmpire();
            Colony = AddHomeWorldToEmpire(new Vector2(1000), Player);
            Bio = ResourceManager.GetBuildingTemplate(Building.BiospheresId);
            Player.UnlockEmpireBuilding(Bio.Name);
            Colony.CType = Planet.ColonyType.Core;
            Colony.SetManualCivBudget(1000);
            Colony.UpdateIncomes();
        }

        // Xammar I: dead ground under an outpost or a terraformer, all but `bareTiles` of it
        void OccupyTheColonyLeaving(int bareTiles)
        {
            PlanetGridSquare[] bare = Colony.TilesList.Filter(t => !t.VolcanoHere && !t.LavaHere && t.NoBuildingOnTile);
            for (int i = 0; i < bare.Length; ++i)
            {
                bare[i].SetHabitable(false);
                if (i >= bareTiles)
                    bare[i].PlaceBuilding(ResourceManager.CreateBuilding(Colony, Building.TerraformerId), Colony);
            }

            Colony.UpdateMaxPopulation();
            Colony.UpdatePlanetStatsByRecalculation();
            AssertEqual(0, Colony.FreeHabitableTiles, "setup: the colony must have nowhere to put a new building");
        }

        void SetPopulationRatio(float ratio)
        {
            Colony.Population = Colony.MaxPopulation * ratio;
            Colony.UpdatePlanetStatsByRecalculation();
        }

        QueueItem QueuedBiosphere => Colony.ConstructionQueue.FirstOrDefault(q => q.isBuilding && q.Building.IsBiospheres);

        [TestMethod]
        public void ABiosphereForRoomGoesOnBareGround()
        {
            OccupyTheColonyLeaving(bareTiles: 1);
            SetPopulationRatio(0.5f);
            Assert.IsFalse(Colony.BiosphereCarriesItsPopulation(Bio), "setup: room must be the only reason to build");

            Colony.DoGoverning();

            Assert.IsNotNull(QueuedBiosphere, "setup: a colony with nowhere to build must roof bare ground to make room");
            Assert.IsTrue(QueuedBiosphere.pgs.NoBuildingOnTile, "the biosphere must make room where there is none");
        }

        [TestMethod]
        public void ABiosphereForRoomIsNotBuiltOverABuilding()
        {
            OccupyTheColonyLeaving(bareTiles: 0);
            SetPopulationRatio(0.5f);
            Assert.IsFalse(Colony.BiosphereCarriesItsPopulation(Bio), "setup: room must be the only reason to build");

            Colony.DoGoverning();

            Assert.IsNull(QueuedBiosphere, "a biosphere over a building makes no room, so it must not be built for room");
        }

        [TestMethod]
        public void ABiosphereForPopulationMayGoOverABuilding()
        {
            OccupyTheColonyLeaving(bareTiles: 0);
            float upkeep = Bio.ActualMaintenance(Colony);
            float perColonist = Colony.Money.IncomePerColonist * Colony.Money.TaxRateMultiplier;
            float popPerBiosphere = upkeep / (0.3f * 0.001f * perColonist);
            Colony.BasePopPerTile = popPerBiosphere * 2f / Empire.RacialEnvModifer(Colony.Category, Player);
            Colony.UpdateMaxPopulation();
            SetPopulationRatio(0.9f);
            Assert.IsTrue(Colony.BiosphereCarriesItsPopulation(Bio), "setup: the biosphere's own population must pay for it");

            Colony.DoGoverning();

            Assert.IsNotNull(QueuedBiosphere, "the population a biosphere adds is worth having under a building too");
            Assert.IsFalse(QueuedBiosphere.pgs.NoBuildingOnTile, "setup: the only ground left is under a building");
        }
    }
}
