using System.IO;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;

namespace UnitTests.Ships;

[TestClass]
public class ModuleLimitOnShipTests : StarDriveTest
{
    const string Conduit = "PowerConduit"; // 1x1
    const string Reactor = "Fusion Reactor"; // 2x2

    readonly ShipHull Hull;
    readonly DesignModuleGrid Grid;

    public ModuleLimitOnShipTests()
    {
        CreateUniverseAndPlayerEmpire();
        Assert.IsTrue(ResourceManager.Hull("Terran/Dreadnought", out Hull), "setup: hull");
        Grid = new DesignModuleGrid(null, "LimitTest", Hull);
    }

    ShipModule ModuleWithLimit(string percent, int size = 1)
    {
        string xml = "<ShipModule><UID>LimitTestModule</UID><XSize>" + size + "</XSize><YSize>" + size + "</YSize>"
                   + "<PercentLimitOnShips>" + percent + "</PercentLimitOnShips></ShipModule>";
        var template = (ShipModule_XMLTemplate)new XmlSerializer(typeof(ShipModule_XMLTemplate)).Deserialize(new StringReader(xml));
        return ShipModule.CreateTemplate(UState, template);
    }

    ShipModule Module(string uid)
    {
        return ShipModule.CreateDesignModule(UState, uid, ModuleOrientation.Normal, 0, null, Hull);
    }

    SlotStruct At(SlotStruct slot, int dx, int dy)
    {
        return Grid.Get(new Point(slot.Pos.X + dx, slot.Pos.Y + dy));
    }

    // a slot where a 2x2 reactor fits, as it does again one slot down and right of it
    SlotStruct RoomForOverlappingReactors()
    {
        ShipModule reactor = Module(Reactor);
        foreach (SlotStruct s in Grid.SlotsList)
        {
            SlotStruct diagonal = At(s, 1, 1);
            if (Grid.ModuleFitsAtSlot(s, reactor) && diagonal != null && Grid.ModuleFitsAtSlot(diagonal, reactor))
                return s;
        }
        Assert.Fail("setup: room for two overlapping reactors");
        return null;
    }

    SlotStruct FreeSlotAwayFrom(SlotStruct block, SlotStruct other = null)
    {
        foreach (SlotStruct s in Grid.SlotsList)
        {
            bool nearBlock = s.Pos.X >= block.Pos.X - 1 && s.Pos.X <= block.Pos.X + 3 && s.Pos.Y >= block.Pos.Y - 1 && s.Pos.Y <= block.Pos.Y + 3;
            if (!nearBlock && s.Root.Module == null && s != other)
                return s;
        }
        Assert.Fail("setup: a free slot");
        return null;
    }

    [TestMethod]
    public void TheLimitIsReadFromTheModuleXml()
    {
        AssertEqual(0.001f, 5f, ModuleWithLimit("5").PercentLimitOnShips);
    }

    [TestMethod]
    public void AModuleWithoutTheTagHasNoLimit()
    {
        AssertEqual(int.MaxValue, ResourceManager.GetModuleTemplate(Conduit).SlotLimitOnShip(1000), "the vanilla conduit");
    }

    [TestMethod]
    public void TheLimitIsTheHullsShareOfSlotsButNeverUnderSixModules()
    {
        AssertEqual(15, ModuleWithLimit("5").SlotLimitOnShip(300), "5% of 300 slots");
        AssertEqual(6, ModuleWithLimit("5").SlotLimitOnShip(88), "5% of 88 slots is 4, raised to six 1x1 modules");
        AssertEqual(7, ModuleWithLimit("10").SlotLimitOnShip(79), "10% of 79 slots rounds down");
        AssertEqual(24, ModuleWithLimit("15", size: 2).SlotLimitOnShip(100), "15% of 100 slots is 15, raised to six 2x2 modules");
        AssertEqual(45, ModuleWithLimit("15", size: 2).SlotLimitOnShip(300), "15% of 300 slots");
    }

    [TestMethod]
    public void StationsAndPlatformsHaveNoLimit()
    {
        Assert.IsTrue(ResourceManager.Hull("Terran/Terran_Station", out ShipHull station), "setup: station hull");
        Assert.IsTrue(ResourceManager.Hull("Platforms/Platform Base", out ShipHull platform), "setup: platform hull");
        ShipModule module = ModuleWithLimit("5");
        AssertEqual(int.MaxValue, ShipDesignScreen.SlotLimitOnShip(module, station), "a station");
        AssertEqual(int.MaxValue, ShipDesignScreen.SlotLimitOnShip(module, platform), "a platform");
        Assert.AreNotEqual(int.MaxValue, ShipDesignScreen.SlotLimitOnShip(module, Hull), "a ship");
    }

    [TestMethod]
    public void TheShipyardRefusesAChangePastTheLimitUnlessTheDesignAlreadyHeldMore()
    {
        Assert.IsTrue(ShipDesignScreen.PastLimitOnShip(limit: 6, before: 6, after: 7), "one past the limit");
        Assert.IsFalse(ShipDesignScreen.PastLimitOnShip(limit: 6, before: 5, after: 6), "up to the limit");
        Assert.IsFalse(ShipDesignScreen.PastLimitOnShip(limit: 6, before: 8, after: 8), "an old design over the limit keeps what it has");
        Assert.IsFalse(ShipDesignScreen.PastLimitOnShip(limit: 6, before: 8, after: 7), "and may shed some");
        Assert.IsTrue(ShipDesignScreen.PastLimitOnShip(limit: 6, before: 8, after: 9), "but takes no more");
    }

    [TestMethod]
    public void ModulesCountTheSlotsTheyCover()
    {
        SlotStruct block = RoomForOverlappingReactors();
        Grid.InstallModule(block, Module(Reactor));
        SlotStruct first = FreeSlotAwayFrom(block);
        Grid.InstallModule(first, Module(Conduit));
        Grid.InstallModule(FreeSlotAwayFrom(block, first), Module(Conduit));
        AssertEqual(4, Grid.SlotsOf(Reactor), "a 2x2 reactor");
        AssertEqual(2, Grid.SlotsOf(Conduit), "two conduits");
    }

    [TestMethod]
    public void AnInstallTakesThePlaceOfEveryModuleUnderIt()
    {
        SlotStruct block = RoomForOverlappingReactors();
        Grid.InstallModule(block, Module(Conduit));
        Grid.InstallModule(At(block, 1, 1), Module(Conduit));
        AssertEqual(0, Grid.SlotsOfAfterInstalls(Conduit, block, Module(Reactor), null, null), "a reactor laid over both conduits");
        AssertEqual(4, Grid.SlotsOfAfterInstalls(Reactor, block, Module(Reactor), null, null), "adds its own 4 slots");
        AssertEqual(3, Grid.SlotsOfAfterInstalls(Conduit, FreeSlotAwayFrom(block), Module(Conduit), null, null),
                    "and a later install elsewhere forgets what the first one covered");
    }

    [TestMethod]
    public void AnInstallTakesThePlaceOfAModuleItOnlyPartlyCovers()
    {
        SlotStruct block = RoomForOverlappingReactors();
        Grid.InstallModule(block, Module(Reactor));
        AssertEqual(4, Grid.SlotsOfAfterInstalls(Reactor, At(block, 1, 1), Module(Reactor), null, null),
                    "a reactor one slot down and right replaces the one it overlaps");
    }

    [TestMethod]
    public void AModuleAndItsMirrorAddUp()
    {
        SlotStruct block = RoomForOverlappingReactors();
        SlotStruct a = FreeSlotAwayFrom(block);
        SlotStruct b = FreeSlotAwayFrom(block, a);
        AssertEqual(2, Grid.SlotsOfAfterInstalls(Conduit, a, Module(Conduit), b, Module(Conduit)), "a conduit and its mirror");
        AssertEqual(1, Grid.SlotsOfAfterInstalls(Conduit, a, Module(Conduit), null, null), "a conduit alone");
    }

    [TestMethod]
    public void ReplacingEveryModuleOfAKindAddsItsSlots()
    {
        SlotStruct block = RoomForOverlappingReactors();
        SlotStruct first = FreeSlotAwayFrom(block);
        Grid.InstallModule(first, Module(Conduit));
        Grid.InstallModule(FreeSlotAwayFrom(block, first), Module(Conduit));
        AssertEqual(2, Grid.SlotsOfAfterReplace("BasicCargoHold", Conduit), "two conduits turned into cargo holds");
        AssertEqual(2, Grid.SlotsOfAfterReplace(Conduit, Conduit), "conduits replaced by conduits add none");
    }
}
