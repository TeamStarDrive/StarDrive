using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    /// <summary>
    /// Which tile the governor picks for a biosphere (issue #312) and for a terraformer. Both
    /// picks used to hand back tiles the caller could not use: the biosphere pick could name a
    /// tile Enqueue then refuses, stalling the colony forever, and the terraformer pick was
    /// random, so it competed with biospheres for ground and moved on every reload.
    /// </summary>
    [TestClass]
    public class ColonyTilePickingTests : StarDriveTest
    {
        readonly Planet P;
        readonly Building Bio;
        readonly Building Terraformer;

        public ColonyTilePickingTests()
        {
            CreateUniverseAndPlayerEmpire();
            P = AddHomeWorldToEmpire(new Vector2(1000), Player);
            Bio = ResourceManager.GetBuildingTemplate(Building.BiospheresId);
            Terraformer = ResourceManager.GetBuildingTemplate(Building.TerraformerId);
        }

        PlanetGridSquare[] Uninhabitable => P.TilesList.Filter(t => !t.Habitable && !t.VolcanoHere && !t.LavaHere && t.NoBuildingOnTile);

        // A homeworld already carries uninhabitable tiles of its own, so every one of them is
        // normalized here - otherwise a stray tile answers the pick and the test proves nothing.
        void MakeUninhabitable(int count, bool terraformable)
        {
            PlanetGridSquare[] tiles = P.TilesList.Filter(t => !t.VolcanoHere && !t.LavaHere && t.NoBuildingOnTile);
            for (int i = 0; i < count; ++i)
                tiles[i].SetHabitable(false);

            foreach (PlanetGridSquare t in tiles)
                if (!t.Habitable)
                    t.Terraformable = terraformable;

            P.UpdatePlanetStatsByRecalculation();
            Assert.IsTrue(Uninhabitable.Length >= count, "test setup failed to free up tiles");
        }

        [TestMethod]
        public void TheBiosphereTileIsOneEnqueueWillAccept()
        {
            MakeUninhabitable(4, terraformable: false);
            PlanetGridSquare[] free = Uninhabitable;
            Assert.IsTrue(free.Length >= 2, "test needs a couple of uninhabitable tiles");

            // Occupy every candidate but the last, the way a colony mid-build looks: the first
            // carries a queued item, which is what Enqueue refuses and the old pick ignored.
            Assert.IsTrue(P.Construction.Enqueue(Bio, free[0]), "could not queue the blocking biosphere");
            for (int i = 1; i < free.Length - 1; ++i)
                free[i].PlaceBuilding(ResourceManager.CreateBuilding(P, Building.BiospheresId), P);

            Assert.IsTrue(P.Construction.Enqueue(Bio, PickBiosphereTile()),
                "the governor picked a tile Enqueue refuses, so the colony would stall forever");
        }

        [TestMethod]
        public void TheBiosphereTakesBareGroundOverAnOccupiedTile()
        {
            MakeUninhabitable(4, terraformable: false);
            PlanetGridSquare[] free = Uninhabitable;
            Assert.IsTrue(free.Length >= 2, "test needs at least two uninhabitable tiles");

            // A biosphere is allowed onto a tile that already carries a build-anywhere building,
            // but roofing one buys the colony no room, and having no room is why it is building.
            free[0].PlaceBuilding(ResourceManager.CreateBuilding(P, Building.TerraformerId), P);

            Assert.IsTrue(PickBiosphereTile().NoBuildingOnTile,
                "a biosphere over an occupied tile leaves the colony exactly as cramped as before");
        }

        [TestMethod]
        public void TheBiosphereLeavesTerraformableGroundAlone()
        {
            MakeUninhabitable(4, terraformable: true);
            PlanetGridSquare[] free = Uninhabitable;
            free[free.Length - 1].Terraformable = false; // one patch of permanently dead ground
            Player.UnlockEmpireBuilding(Terraformer.Name);
            Player.data.Traits.TerraformingLevel = 2;

            Assert.IsTrue(Player.CanTerraformPlanetTiles, "test needs tile terraforming available");
            Assert.IsFalse(PickBiosphereTile().Terraformable,
                "a biosphere should take dead ground and leave terraformable tiles to the terraformer");
        }

        [TestMethod]
        public void TheBiosphereUsesTerraformableGroundBeforeTerraformingIsPossible()
        {
            MakeUninhabitable(4, terraformable: true);
            PlanetGridSquare[] free = Uninhabitable;
            free[free.Length - 1].Terraformable = false; // dead ground, further down the list
            Player.UnlockEmpireBuilding(Terraformer.Name);
            Player.data.Traits.TerraformingLevel = 1; // level 1 clears volcanoes, it cannot turn a tile

            // Owning a terraformer is not the same as being able to terraform a tile, which needs
            // level 2. Until then there is nothing to save the terraformable ground for, so the
            // biosphere takes the nearest tile rather than walking past it to the dead one.
            Assert.IsFalse(Player.CanTerraformPlanetTiles, "test needs tile terraforming out of reach");
            Assert.AreSame(free[0], PickBiosphereTile(),
                "with no tile terraforming yet the biosphere should just take the first free tile");
        }

        [TestMethod]
        public void TheTerraformerTakesTerraformableGround()
        {
            MakeUninhabitable(4, terraformable: false);
            PlanetGridSquare[] free = Uninhabitable;
            free[free.Length - 1].Terraformable = true;

            Assert.IsTrue(PickTerraformerTile(free).Terraformable,
                "the terraformer should stand on the ground it can actually change");
        }

        [TestMethod]
        public void TheTerraformerTilePickIsRepeatable()
        {
            MakeUninhabitable(5, terraformable: false);
            PlanetGridSquare[] free = Uninhabitable;

            PlanetGridSquare first = PickTerraformerTile(free);
            for (int i = 0; i < 8; ++i)
                Assert.AreSame(first, PickTerraformerTile(free),
                    "the same save must lay a colony out the same way twice");
        }

        PlanetGridSquare PickBiosphereTile()
        {
            PlanetGridSquare tile = P.PreferredBiosphereTile(Bio);
            Assert.IsNotNull(tile, "the governor found no tile for a biosphere at all");
            return tile;
        }

        PlanetGridSquare PickTerraformerTile(PlanetGridSquare[] tiles) => P.PickTileForTerraformer(tiles);
    }
}
