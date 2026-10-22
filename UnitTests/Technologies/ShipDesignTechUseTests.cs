using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Technologies;

/// <summary>
/// A tech that unlocks modules and no hull is of use to an empire while a design of its race has one of those
/// modules, shipyards and the salvage platform left out, or while it leads to a tech of use. It is worked out both
/// ways at start, on load, as the player saves or deletes a design and as an absorbed race's designs join the
/// empire's, the race's designs read again.
/// </summary>
[TestClass]
public class ShipDesignTechUseTests : StarDriveTest
{
    const string Dreadnought = "Dreadnought mk2-c"; // a Terran design with a canopy shield
    const string Copy = "TEST Canopy Copy";

    // the starter designs, none of which has a canopy shield or a 4x4 disruptor, and a Cordrazine one that has the disruptor
    public ShipDesignTechUseTests()
    {
        ReloadStarterShips();
        LoadStarterShips("C-Mining Station mk1-a");
        CreateUniverseAndPlayerEmpire("Human", "Cordrazine");
    }

    [TestCleanup]
    public override void Cleanup()
    {
        ReloadStarterShips(); // the station and the copies leave with the rest
        base.Cleanup();
    }

    bool OfUse(Empire empire, string tech) => empire.GetTechEntry(tech).shipDesignsCanuseThis;

    // the dreadnought's modules in a design of this name, the dreadnought itself unloaded
    void DreadnoughtCopy(string name, bool shipyard)
    {
        LoadStarterShips(Dreadnought);
        ShipDesign copy = ((ShipDesign)ResourceManager.Ships.GetDesign(Dreadnought)).GetClone(name);
        copy.IsShipyard = shipyard;
        ResourceManager.Ships.Delete(Dreadnought);
        Assert.IsTrue(ResourceManager.AddShipTemplate(copy, playerDesign: false), "setup: the copy is added");
        Player.ShipDesignsChanged();
    }

    [TestMethod]
    public void ATechIsOfUseOnlyWhileADesignOfTheRaceHasItsModules()
    {
        Assert.IsTrue(OfUse(Player, "SpaceWeapons"), "the Vulcan Scout's cannon makes its tech of use to the Terran player");
        Assert.IsFalse(OfUse(Player, "CanopyShields"), "a tech no Terran design loaded uses is not");
        Assert.IsFalse(OfUse(Player, "MassiveDisruptors"), "nor one only a Cordrazine design uses");
        Assert.IsTrue(OfUse(Enemy, "MassiveDisruptors"), "which is of use to the Cordrazine");
    }

    [TestMethod]
    public void AFlagASaveHeldOffTurnsBackOnAtLoadWhileADesignUsesIt()
    {
        AssertEqual(0, Enemy.GetTechEntry("MassiveDisruptors").Tech.LeadsTo.Count, "setup: it leads to no tech that could turn it back on");
        Enemy.GetTechEntry("MassiveDisruptors").shipDesignsCanuseThis = false;
        Enemy.ResetTechsAndUnlocks(); // as a load does
        Assert.IsTrue(OfUse(Enemy, "MassiveDisruptors"), "the Cordrazine mining station's disruptor still uses it");
    }

    [TestMethod]
    public void ADesignOfTheRaceAddedMakesItsTechsOfUseAndOneDeletedNoLonger()
    {
        DreadnoughtCopy(Copy, shipyard: false); // as the player saves a design
        Assert.IsTrue(Player.AllFactionShipDesigns.Any(d => d.Name == Copy), "the race's designs are read again");
        Assert.IsTrue(OfUse(Player, "CanopyShields"), "and the copy's canopy shield makes its tech of use");

        ResourceManager.DeleteShip(UState, Copy); // as the player deletes it; no file of that name is on disk
        Assert.IsFalse(OfUse(Player, "CanopyShields"), "with it gone, the tech is of no use again");
    }

    [TestMethod]
    public void AShipyardsOrTheSalvagePlatformsModulesMakeNoTechOfUse()
    {
        DreadnoughtCopy(Copy, shipyard: true);
        Assert.IsFalse(OfUse(Player, "CanopyShields"), "a shipyard with a canopy shield makes its tech of no use");

        ResourceManager.Ships.Delete(Copy);
        DreadnoughtCopy(ShipDesign.SalvagePlatformName, shipyard: false);
        Assert.IsFalse(OfUse(Player, "CanopyShields"), "and nor does the salvage platform");

        ResourceManager.Ships.Delete(ShipDesign.SalvagePlatformName);
        DreadnoughtCopy(Copy, shipyard: false);
        Assert.IsTrue(OfUse(Player, "CanopyShields"), "while any other design with one does");
    }

    [TestMethod]
    public void AnAbsorbedRacesDesignsMakeTheirTechsOfUse()
    {
        Assert.IsFalse(OfUse(Player, "MassiveDisruptors"), "setup: of no use to the Terran player");
        Player.AbsorbEmpire(Enemy);
        Assert.IsTrue(OfUse(Player, "MassiveDisruptors"), "the absorbed Cordrazine's mining station makes it of use");
    }
}
