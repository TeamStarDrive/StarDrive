using System.Reflection;
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
        public void AnExporterNeverEndsUpBelowItsImportLineWhateverItsSize()
        {
            Planet exporter = Colony(new Vector2(100_000), maxPop: 3000f, fill: 0.95f);
            Planet importer = Colony(new Vector2(200_000), maxPop: 20000f, fill: 0.1f);

            int belowTheLine = 0;
            for (int size = 2001; size <= 12000; ++size)
            {
                exporter.BasePopPerTile = size * 2f;
                exporter.Population = exporter.MaxPopulation * 0.95f;
                exporter.Population -= exporter.ExportablePop(exporter, importer);
                if (exporter.PopulationRatio < 0.8f)
                    ++belowTheLine;
            }
            AssertEqual(0, belowTheLine, "no world size rounds an exporter down to where it imports its colonists back");
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
            Planet big = Colony(new Vector2(200_000), maxPop: 6000f, fill: 0.79f);

            huge.UpdateIncomingTradeGoods();
            big.UpdateIncomingTradeGoods();
            AssertEqual(20, huge.ColonistsImportSlots, "a 20 billion world filling up takes 20 freighters at once");
            AssertEqual(6, big.ColonistsImportSlots, "a 6 billion world just under its import line still takes one per billion");
        }

        [TestMethod]
        public void AWorldOfUpToFiveBillionTakesFreightersByHowEmptyItIs()
        {
            Planet three = Colony(new Vector2(100_000), maxPop: 3000f, fill: 0.67f);
            Planet five = Colony(new Vector2(200_000), maxPop: 5000f, fill: 0.79f);
            Planet emptyFive = Colony(new Vector2(300_000), maxPop: 5000f, fill: 0.3f);

            three.UpdateIncomingTradeGoods();
            five.UpdateIncomingTradeGoods();
            emptyFive.UpdateIncomingTradeGoods();
            AssertEqual(2, three.ColonistsImportSlots, "a 3 billion world at two thirds full takes 2");
            AssertEqual(2, five.ColonistsImportSlots, "a 5 billion world just under its import line takes 2");
            AssertEqual(5, emptyFive.ColonistsImportSlots, "and at 30% full it takes the most, 5");
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

        [TestMethod]
        public void AFreighterTakesNothingOffAWorldThatIsOverItsCap()
        {
            Ship foodFreighter = SpawnShip("Small Transport", Player, new Vector2(200_000));
            Ship colonyFreighter = SpawnShip("Small Transport", Player, new Vector2(210_000));
            AssertEqual(1f, 5f, foodFreighter.LoadFood(5f), "setup: food aboard");
            AssertEqual(1f, 5f, colonyFreighter.LoadColonists(5f), "setup: colonists aboard");

            AssertEqual(0f, 0f, foodFreighter.UnloadFood(-2f), "a store over its cap takes no food, and gives none back");
            AssertEqual(0f, 0f, colonyFreighter.UnloadColonists(-2f), "an overcrowded world takes no colonists, and gives none back");
            AssertEqual(0f, 5f, foodFreighter.GetFood(), "the food stays aboard");
            AssertEqual(0f, 5f, colonyFreighter.GetColonists(), "the colonists stay aboard");
        }

        [TestMethod]
        public void AnOverloadedFreighterTakesNothingOnAndGivesNothingBack()
        {
            Ship freighter = SpawnShip("Small Transport", Player, new Vector2(200_000));
            AssertEqual(1f, 5f, freighter.LoadFood(5f), "setup: food aboard");
            object hold = typeof(Ship).GetField("Cargo", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(freighter);
            hold!.GetType().GetField("TotalCargo")!.SetValue(hold, freighter.CargoSpaceMax + 5f);

            AssertEqual(0f, 0f, freighter.LoadFood(10f), "a hold already over its space takes nothing on");
            AssertEqual(0f, 5f, freighter.GetFood(), "and gives none of its food back");
        }
    }
}
