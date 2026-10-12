using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    [TestClass]
    public class ColonistTradeTests : StarDriveTest
    {
        public ColonistTradeTests()
        {
            LoadStarterShips("Small Transport");
            CreateUniverseAndPlayerEmpire();
        }

        // a dummy planet with no tiles holds half its base population per tile
        Planet Colony(Vector2 pos, float maxPop, float fill, bool fed = true)
        {
            Planet p = AddDummyPlanetToEmpire(pos, Player, fertility: 5f, minerals: 1f, maxPop: maxPop * 2f);
            AssertEqual(1f, maxPop, p.MaxPopulation, "setup: the colony's size");
            p.Population = p.MaxPopulation * fill;
            p.Food.Percent = fed ? 1f : 0f;
            p.Prod.Percent = fed ? 0f : 1f;
            p.Res.Percent  = 0f;
            p.FoodHere = fed ? p.Storage.Max : 0f;
            p.UpdateIncomes();
            Assert.AreEqual(!fed, p.ShortOnFood(), "setup: whether the colony runs short of food");
            return p;
        }

        [TestMethod]
        public void AnExporterGivesOnlyThePeopleAboveTheLineWhereItWouldImportThemBack()
        {
            Planet exporter = Colony(new Vector2(100_000), maxPop: 3000f, fill: 0.91f);
            Planet importer = Colony(new Vector2(200_000), maxPop: 10000f, fill: 0.1f);

            AssertEqual(1f, 330f, exporter.ExportablePop(exporter, importer),
                "a world at 91% of 3 billion gives 330 million and stays at the 80% import line");
        }

        [TestMethod]
        public void AWorldRunningOutOfFoodStillGivesPeopleAwayFreely()
        {
            Planet starving = Colony(new Vector2(100_000), maxPop: 3000f, fill: 0.91f, fed: false);
            Planet importer = Colony(new Vector2(200_000), maxPop: 10000f, fill: 0.1f);

            AssertEqual(1f, 600f, starving.ExportablePop(starving, importer),
                "a world about to starve is not held at the import line");
        }

        [TestMethod]
        public void AnExporterOpensOnlyAsManySlotsAsItsSurplusFills()
        {
            Player.data.Traits.PassengerModifier = 20f;
            Planet exporter = Colony(new Vector2(100_000), maxPop: 3000f, fill: 0.91f);

            exporter.UpdateIncomingTradeGoods();
            AssertEqual(1, exporter.ColonistsExportSlots, "a 330 million surplus fills one freighter carrying 400 million");
        }

        [TestMethod]
        public void ABigEmptyWorldTakesAFreighterPerBillionOfItsSize()
        {
            Planet huge = Colony(new Vector2(100_000), maxPop: 20000f, fill: 0.1f);
            Planet normal = Colony(new Vector2(200_000), maxPop: 3000f, fill: 0.67f);

            huge.UpdateIncomingTradeGoods();
            normal.UpdateIncomingTradeGoods();
            AssertEqual(20, huge.ColonistsImportSlots, "a 20 billion world filling up takes 20 freighters at once");
            AssertEqual(3, normal.ColonistsImportSlots, "a 3 billion world at two thirds full still takes 3");
        }

        [TestMethod]
        public void AFreighterUnloadsOnlyThePeopleTheWorldHasRoomFor()
        {
            Player.data.Traits.PassengerModifier = 3f;
            Ship freighter = SpawnShip("Small Transport", Player, new Vector2(200_000));
            float room = freighter.CargoSpaceMax;
            AssertEqual(1f, room * 3f, freighter.LoadColonists(room * 3f), "setup: a full hold carries three times its space in people");

            AssertEqual(1f, room, freighter.UnloadColonists(room), "only the people the world has room for get off");
            AssertEqual(1f, room * 2f, freighter.GetColonists(), "the rest stay aboard");
        }
    }
}
