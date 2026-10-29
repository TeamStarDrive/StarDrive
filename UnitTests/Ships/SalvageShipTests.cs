using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;

namespace UnitTests.Ships;

/// <summary>
/// Every major race names a salvage ship: a ship, not a station, carrying one salvage unit, every module on its hull,
/// which no empire adds to its buildable ships under any role, and which the design list leaves out with salvage ops
/// off. A station with a salvage unit is no salvage ship. The Salvage Ships technology comes after Corvettes, leads to
/// Salvage Speed in place of Small Stations, costs 400 and is hidden with salvage ops off. Researched, it lets an
/// empire build its race's salvage ship; a race naming none, a station or a ship without a salvage unit cannot. The
/// technology's flag survives a save and goes with a reset of all techs, and an empire saved before salvage ships,
/// or with a salvage ship that no longer is a design, takes its race's on load.
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
        LoadStarterShips("Terran Basic Salvage Station");
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

    // a copy of the design under the freighter role, with or without its salvage unit
    static ShipDesign AsFreighter(IShipDesign from, string name, bool keepUnit)
    {
        ShipDesign design = ((ShipDesign)from).GetClone(name);
        typeof(ShipDesign).GetProperty(nameof(ShipDesign.Role)).SetValue(design, RoleName.freighter);
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
    public void NoEmpireAddsASalvageShipToItsBuildableShipsUnderAnyRole()
    {
        IShipDesign ship = Design("Terran Salvage Ship");
        Assert.IsTrue(ship.IsSalvageShip, "setup: the Terran salvage ship");
        Assert.IsFalse(ship.CanBeAddedToBuildableShips(Player), "the player never builds it from a list");
        Assert.IsFalse(ship.CanBeAddedToBuildableShips(Enemy), "nor does an AI empire");

        ShipDesign freighter = AsFreighter(ship, "Salvage Freighter", keepUnit: true);
        Assert.IsTrue(freighter.Role == RoleName.freighter && freighter.IsSalvageShip, "setup: a copy under a role the lists take");
        Assert.IsFalse(freighter.CanBeAddedToBuildableShips(Player), "is not buildable either");
        ShipDesign plain = AsFreighter(ship, "Plain Freighter", keepUnit: false);
        Assert.IsFalse(plain.IsSalvageShip, "setup: without its salvage unit it is no salvage ship");
        Assert.IsTrue(plain.CanBeAddedToBuildableShips(Player), "control: and the player can build it");
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
    public void AStationWithASalvageUnitIsNoSalvageShip()
    {
        IShipDesign station = Design("Terran Basic Salvage Station");
        Assert.IsTrue(station.IsSalvageStation && station.NumSalvageModules > 0, "setup: a station with salvage units");
        Assert.IsFalse(station.IsSalvageShip, "is a salvage station, not a salvage ship");
    }

    [TestMethod]
    public void TheSalvageShipsTechnologyComesAfterCorvettesAndLeadsToSalvageSpeed()
    {
        AssertEqual(400f, ResourceManager.Tech("SalvageShips").Cost, "it costs 400");
        Assert.IsTrue(LeadsTo("Corvettes", "SalvageShips"), "Corvettes leads to it");
        Assert.IsTrue(LeadsTo("SalvageShips", "SalvageSpeed"), "and it to Salvage Speed");
        Assert.IsFalse(LeadsTo("SmallStations", "SalvageSpeed"), "which Small Stations no longer leads to");

        TechEntry entry = Player.GetTechEntry("SalvageShips");
        Assert.IsFalse(entry.IsHidden(Player), "shown with salvage ops on");
        UState.P.DisableSalvageOps = true;
        Assert.IsTrue(entry.IsHidden(Player), "hidden with them off");
    }

    [TestMethod]
    public void ResearchingSalvageShipsLetsAnEmpireBuildItsRacesSalvageShip()
    {
        AssertEqual("Terran Salvage Ship", Player.SalvageShipDesign?.Name, "setup: the Terran race's salvage ship");
        Assert.IsFalse(Player.data.AllowSalvageShip || Player.CanBuildSalvageShips, "setup: without the technology it cannot build it");

        Player.UnlockTech("SalvageShips", TechUnlockType.Normal);
        Assert.IsTrue(Player.data.AllowSalvageShip, "the technology allows salvage ships");
        Assert.IsTrue(Player.CanBuildSalvageShips, "so the empire can build its race's");
        Assert.IsFalse(Enemy.CanBuildSalvageShips, "and an empire without the technology still cannot");

        Player.data.DefaultSalvageShip = "Terran Basic Salvage Station";
        Assert.IsNull(Player.SalvageShipDesign, "a race naming a salvage station names no salvage ship");
        Assert.IsFalse(Player.CanBuildSalvageShips, "and cannot build one, technology or not");
        Player.data.DefaultSalvageShip = "Terran Constructor";
        Assert.IsFalse(Player.CanBuildSalvageShips, "nor can one naming a ship without a salvage unit");
        Player.data.DefaultSalvageShip = null;
        Assert.IsFalse(Player.CanBuildSalvageShips, "nor one naming none");
    }

    [TestMethod]
    public void ResettingAllTechsTakesAwayTheSalvageShips()
    {
        Player.UnlockTech("SalvageShips", TechUnlockType.Normal);
        Assert.IsTrue(Player.CanBuildSalvageShips, "setup: researched");
        Player.ResetAllTechsAndBonuses();
        Assert.IsFalse(Player.GetTechEntry("SalvageShips").Unlocked, "setup: the reset takes the technology back");
        Assert.IsFalse(Player.data.AllowSalvageShip || Player.CanBuildSalvageShips, "and with it the salvage ships");
    }

    [TestMethod]
    public void TheFlagSurvivesASaveAndAnEmpireSavedBeforeSalvageShipsOrWithOneGoneTakesItsRaces()
    {
        Player.UnlockTech("SalvageShips", TechUnlockType.Normal);
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
        Assert.IsTrue(player.data.AllowSalvageShip, "the technology's flag survives the save");
        AssertEqual("Terran Salvage Ship", player.data.DefaultSalvageShip, "an empire saved without a salvage ship takes its race's");
        Assert.IsTrue(player.CanBuildSalvageShips, "so it can build it");
        AssertEqual("Ralyeh Salvage Ship", loaded.UState.GetEmpireById(Enemy.Id).data.DefaultSalvageShip,
                    "one saved with a salvage ship keeps it");
        AssertEqual(thirdRaces, loaded.UState.GetEmpireById(ThirdMajor.Id).data.DefaultSalvageShip,
                    "and one saved with a design that is gone takes its race's");
    }
}
