using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// Every race has a basic salvage station on its own small station hull, the Terran layout with two salvage units,
/// which the AI's orbital pickers never choose. The salvage platform is one fixed design: no empire builds it from a
/// list, and the player cannot order, refit, scrap or list it, nor pays upkeep for it. With salvage ops off, the ship
/// designer and the build lists leave out the salvage unit, the salvage stations and the platform. The ship designer
/// flags a salvage station with less cargo room than one load, the platform's hold, and every race's default holds one.
/// A mod that turns salvage ops on without the salvage platform has them off.
/// </summary>
[TestClass]
public class SalvageDesignTests : StarDriveTest
{
    static readonly string[] Stations =
    {
        "Terran Basic Salvage Station", "Cordrazine Basic Salvage Station", "Draylok Basic Salvage Station",
        "Kulrathi Basic Salvage Station", "Opteris Basic Salvage Station", "Pollops Basic Salvage Station",
        "Ralyeh Basic Salvage Station", "Vulfar Basic Salvage Station",
    };

    public SalvageDesignTests()
    {
        LoadStarterShips(Stations);
        LoadStarterShips(ShipDesign.SalvagePlatformName);
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

    [TestMethod]
    public void EveryRaceHasABasicSalvageStationOnItsOwnSmallStationHull()
    {
        foreach (string name in Stations)
        {
            IShipDesign station = Design(name);
            Assert.IsTrue(station.IsValidDesign, $"{name} is a valid design");
            Assert.IsTrue(station.IsSalvageStation, $"{name} is a salvage station");
            AssertEqual(2, station.NumSalvageModules, $"{name} carries two salvage units");
            StringAssert.EndsWith(station.Hull, "_Station_Small", $"{name} is on a small station hull");
            AssertEqual(station.BaseHull.Style, station.ShipStyle, $"{name} takes the style of its hull");

            var cells = new HashSet<Point>();
            foreach (HullSlot slot in station.BaseHull.HullSlots)
                cells.Add(slot.Pos);
            foreach (DesignSlot slot in station.GetOrLoadDesignSlots())
                for (int x = 0; x < slot.Size.X; ++x)
                    for (int y = 0; y < slot.Size.Y; ++y)
                        Assert.IsTrue(cells.Contains(new Point(slot.Pos.X + x, slot.Pos.Y + y)),
                                      $"{name}: {slot.ModuleUID} at {slot.Pos} sits on its hull");
        }
    }

    [TestMethod]
    public void EveryMajorRaceNamesItsOwnBasicSalvageStationAsItsDefault()
    {
        AssertEqual(Stations.Length, ResourceManager.MajorRaces.Count, "setup: one station per major race");
        foreach (IEmpireData race in ResourceManager.MajorRaces)
        {
            var data = (EmpireData)race;
            Assert.IsTrue(data.DefaultSalvageStation.NotEmpty(), $"{race.Name} names a default salvage station");
            IShipDesign station = Design(data.DefaultSalvageStation);
            Assert.IsTrue(station.IsSalvageStation, $"{race.Name}: {station.Name} is a salvage station");
            Assert.IsTrue(ShipResupply.HasRoomForSalvageLoad(station), $"{race.Name}: {station.Name} holds one salvage load");
            AssertEqual(data.Traits.ShipType, station.BaseHull.Style, $"{race.Name}: {station.Name} is on its own hull");
        }
    }

    [TestMethod]
    public void TheShipDesignerFlagsASalvageStationWithLessCargoRoomThanOneLoad()
    {
        float platformHold = Design(ShipDesign.SalvagePlatformName).BaseCargoSpace;
        AssertGreaterThan(platformHold, 0f, "setup: the platform has a hold");
        AssertEqual(platformHold, ShipResupply.SalvageLoad, "one load is the salvage platform's hold");

        IShipDesign station = Design("Terran Basic Salvage Station");
        var issues = new ShipDesignIssues(Universe, station);
        issues.CheckLowSalvageCargo();
        AssertEqual(0, issues.CurrentDesignIssues.Count, "setup: the basic station holds one load");

        ShipDesign holdless = Holdless(station, "Holdless Salvage Station");
        AssertLessThan(holdless.BaseCargoSpace, platformHold, "setup: the station without its cargo holds cannot hold one load");
        var flagged = new ShipDesignIssues(Universe, holdless);
        flagged.CheckLowSalvageCargo();
        AssertEqual(1, flagged.CurrentDesignIssues.Count, "a station that cannot hold one load is flagged");
        DesignIssueDetails issue = flagged.CurrentDesignIssues[0];
        Assert.AreEqual(DesignIssueType.LowSalvageCargo, issue.Type, "as low salvage cargo");
        Assert.AreEqual(WarningLevel.Critical, issue.Severity, "a critical issue");
        StringAssert.EndsWith(issue.AdditionalText, $" {platformHold.GetNumberString()}.", "its remedy names the cargo room one load needs");

        ShipDesign research = Holdless(Design("Basic Research Station"), "Holdless Research Station");
        Assert.IsFalse(ShipResupply.HasRoomForSalvageLoad(research), "setup: a research station with less cargo room");
        var other = new ShipDesignIssues(Universe, research);
        other.CheckLowSalvageCargo();
        Assert.IsTrue(other.CurrentDesignIssues.IsEmpty, "other stations are never flagged for it");
    }

    [TestMethod]
    public void TheAIOrbitalPickersNeverChooseASalvageStation()
    {
        IShipDesign station = Design("Terran Basic Salvage Station");
        Enemy.ClearShipsWeCanBuild();
        Enemy.AddBuildableShip(station);
        AssertEqual(RoleName.station, station.Role, "setup: a station-role design the empire can build");

        Assert.IsNull(ShipBuilder.PickCostEffectiveShipToBuild(RoleName.station, Enemy, float.MaxValue, float.MaxValue),
                      "the cost-effective orbital pick skips it");
        Assert.IsNull(ShipBuilder.BestShipWeCanBuild(RoleName.station, Enemy), "and so does the best station pick");
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
        IShipDesign station = Design("Terran Basic Salvage Station");
        IShipDesign platform = Design(ShipDesign.SalvagePlatformName);
        Assert.IsTrue(UState.P.SalvageOps, "setup: salvage ops on");
        Assert.IsFalse(ModuleSelectScrollList.IsTurnedOff(unit, UState.P), "setup: the designer lists the salvage unit");
        Assert.IsFalse(ShipDesignLoadScreen.IsTurnedOff(station, UState.P), "setup: and the salvage stations");
        Assert.IsTrue(station.CanBeAddedToBuildableShips(Player), "setup: which the player can add to the buildable ships");

        UState.P.DisableSalvageOps = true;
        Assert.IsTrue(ModuleSelectScrollList.IsTurnedOff(unit, UState.P), "with salvage ops off the designer leaves out the salvage unit");
        Assert.IsTrue(ShipDesignLoadScreen.IsTurnedOff(station, UState.P), "and the salvage stations");
        Assert.IsTrue(ShipDesignLoadScreen.IsTurnedOff(platform, UState.P), "and the salvage platform");
        Assert.IsFalse(station.CanBeAddedToBuildableShips(Player), "and no salvage station goes in the buildable ships");

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
