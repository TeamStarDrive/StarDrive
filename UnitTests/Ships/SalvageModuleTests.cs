using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;

namespace UnitTests.Ships;

/// <summary>
/// A salvage module is one with Salvage set in its module file. A station-role design with one is a salvage station,
/// and counts its salvage modules; a platform-role design never is one, whatever it carries, and neither is a salvage
/// ship. The count follows the design as it is edited. The designer keeps research and refining modules out of one
/// design, while the salvage unit, which no station takes, mixes with either.
/// </summary>
[TestClass]
public class SalvageModuleTests : StarDriveTest
{
    public SalvageModuleTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    static IShipDesign Design(string name)
    {
        Assert.IsTrue(ResourceManager.Ships.GetDesign(name, out IShipDesign design), $"setup: {name} is loaded");
        return design;
    }

    // the design plus `count` salvage units in a strip below its grid, so no slots overlap
    static ShipDesign WithSalvageUnits(IShipDesign from, int count)
    {
        ShipModule unit = ResourceManager.GetModuleTemplate("SalvageUnit");
        ShipDesign design = ((ShipDesign)from).GetClone($"{from.Name} with {count} salvage units");
        ShipGridInfo info = design.GridInfo;
        var slots = new Array<DesignSlot>(from.GetOrLoadDesignSlots());
        var size = new Point(unit.XSize, unit.YSize);
        for (int i = 0; i < count; ++i)
            slots.Add(new DesignSlot(new Point(i * size.X, info.Size.Y), unit.UID, size, 0, ModuleOrientation.Normal, null));
        info.Size = new Point(Math.Max(info.Size.X, count * size.X), info.Size.Y + size.Y);
        design.GridInfo = info;
        design.SetDesignSlots(slots.ToArray(), updateRole: false);
        return design;
    }

    [TestMethod]
    public void TheSalvageUnitIsASalvageModule()
    {
        Assert.IsTrue(ResourceManager.GetModuleTemplate("SalvageUnit", out ShipModule unit), "the salvage unit is loaded");
        Assert.IsTrue(unit.Salvage, "its Salvage flag is read from the module file");
        AssertEqual("Salvage Unit", unit.NameText.Text, "it has its own name");
        StringAssert.StartsWith(unit.DescriptionText.Text, "Salvage Unit strips derelict ships", "and its own description");
        Assert.IsFalse(ResourceManager.GetModuleTemplate("ProcessingUnit").Salvage, "other modules are not salvage modules");
    }

    [TestMethod]
    public void AStationDesignCountsItsSalvageModules()
    {
        IShipDesign mining = Design("Basic Mining Station");
        Assert.IsTrue(mining.IsStation, "setup: a station-role design");
        Assert.IsFalse(mining.IsSalvageStation, "with no salvage module it is no salvage station");

        ShipDesign salvage = WithSalvageUnits(mining, 2);
        AssertEqual(2, salvage.NumSalvageModules, "each salvage unit counts");
        Assert.IsTrue(salvage.IsSalvageStation, "a station with a salvage module is a salvage station");
    }

    [TestMethod]
    public void APlatformDesignIsNeverASalvageStation()
    {
        IShipDesign research = Design("Basic Research Station");
        AssertEqual(RoleName.platform, research.Role, "setup: a platform-role design");

        ShipDesign platform = WithSalvageUnits(research, 1);
        AssertEqual(1, platform.NumSalvageModules, "its salvage unit still counts");
        Assert.IsFalse(platform.IsSalvageStation, "but a platform is no salvage station");
    }

    [TestMethod]
    public void AStationWithASalvageUnitIsNoSalvageShip()
    {
        ShipDesign station = WithSalvageUnits(Design("Basic Mining Station"), 1);
        Assert.IsTrue(station.IsSalvageStation && station.NumSalvageModules > 0, "setup: a station with a salvage unit");
        Assert.IsFalse(station.IsSalvageShip, "is a salvage station, not a salvage ship");
    }

    [TestMethod]
    public void TheDesignerKeepsResearchAndMiningModulesApartButNotTheSalvageUnit()
    {
        ShipModule research = ResourceManager.GetModuleTemplate("ResearchLab");
        ShipModule refining = ResourceManager.GetModuleTemplate("ProcessingUnit");
        ShipModule salvage = ResourceManager.GetModuleTemplate("SalvageUnit");
        ShipModule plain = ResourceManager.GetModuleTemplate("Structure");
        Assert.IsTrue(research.ResearchPerTurn > 0 && refining.Refining > 0 && salvage.Salvage, "setup: one module of each kind");

        Assert.IsTrue(ShipDesignScreen.MixesStationKinds(refining, new[] { plain, research }), "no refining module on a research station");
        Assert.IsTrue(ShipDesignScreen.MixesStationKinds(research, new[] { refining }), "no research module on a mining station");
        Assert.IsFalse(ShipDesignScreen.MixesStationKinds(salvage, new[] { plain, refining }), "a salvage unit is no station kind of its own");
        Assert.IsFalse(ShipDesignScreen.MixesStationKinds(research, new[] { salvage }), "so a research module goes with it");
        Assert.IsFalse(ShipDesignScreen.MixesStationKinds(refining, new[] { plain, refining }), "more of the same kind is fine");
        Assert.IsFalse(ShipDesignScreen.MixesStationKinds(plain, new[] { refining, research }), "and other modules go anywhere");
        Assert.IsFalse(ShipDesignScreen.MixesStationKinds(research, new ShipModule[0]), "as does the first of a kind");
    }

    [TestMethod]
    public void RemovingTheSalvageModulesInTheDesignerClearsTheStation()
    {
        IShipDesign mining = Design("Basic Mining Station");
        ShipDesign salvage = WithSalvageUnits(mining, 1);
        Assert.IsTrue(salvage.IsSalvageStation, "setup: a salvage station");

        salvage.SetDesignSlots(mining.GetOrLoadDesignSlots(), updateRole: false);
        AssertEqual(0, salvage.NumSalvageModules, "no salvage module left");
        Assert.IsFalse(salvage.IsSalvageStation, "so no longer a salvage station");
    }
}
