using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships;

/// <summary>
/// The shipyard gives an unarmed design set to the Recon category the scout role on any hull but a drone's, so a
/// freighter hull fitted out as a scout gets the scout's Explore order and is sent exploring by the automation. Armed, it
/// keeps its hull's role.
/// </summary>
[TestClass]
public class ReconScoutRoleTests : StarDriveTest
{
    const string FreighterName = "Owlwok Freighter S";
    const string WeaponName = "LaserCannon";

    public ReconScoutRoleTests()
    {
        LoadStarterShips(FreighterName);
        CreateUniverseAndPlayerEmpire();
    }

    // the freighter design as the shipyard saves it in this category, with its first small cargo hold swapped for a gun when armed
    static ShipDesign SavedAs(ShipCategory category, bool armed)
    {
        Assert.IsTrue(ResourceManager.Ships.GetDesign(FreighterName, out IShipDesign from), $"setup: {FreighterName} is loaded");
        ShipDesign design = ((ShipDesign)from).GetClone($"{FreighterName} {category} {armed}");
        design.ShipCategory = category;
        var slots = (DesignSlot[])from.GetOrLoadDesignSlots().Clone();
        if (armed)
        {
            ShipModule gun = ResourceManager.GetModuleTemplate(WeaponName);
            Assert.IsTrue(gun.IsWeapon && gun.XSize == 1 && gun.YSize == 1, "setup: a small gun");
            int hold = System.Array.FindIndex(slots, s => s.ModuleUID == "BasicCargoHold");
            Assert.IsTrue(hold >= 0, "setup: the freighter has a small cargo hold to swap");
            slots[hold] = new DesignSlot(slots[hold].Pos, WeaponName, slots[hold].Size, 0, slots[hold].ModuleRot, null);
        }
        design.SetDesignSlots(slots, updateRole: true);
        return design;
    }

    [TestMethod]
    public void AnUnarmedReconDesignOnAFreighterHullIsAScout()
    {
        AssertEqual(RoleName.scout, SavedAs(ShipCategory.Recon, armed: false).Role, "an unarmed Recon design scouts on a freighter hull");
    }

    [TestMethod]
    public void AnArmedReconDesignKeepsItsHullsRole()
    {
        AssertEqual(RoleName.freighter, SavedAs(ShipCategory.Recon, armed: true).Role, "an armed Recon design on a freighter hull stays a freighter");
    }

    [TestMethod]
    public void ACivilianDesignOnAFreighterHullStaysAFreighter()
    {
        AssertEqual(RoleName.freighter, SavedAs(ShipCategory.Civilian, armed: false).Role, "a Civilian freighter design is still a freighter");
    }
}
