using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;

namespace UnitTests.Ships;

[TestClass]
public class CivilianUpkeepTests : StarDriveTest
{
    public CivilianUpkeepTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    // every slot of the hull holds a 1x1 cargo hold
    internal static ShipDesign CargoDesign(string hullName, ShipCategory category)
    {
        Assert.IsTrue(ResourceManager.Hull(hullName, out ShipHull hull), $"setup: hull {hullName}");
        var design = new ShipDesign(hull, $"Cargo Test {hullName} {category}") { ShipCategory = category };
        var slots = new DesignSlot[hull.HullSlots.Length];
        for (int i = 0; i < slots.Length; ++i)
            slots[i] = new DesignSlot(hull.HullSlots[i].Pos, "BasicCargoHold", new Point(1, 1), 0, ModuleOrientation.Normal, null);
        design.SetDesignSlots(slots);
        return design;
    }

    float PrivatizedShareOfUpkeep(IShipDesign design)
    {
        Player.data.Privatization = false;
        float full = ShipMaintenance.GetBaseMaintenance(design, Player, 0);
        Player.data.Privatization = true;
        float privatized = ShipMaintenance.GetBaseMaintenance(design, Player, 0);
        AssertGreaterThan(full, 0f, $"setup: {design.Name} has upkeep");
        return privatized / full;
    }

    [TestMethod]
    public void PrivatizationHalvesTheUpkeepOfAFreighterOnAWarshipHull()
    {
        ShipDesign freighter = CargoDesign("Terran/Gunboat", ShipCategory.Civilian);
        AssertEqual(RoleName.frigate, freighter.HullRole, "setup: a frigate hull");
        Assert.IsTrue(freighter.IsFreighter, "setup: civilian and all cargo, so it is classified as a freighter");

        AssertEqual(0.001f, 0.5f, PrivatizedShareOfUpkeep(freighter));
    }

    [TestMethod]
    public void TheSameShipLeftUnclassifiedKeepsItsFullUpkeep()
    {
        ShipDesign ship = CargoDesign("Terran/Gunboat", ShipCategory.Unclassified);
        Assert.IsFalse(ship.IsFreighter, "setup: not civilian, so not a freighter");

        AssertEqual(0.001f, 1f, PrivatizedShareOfUpkeep(ship));
    }

    [TestMethod]
    public void ShipsOnFreighterHullsKeepTheirDiscountWhateverTheirRole()
    {
        foreach (string name in new[] { "Small Transport", "Colony Ship" })
        {
            Assert.IsTrue(ResourceManager.Ships.GetDesign(name, out IShipDesign design), $"setup: {name} is loaded");
            AssertEqual(RoleName.freighter, design.HullRole, $"setup: {name} is on a freighter hull");
            AssertEqual(0.001f, 0.5f, PrivatizedShareOfUpkeep(design), name);
        }
    }
}
