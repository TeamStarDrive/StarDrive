using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// The salvage platform is one fixed design: no empire builds it from a list, and the player cannot order, refit,
/// scrap or list it, nor pays upkeep for it. With salvage ops off, the ship designer and the build lists leave out the
/// salvage unit, the salvage ships, the salvage stations and the platform. The ship designer flags a salvage ship red
/// with less cargo room than one load, the platform's hold, for each of its salvage units, orange under 1.1 loads a
/// unit, yellow under four thirds and green under five thirds, and flags no other design, a salvage station included.
/// A mod that turns salvage ops on without the salvage platform has them off.
/// </summary>
[TestClass]
public class SalvageDesignTests : StarDriveTest
{
    const string SalvageShip = "Terran Salvage Ship";
    const string TwinShip = "TEST_Twin Salvage Ship";
    const string OldStation = "TEST_Old Salvage Station";

    public SalvageDesignTests()
    {
        LoadStarterShips(SalvageShip, TwinShip, OldStation, ShipDesign.SalvagePlatformName);
        CreateUniverseAndPlayerEmpire();
    }

    static IShipDesign Design(string name)
    {
        Assert.IsTrue(ResourceManager.Ships.GetDesign(name, out IShipDesign design), $"setup: {name} is loaded");
        return design;
    }

    // a copy of the design without its cargo holds
    static ShipDesign Holdless(IShipDesign from, string name)
    {
        ShipDesign design = ((ShipDesign)from).GetClone(name);
        design.SetDesignSlots(from.GetOrLoadDesignSlots().Filter(s => ResourceManager.GetModuleTemplate(s.ModuleUID).CargoCapacity <= 0f), updateRole: false);
        return design;
    }

    [TestMethod]
    public void AModTurningSalvageOpsOnWithoutTheSalvagePlatformHasThemOff()
    {
        ModEntry activeMod = GlobalStats.ActiveMod;
        GamePlayGlobals defaults = GlobalStats.Defaults;
        try
        {
            GlobalStats.ActiveMod = new ModEntry(new GamePlayGlobals { Mod = new ModInformation { Name = "ZzSalvageMod" } });
            GlobalStats.Defaults = new GamePlayGlobals { EnableSalvageOps = true };
            ResourceManager.CheckModSalvageContent();
            Assert.IsTrue(GlobalStats.Defaults.EnableSalvageOps, "a mod with the salvage platform and a salvage unit keeps them on");

            ResourceManager.Ships.Delete(ShipDesign.SalvagePlatformName);
            ResourceManager.CheckModSalvageContent();
            Assert.IsFalse(GlobalStats.Defaults.EnableSalvageOps, "one without the salvage platform has them off");

            GlobalStats.ActiveMod = null;
            GlobalStats.Defaults.EnableSalvageOps = true;
            ResourceManager.CheckModSalvageContent();
            Assert.IsTrue(GlobalStats.Defaults.EnableSalvageOps, "vanilla is left as it is");
        }
        finally
        {
            GlobalStats.ActiveMod = activeMod;
            GlobalStats.Defaults = defaults;
            LoadStarterShips(ShipDesign.SalvagePlatformName);
        }
    }

    // a copy of the design with cargo holds taken off until its cargo room is under `high`, each the largest that leaves `low` or more, else the smallest
    static ShipDesign CargoIn(IShipDesign from, float low, float high, string name)
    {
        ShipDesign design = ((ShipDesign)from).GetClone(name);
        var slots = new Array<DesignSlot>(from.GetOrLoadDesignSlots());
        while (design.BaseCargoSpace >= high)
        {
            float room = design.BaseCargoSpace;
            DesignSlot[] holds = slots.Filter(s => ResourceManager.GetModuleTemplate(s.ModuleUID) is { CargoCapacity: > 0f, Salvage: false });
            DesignSlot[] keeping = holds.Filter(s => room - ResourceManager.GetModuleTemplate(s.ModuleUID).CargoCapacity >= low);
            slots.Remove(keeping.Length > 0 ? keeping.FindMax(s => ResourceManager.GetModuleTemplate(s.ModuleUID).CargoCapacity)
                                            : holds.FindMin(s => ResourceManager.GetModuleTemplate(s.ModuleUID).CargoCapacity));
            design.SetDesignSlots(slots.ToArray(), updateRole: false);
        }
        return design;
    }

    // a copy of the design with cargo room from `low` to under `high`
    static ShipDesign Band(IShipDesign from, float low, float high, string name)
    {
        ShipDesign design = CargoIn(from, low, high, name);
        Assert.IsTrue(design.BaseCargoSpace >= low && design.BaseCargoSpace < high, $"setup: {name} has room from {low} to under {high}, not {design.BaseCargoSpace}");
        return design;
    }

    // the one issue the ship designer flags about the design's salvage cargo
    DesignIssueDetails CargoIssue(IShipDesign design)
    {
        var issues = new ShipDesignIssues(Universe, design);
        issues.CheckLowSalvageCargo();
        AssertEqual(1, issues.CurrentDesignIssues.Count, $"{design.Name} is flagged");
        DesignIssueDetails issue = issues.CurrentDesignIssues[0];
        Assert.AreEqual(DesignIssueType.LowSalvageShipCargo, issue.Type, $"{design.Name}: as low salvage cargo");
        return issue;
    }

    [TestMethod]
    public void TheShipDesignerFlagsASalvageShipRedUnderALoadAUnitThenOrangeYellowAndGreenUnderFiveThirds()
    {
        float platformHold = Design(ShipDesign.SalvagePlatformName).BaseCargoSpace;
        AssertEqual(75f, platformHold, "setup: the platform holds 75");
        AssertEqual(platformHold, ShipResupply.SalvageLoad, "one load is the salvage platform's hold");

        IShipDesign ship = Design(TwinShip);
        AssertEqual(2, ship.NumSalvageModules, "setup: a ship with two salvage units");
        var issues = new ShipDesignIssues(Universe, Band(ship, 250f, 251f, "Clear Salvage Ship"));
        issues.CheckLowSalvageCargo();
        AssertEqual(0, issues.CurrentDesignIssues.Count, "room for 125 a unit is not flagged");

        DesignIssueDetails issue = CargoIssue(Band(ship, 200f, 201f, "Green Salvage Ship"));
        Assert.AreEqual(WarningLevel.Informative, issue.Severity, "under 125 a unit, 100 among it, a green issue");
        StringAssert.EndsWith(issue.AdditionalText, $" {Localizer.Token(GameText.DesignIssueLowSalvageCargoClears)} {250f.GetNumberString()}.",
                              "whose remedy names the cargo room that clears it");

        issue = CargoIssue(Band(ship, 165f, 200f, "Yellow Salvage Ship"));
        Assert.AreEqual(WarningLevel.Minor, issue.Severity, "under 100 a unit, a yellow one");
        StringAssert.EndsWith(issue.AdditionalText, $" {250f.GetNumberString()}.", "naming the same room");

        issue = CargoIssue(Band(ship, 0f, 150f, "Red Salvage Ship"));
        Assert.AreEqual(WarningLevel.Critical, issue.Severity, "under a load a unit, a red one");
        StringAssert.Contains(issue.AdditionalText, $" {150f.GetNumberString()}.", "whose remedy names the cargo room a load a unit needs");
        StringAssert.EndsWith(issue.AdditionalText, Localizer.Token(GameText.DesignIssueLowSalvageShipCargoSkipped), "and says the salvage ship list and auto pick skip it");

        IShipDesign single = Design(SalvageShip);
        AssertEqual(1, single.NumSalvageModules, "setup: a ship with one salvage unit, whose small holds step its room finely");
        issue = CargoIssue(Band(single, 83f, 84f, "Yellow Single Salvage Ship"));
        Assert.AreEqual(WarningLevel.Minor, issue.Severity, "from 82.5, yellow");
        issue = CargoIssue(Band(single, 82f, 82.5f, "Orange Single Salvage Ship"));
        Assert.AreEqual(WarningLevel.Major, issue.Severity, "under 82.5 a unit, an orange one");
        StringAssert.EndsWith(issue.AdditionalText, $" {125f.GetNumberString()}.", "naming the room that clears it, 125 for its one unit");
        issue = CargoIssue(Band(single, 75f, 76f, "Full Load Salvage Ship"));
        Assert.AreEqual(WarningLevel.Major, issue.Severity, "a load a unit is orange, not red");

        ShipDesign research = Holdless(Design("Basic Research Station"), "Holdless Research Station");
        AssertLessThan(research.BaseCargoSpace, platformHold, "setup: a research station with less cargo room than one load");
        var other = new ShipDesignIssues(Universe, research);
        other.CheckLowSalvageCargo();
        Assert.IsTrue(other.CurrentDesignIssues.IsEmpty, "other designs are never flagged for it");

        ShipDesign station = CargoIn(Design(OldStation), 0f, 150f, "Short Salvage Station");
        Assert.IsTrue(station.IsSalvageStation && station.BaseCargoSpace < station.NumSalvageModules * platformHold,
                      "setup: a salvage station with less cargo room than a load a unit");
        var old = new ShipDesignIssues(Universe, station);
        old.CheckLowSalvageCargo();
        Assert.IsTrue(old.CurrentDesignIssues.IsEmpty, "nor a salvage station, which no empire builds");
    }

    [TestMethod]
    public void NoEmpireCanAddTheSalvagePlatformToItsBuildableShips()
    {
        IShipDesign platform = Design(ShipDesign.SalvagePlatformName);
        Assert.IsTrue(platform.IsSalvagePlatform && platform.IsValidDesign, "setup: the salvage platform is a valid design");
        Assert.IsFalse(platform.CanBeAddedToBuildableShips(Player), "the player never builds it from a list");
        Assert.IsFalse(platform.CanBeAddedToBuildableShips(Enemy), "nor does an AI empire");

        ShipDesign copy = ((ShipDesign)platform).GetClone("Salvage Platform Copy");
        Assert.IsFalse(copy.IsSalvagePlatform, "only the design with that name is the salvage platform");
        Assert.IsTrue(copy.CanBeAddedToBuildableShips(Player), "setup: the same platform under another name is buildable");
    }

    [TestMethod]
    public void WithSalvageOpsOffTheDesignerAndTheBuildListsLeaveOutTheSalvageUnitAndDesigns()
    {
        ShipModule unit = ResourceManager.GetModuleTemplate("SalvageUnit");
        IShipDesign station = Design(OldStation);
        IShipDesign ship = Design(SalvageShip);
        IShipDesign platform = Design(ShipDesign.SalvagePlatformName);
        Assert.IsTrue(UState.P.SalvageOps, "setup: salvage ops on");
        Assert.IsFalse(ModuleSelectScrollList.IsTurnedOff(unit, UState.P), "setup: the designer lists the salvage unit");
        Assert.IsFalse(ShipDesignLoadScreen.IsTurnedOff(station, UState.P), "setup: and the salvage stations");
        Assert.IsTrue(ship.CanBeAddedToBuildableShips(Player), "setup: the player can add a salvage ship to the buildable ships");

        UState.P.DisableSalvageOps = true;
        Assert.IsTrue(ModuleSelectScrollList.IsTurnedOff(unit, UState.P), "with salvage ops off the designer leaves out the salvage unit");
        Assert.IsTrue(ShipDesignLoadScreen.IsTurnedOff(station, UState.P), "and the salvage stations");
        Assert.IsTrue(ShipDesignLoadScreen.IsTurnedOff(platform, UState.P), "and the salvage platform");
        Assert.IsTrue(ShipDesignLoadScreen.IsTurnedOff(ship, UState.P), "and the salvage ships");
        Assert.IsFalse(ship.CanBeAddedToBuildableShips(Player), "and no salvage ship goes in the buildable ships");

        Assert.IsFalse(ModuleSelectScrollList.IsTurnedOff(ResourceManager.GetModuleTemplate("Structure"), UState.P),
                       "other modules stay listed");
        foreach (string name in new[] { "Basic Mining Station", "Basic Research Station" })
        {
            IShipDesign other = Design(name);
            Assert.IsFalse(ShipDesignLoadScreen.IsTurnedOff(other, UState.P), $"other stations and platforms stay listed: {name}");
            Assert.IsTrue(other.CanBeAddedToBuildableShips(Player), $"and buildable: {name}");
        }
    }

    [TestMethod]
    public void ThePlayerCannotOrderRefitScrapOrListASalvagePlatformAndPaysNoUpkeepForIt()
    {
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, new Vector2(20_000));
        Assert.IsFalse(platform.CanBeScrapped, "it cannot be scrapped");
        Assert.IsFalse(platform.CanBeRefitted, "nor refit");
        Assert.IsFalse(platform.PlayerShipCanTakeFleetOrders(forAttack: true), "nor given orders");
        Assert.IsTrue(ShipListScreen.IsLeftOffTheList(platform), "and the ship list leaves it out");
        Ship target = SpawnShip("Vulcan Scout", Enemy, new Vector2(21_000));
        Assert.IsFalse(new Ship_Game.Universe.ShipMoveCommands(Universe).AttackSpecificShip(platform, target),
                       "right-clicking a ship gives it no order");
        Assert.IsTrue(platform.AI.OrderQueue.IsEmpty, "so it keeps its orders");
        AssertEqual(0f, ShipMaintenance.GetMaintenanceCost(platform, Player, 0), "it has no upkeep");
        AssertEqual(0f, ShipMaintenance.GetBaseMaintenance(platform.ShipData, Player, 0), "nor does its design");

        Ship other = SpawnShip("Basic Research Station", Player, new Vector2(40_000));
        AssertEqual(RoleName.platform, other.ShipData.Role, "setup: another platform-role design");
        Assert.IsTrue(other.CanBeScrapped && !ShipListScreen.IsLeftOffTheList(other), "setup: other platforms can be scrapped and are listed");
        AssertGreaterThan(ShipMaintenance.GetMaintenanceCost(other, Player, 0), 0f, "setup: and pay upkeep");
    }
}
