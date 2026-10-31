using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// Every major race names a salvage ship: a support ship, not a station, carrying one salvage unit, every module on its
/// hull. The shipyard gives a design with a salvage unit the support role; its unit is a bay, which launches its
/// constructors, but makes it no carrier, and the ship designer flags one short of a load a unit with its own warning.
/// A major empire, the player or an AI's, adds its race's salvage ship to its buildable ships with salvage ops on, a
/// faction never does, and none is ever a freighter, whatever its role; the design list leaves them out with salvage ops off.
/// The Salvage Ships technology comes after Frigate Construction, unlocks the salvage unit
/// and leads to Salvage Speed in place of Small Stations, costs 400 and is hidden with salvage ops off. An empire can build salvage ships once a buildable
/// salvage ship has room for a platform's load for each of its units; a race naming a station or a ship without a
/// salvage unit names no salvage ship. An empire saved before salvage ships, or with a salvage ship that no longer is
/// a design, takes its race's on load.
/// </summary>
[TestClass]
public class SalvageShipTests : StarDriveTest
{
    static readonly string[] SalvageShips =
    {
        "Terran Salvage Ship", "Cordrazine Salvage Ship", "Draylok Salvage Ship", "Kulrathi Salvage Ship",
        "Opteris Salvage Ship", "Pollops Salvage Ship", "Ralyeh Salvage Ship", "Vulfar Salvage Ship",
    };

    public SalvageShipTests()
    {
        LoadStarterShips(SalvageShips);
        LoadStarterShips("TEST_Old Salvage Station", ShipDesign.SalvagePlatformName); // the platform's hold is a salvage load
        CreateUniverseAndPlayerEmpire();
    }

    static IShipDesign Design(string name)
    {
        Assert.IsTrue(ResourceManager.Ships.GetDesign(name, out IShipDesign design), $"setup: {name} is loaded");
        return design;
    }

    static bool LeadsTo(string from, string to)
    {
        foreach (Technology.LeadsToTech next in ResourceManager.Tech(from).LeadsTo)
            if (next.UID == to)
                return true;
        return false;
    }

    static bool Unlocks(string tech, string module)
    {
        foreach (Technology.UnlockedMod unlocked in ResourceManager.Tech(tech).ModulesUnlocked)
            if (unlocked.ModuleUID == module)
                return true;
        return false;
    }

    // a copy of the design under the freighter role and a freighter's civilian category, with or without its salvage unit
    static ShipDesign AsFreighter(IShipDesign from, string name, bool keepUnit)
    {
        ShipDesign design = ((ShipDesign)from).GetClone(name);
        typeof(ShipDesign).GetProperty(nameof(ShipDesign.Role)).SetValue(design, RoleName.freighter);
        design.ShipCategory = ShipCategory.Civilian;
        design.SetDesignSlots(from.GetOrLoadDesignSlots().Filter(s => keepUnit || !ResourceManager.GetModuleTemplate(s.ModuleUID).Salvage),
                              updateRole: false);
        return design;
    }

    [TestMethod]
    public void EveryMajorRaceNamesASalvageShipWithOneSalvageUnitOnAShipHull()
    {
        AssertEqual(SalvageShips.Length, ResourceManager.MajorRaces.Count, "setup: one salvage ship per major race");
        foreach (IEmpireData race in ResourceManager.MajorRaces)
        {
            var data = (EmpireData)race;
            IShipDesign ship = ResourceManager.SalvageShipDesignOf(data);
            Assert.IsNotNull(ship, $"{race.Name} names a salvage ship, not '{data.DefaultSalvageShip}'");
            Assert.IsTrue(ship.IsValidDesign && ship.BaseCanWarp, $"{race.Name}: {ship.Name} is a valid design that can warp");
            AssertEqual(1, ship.NumSalvageModules, $"{race.Name}: {ship.Name} carries one salvage unit");
            AssertEqual(RoleName.support, ship.Role, $"{race.Name}: {ship.Name} is a support ship");
            Assert.IsFalse(ship.IsPlatformOrStation, $"{race.Name}: {ship.Name} is no station or platform");

            var cells = new HashSet<Point>();
            foreach (HullSlot slot in ship.BaseHull.HullSlots)
                cells.Add(slot.Pos);
            foreach (DesignSlot slot in ship.GetOrLoadDesignSlots())
                for (int x = 0; x < slot.Size.X; ++x)
                    for (int y = 0; y < slot.Size.Y; ++y)
                        Assert.IsTrue(cells.Contains(new Point(slot.Pos.X + x, slot.Pos.Y + y)),
                                      $"{ship.Name}: {slot.ModuleUID} at {slot.Pos} sits on its hull");
        }
    }

    [TestMethod]
    public void AMajorEmpireAddsASalvageShipToItsBuildableShipsAFactionNeverAndNoneIsEverAFreighter()
    {
        IShipDesign ship = Design("Terran Salvage Ship");
        Assert.IsTrue(ship.IsSalvageShip, "setup: the Terran salvage ship");
        Assert.IsTrue(ship.CanBeAddedToBuildableShips(Player), "the player can build it");
        foreach (string name in SalvageShips)
            Assert.IsTrue(Design(name).CanBeAddedToBuildableShips(Enemy), $"an AI empire can build {name}, as the race's own AI builds it");
        CreateAMinorFaction("Corsairs");
        Assert.IsFalse(ship.CanBeAddedToBuildableShips(Faction), "a faction cannot");
        UState.P.DisableSalvageOps = true;
        Assert.IsFalse(ship.CanBeAddedToBuildableShips(Player) || ship.CanBeAddedToBuildableShips(Enemy), "nor any empire with salvage ops off");
        UState.P.DisableSalvageOps = false;

        ShipDesign freighter = AsFreighter(ship, "Salvage Freighter", keepUnit: true);
        Assert.IsTrue(freighter.Role == RoleName.freighter && freighter.IsSalvageShip, "setup: a copy under the freighter role");
        Assert.IsFalse(freighter.IsFreighter || freighter.IsCandidateForTradingBuild, "is no freighter, so trade never takes it");
        ShipDesign plain = AsFreighter(ship, "Plain Freighter", keepUnit: false);
        Assert.IsTrue(!plain.IsSalvageShip && plain.IsFreighter, "control: without its salvage unit it is one");
    }

    [TestMethod]
    public void WithSalvageOpsOffTheDesignListLeavesOutTheSalvageShips()
    {
        ShipDesign freighter = AsFreighter(Design("Terran Salvage Ship"), "Salvage Freighter", keepUnit: true);
        Assert.IsFalse(ShipDesignLoadScreen.IsTurnedOff(freighter, UState.P), "setup: with salvage ops on, the design list may show it");
        UState.P.DisableSalvageOps = true;
        Assert.IsTrue(ShipDesignLoadScreen.IsTurnedOff(freighter, UState.P), "with them off it leaves it out");
        Assert.IsFalse(ShipDesignLoadScreen.IsTurnedOff(AsFreighter(freighter, "Plain Freighter", keepUnit: false), UState.P),
                       "control: and the same freighter without its salvage unit stays listed");
    }

    [TestMethod]
    public void ASalvageShipsUnitIsABayForItsConstructorsButMakesItNoCarrier()
    {
        Ship ship = SpawnShip("Terran Salvage Ship", Player, new Vector2(100_000f, 0));
        AssertEqual(1, ship.Carrier.AllHangars.Length, "setup: the ship's one bay");
        Assert.IsTrue(ship.Carrier.AllHangars[0].IsSalvageBay, "is its salvage unit, which launches its constructors");
        Assert.IsFalse(ship.Carrier.HasActiveHangars || ship.Carrier.IsPrimaryCarrierRoleForLaunchRange, "but it makes no carrier");
        AssertEqual(0f, ship.WeaponsMaxRange, "so the unarmed ship takes no carrier's range");
    }

    [TestMethod]
    public void TheShipyardGivesADesignWithASalvageUnitTheSupportRole()
    {
        IShipDesign from = Design("Terran Salvage Ship");
        ShipDesign design = ((ShipDesign)from).GetClone("Shipyard Salvage Ship");
        design.SetDesignSlots(from.GetOrLoadDesignSlots(), updateRole: true); // as the shipyard sets a design's role from its modules
        AssertEqual(RoleName.support, design.Role, "a design with a salvage unit takes the support role");
    }

    [TestMethod]
    public void TheShipDesignerFlagsASalvageShipShortOfALoadAUnitWithItsOwnWarning()
    {
        IShipDesign from = Design("Terran Salvage Ship");
        ShipDesign design = ((ShipDesign)from).GetClone("Short Salvage Ship");
        design.SetDesignSlots(from.GetOrLoadDesignSlots().Filter(s => ResourceManager.GetModuleTemplate(s.ModuleUID) is { CargoCapacity: <= 0f } or { Salvage: true }),
                              updateRole: false);
        AssertLessThan(design.BaseCargoSpace, 75f, "setup: a salvage ship with less cargo room than its unit's load");

        var issues = new ShipDesignIssues(Universe, design);
        issues.CheckLowSalvageCargo();
        AssertEqual(1, issues.CurrentDesignIssues.Count, "the ship designer flags it");
        DesignIssueDetails issue = issues.CurrentDesignIssues[0];
        Assert.AreEqual(DesignIssueType.LowSalvageShipCargo, issue.Type, "with the salvage ship's warning");
        Assert.AreEqual(WarningLevel.Critical, issue.Severity, "in red");
        StringAssert.EndsWith(issue.AdditionalText, Localizer.Token(GameText.DesignIssueLowSalvageShipCargoSkipped), "saying the salvage ship list and auto pick skip it");
    }

    [TestMethod]
    public void TheSalvageShipsTechnologyComesAfterFrigateConstructionUnlocksTheSalvageUnitAndLeadsToSalvageSpeed()
    {
        AssertEqual(400f, ResourceManager.Tech("SalvageShips").Cost, "it costs 400");
        Assert.IsTrue(LeadsTo("FrigateConstruction", "SalvageShips"), "Frigate Construction leads to it");
        Assert.IsFalse(LeadsTo("Corvettes", "SalvageShips"), "and Corvettes no longer does");
        Assert.IsTrue(Unlocks("SalvageShips", "SalvageUnit"), "it unlocks the salvage unit");
        Assert.IsFalse(Unlocks("SmallStations", "SalvageUnit"), "which Small Stations no longer does");
        Assert.IsTrue(LeadsTo("SalvageShips", "SalvageSpeed"), "and it leads to Salvage Speed");
        Assert.IsFalse(LeadsTo("SmallStations", "SalvageSpeed"), "which Small Stations no longer leads to");

        TechEntry entry = Player.GetTechEntry("SalvageShips");
        Assert.IsFalse(entry.IsHidden(Player), "shown with salvage ops on");
        UState.P.DisableSalvageOps = true;
        Assert.IsTrue(entry.IsHidden(Player), "hidden with them off");
    }

    [TestMethod]
    public void AnEmpireCanBuildSalvageShipsOnceABuildableOneHasRoomForALoadPerUnit()
    {
        Assert.IsFalse(Enemy.CanBuildSalvageShips, "setup: the AI empire can build no salvage ship");
        ShipDesign shortHold = ((ShipDesign)Design("Terran Salvage Ship")).GetClone("Short Salvage Ship");
        shortHold.SetDesignSlots(Design("Terran Salvage Ship").GetOrLoadDesignSlots()
                              .Filter(s => ResourceManager.GetModuleTemplate(s.ModuleUID) is { CargoCapacity: <= 0f } or { Salvage: true }),
                              updateRole: false);
        Assert.IsTrue(shortHold.IsSalvageShip && shortHold.BaseCargoSpace < ShipResupply.SalvageLoad, "setup: a salvage ship short of a load");
        Enemy.MarkShipRolesUsableForEmpire(shortHold);
        Assert.IsFalse(Enemy.CanBuildSalvageShips, "which does not count");
        Enemy.MarkShipRolesUsableForEmpire(Design("Cordrazine Salvage Ship"));
        Assert.IsTrue(Enemy.CanBuildSalvageShips, "one with room for its load does");

        AssertEqual("Terran Salvage Ship", Player.SalvageShipDesign?.Name, "setup: the Terran race's salvage ship");
        Player.data.DefaultSalvageShip = "TEST_Old Salvage Station";
        Assert.IsNull(Player.SalvageShipDesign, "a race naming a salvage station names no salvage ship");
        Player.data.DefaultSalvageShip = "Terran Constructor";
        Assert.IsNull(Player.SalvageShipDesign, "nor one naming a ship without a salvage unit");
    }

    [TestMethod]
    public void AnEmpireSavedBeforeSalvageShipsOrWithOneGoneTakesItsRaces()
    {
        Player.data.DefaultSalvageShip = null; // as a save from before salvage ships
        Assert.AreNotEqual("Ralyeh Salvage Ship", Enemy.data.DefaultSalvageShip, "setup: not the other race's own");
        Enemy.data.DefaultSalvageShip = "Ralyeh Salvage Ship";
        CreateThirdMajorEmpire();
        string thirdRaces = ThirdMajor.data.DefaultSalvageShip;
        Assert.IsTrue(ResourceManager.Ships.GetDesign(thirdRaces, out _), $"setup: a third race with its own salvage ship, {thirdRaces}");
        ThirdMajor.data.DefaultSalvageShip = "Zz Renamed Salvage Ship"; // as a design renamed since the save

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageShips", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        AssertEqual("Terran Salvage Ship", player.data.DefaultSalvageShip, "an empire saved without a salvage ship takes its race's");
        AssertEqual("Ralyeh Salvage Ship", loaded.UState.GetEmpireById(Enemy.Id).data.DefaultSalvageShip,
                    "one saved with a salvage ship keeps it");
        AssertEqual(thirdRaces, loaded.UState.GetEmpireById(ThirdMajor.Id).data.DefaultSalvageShip,
                    "and one saved with a design that is gone takes its race's");
    }
}
