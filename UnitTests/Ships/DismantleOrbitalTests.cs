using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A platform, station or orbital shipyard scrapped where it stands, from its panel, its ship list row, the governor
/// or a salvage station's end, comes apart over 1 turn per 50 slots, from 3 to 20 whatever the pace. It stops at
/// once: its weapons, shields, repair, supply, hangars and work go quiet, it pays no upkeep and cannot be refitted,
/// and it leaves its planet's orbitals, its place there free. Once it is gone, the planet it orbits, when the
/// empire's, gets half of its cost by its health and the production aboard, the excess paid as credits at the tax
/// rate with the turn's income. Scrap again calls it off, the orbital whole and at work, while its place is still
/// free: its planet its empire's or no one's, with room among its orbitals; a salvage station's cannot be. Another
/// Scrap of one coming apart, as a selection's, changes nothing, and one counting down to its scuttling is not taken
/// apart. Boarded, it is whole for its new owner; dying or destroyed, it gives nothing; its planet taken, it stays its
/// empire's, out of the captor's orbitals. A subspace projector still vanishes at once. A save keeps the dismantling
/// where it was.
/// </summary>
[TestClass]
public class DismantleOrbitalTests : StarDriveTest
{
    const string Platform = "Platform Base mk1-a";
    const string PatrolStation = "Terran Small Patrol Station 1";
    const string BigStation = "T-Mining-Station mk1-a";
    const string Projector = "Subspace Projector";
    const string SalvageStation = "Terran Basic Salvage Station";
    readonly Planet Colony;

    public DismantleOrbitalTests()
    {
        LoadStarterShips(Platform, PatrolStation, BigStation, Projector, SalvageStation, "Assault Shuttle");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Colony = AddHomeWorldToEmpire(new Vector2(200_000f), Player);
    }

    Ship Orbital(string design, Planet at = null)
    {
        at ??= Colony;
        Ship orbital = SpawnShip(design, Player, at.Position + new Vector2(3_000f, 0));
        orbital.TetherToPlanet(at);
        Assert.IsTrue(at.OrbitalStations.Contains(orbital), "setup: the orbital is among its planet's");
        return orbital;
    }

    static DismantleOrbital GoalOf(Ship orbital)
        => (DismantleOrbital)orbital.Loyalty.AI.FindGoal(g => g is DismantleOrbital && g.OldShip == orbital);

    // the empire turns of its dismantling
    static void Turns(Ship orbital, int turns)
    {
        DismantleOrbital goal = GoalOf(orbital);
        for (int i = 0; i < turns; ++i)
            goal.Evaluate();
    }

    [TestMethod]
    public void TheTurnsAreOnePerFiftySlotsFromThreeToTwentyWhateverThePace()
    {
        Ship platform = SpawnShip(Platform, Player, Colony.Position + new Vector2(3_000f, 0));
        Ship patrol = SpawnShip(PatrolStation, Player, Colony.Position + new Vector2(6_000f, 0));
        Ship big = SpawnShip(BigStation, Player, Colony.Position + new Vector2(9_000f, 0));
        AssertEqual(30, platform.SurfaceArea, "setup: the platform has 30 slots");
        AssertEqual(356, patrol.SurfaceArea, "setup: the small station 356");
        AssertEqual(954, big.SurfaceArea, "setup: the big station 954");

        AssertEqual(3, DismantleOrbital.Turns(platform), "30 slots take the least, 3 turns");
        AssertEqual(8, DismantleOrbital.Turns(patrol), "356 slots take 8, rounded up");
        AssertEqual(20, DismantleOrbital.Turns(big), "954 slots take the most, 20");

        UState.P.Pace = 3f;
        AssertEqual(8, DismantleOrbital.Turns(patrol), "whatever the pace");
    }

    [TestMethod]
    public void ScrapTakesAnOrbitalApartWhereItStandsAndItsPlanetGetsHalfItsCostByItsHealth()
    {
        Ship platform = Orbital(Platform);
        platform.Health = platform.HealthMax * 0.8f;
        platform.CargoSpaceMax = 100f;
        platform.LoadProduction(10f);
        float cost = platform.GetCost(Player);
        Colony.ProdHere = 0f;

        platform.AI.OrderScrapShip();
        Assert.IsTrue(platform.Active && platform.Dismantling, "Scrap takes it apart where it stands");
        Assert.IsFalse(Colony.OrbitalStations.Contains(platform), "it leaves its planet's orbitals at once");
        AssertEqual(AIState.Scrap, platform.AI.State, "being scrapped");

        Turns(platform, 2);
        Assert.IsTrue(platform.Active && platform.Dismantling, "it stands for its 3 turns");
        AssertEqual(0f, Colony.ProdHere, "the planet gets nothing yet");

        Turns(platform, 1);
        Assert.IsFalse(platform.Active, "after its third it is gone");
        Assert.IsNull(GoalOf(platform), "its dismantling over");
        AssertEqual(0.01f, cost * 0.5f * 0.8f + 10f, Colony.ProdHere, "the planet gets half its cost by its health, and the production aboard");
    }

    [TestMethod]
    public void WhatThePlanetsStoreHasNoRoomForIsPaidAsCreditsAtTheTaxRate()
    {
        Ship platform = Orbital(Platform);
        float refund = platform.GetCost(Player) * 0.5f;
        Colony.Storage.Max = 100f;
        Colony.ProdHere = 100f - refund + 4f;
        Player.data.TaxRate = 0.25f;
        float excess = Player.ExcessCargoMoney;

        platform.AI.OrderScrapShip();
        Turns(platform, 3);
        AssertEqual(0.01f, 100f, Colony.ProdHere, "the store fills");
        AssertEqual(0.01f, excess + 4f * 0.25f, Player.ExcessCargoMoney, "and the 4 it has no room for are paid with the turn's income, at the tax rate");
    }

    [TestMethod]
    public void AtAPlanetNotTheEmpiresItGivesNothingToThatPlanet()
    {
        Planet other = AddDummyPlanet(new Vector2(400_000f), 1f, 1f, 1f, new Vector2(400_000f, 410_000f), explored: true);
        other.SetOwner(Enemy);
        Ship platform = Orbital(Platform, other);
        float stored = other.ProdHere;

        platform.AI.OrderScrapShip();
        Turns(platform, 3);
        Assert.IsFalse(platform.Active, "it is gone");
        AssertEqual(stored, other.ProdHere, "and the planet, another empire's, gets nothing");
    }

    [TestMethod]
    public void ComingApartItStopsAtOnce()
    {
        Ship station = Orbital(PatrolStation);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(station.ShieldPower > 0f, "setup: its shields are up");
        Assert.IsTrue(station.GetMaintCost() > 0f && station.CanBeRefitted, "setup: it pays upkeep and can be refitted");
        Weapon weapon = station.Weapons.First();
        Assert.IsTrue(CanFire(weapon), "setup: its weapons can fire");
        ShipModule bay = station.Carrier.AllFighterHangars.First();
        Assert.IsTrue(station.Carrier.PrepHangarShip(Player, bay, out _), "setup: its bays can launch");

        station.AI.OrderScrapShip();
        Assert.IsFalse(CanFire(weapon), "coming apart, its weapons cannot fire");
        Assert.IsFalse(station.Carrier.PrepHangarShip(Player, bay, out _), "nor its bays launch");
        AssertEqual(0f, station.GetMaintCost(), "it pays no upkeep");
        Assert.IsFalse(station.CanBeRefitted, "and cannot be refitted");
        RunObjectsSim(2f);
        AssertEqual(0f, station.ShieldPower, "its shields are down");
        Assert.IsFalse(station.GetShields().Any(s => s.ShieldsAreActive), "every one of them");
    }

    [TestMethod]
    public void ComingApartItDoesNotRepair()
    {
        Ship station = Orbital(PatrolStation);
        Ship control = Orbital(PatrolStation);
        station.AI.OrderScrapShip();
        FieldInfo damagedAt = typeof(Ship).GetField("LastDamagedTime", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (Ship s in new[] { station, control })
        {
            foreach (ShipModule m in s.Modules)
                m.SetHealth(m.ActualMaxHealth * 0.5f, "Test");
            damagedAt.SetValue(s, -100f); // out of combat long enough to repair
        }
        float health = station.Health;
        AssertEqual(0.01f, health, control.Health, "setup: both are damaged alike");

        RunObjectsSim(5f);
        Assert.IsTrue(control.Health > health + 1f, "setup: one at work repairs");
        AssertEqual(0.01f, health, station.Health, "one coming apart does not");
    }

    static bool CanFire(Weapon weapon)
        => (bool)typeof(Weapon).GetMethod("CanFireWeapon", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(weapon, null);

    [TestMethod]
    public void ScrapAgainCallsItOffTheOrbitalWholeAndAtWork()
    {
        Ship station = Orbital(PatrolStation);
        station.AI.OrderScrapShip();
        Turns(station, 2);
        RunObjectsSim(UState.P.TurnTimer);
        Assert.IsTrue(station.DismantlePercent > 0f, "setup: it is coming apart");

        station.ScrapFromButton();
        Universe.InvokePendingSimThreadActions();
        Assert.IsFalse(station.Dismantling, "Scrap again calls it off");
        Assert.IsNull(GoalOf(station), "its dismantling dropped");
        AssertEqual(0f, station.DismantlePercent, "its progress undone");
        Assert.IsTrue(Colony.OrbitalStations.Contains(station), "back among its planet's orbitals");
        Assert.AreNotEqual(AIState.Scrap, station.AI.State, "no longer being scrapped");
        Assert.IsTrue(station.GetMaintCost() > 0f && CanFire(station.Weapons.First()), "and at work again, paying its upkeep");
    }

    [TestMethod]
    public void TheDoingScrapButtonStartsItAndCallsItOff()
    {
        Ship station = Orbital(PatrolStation);
        Assert.IsFalse(station.DoingScrap, "setup: not being scrapped");
        station.DoingScrap = true;
        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(station.Dismantling && station.DoingScrap, "the panel's Scrap starts it, the button lit");

        station.DoingScrap = false;
        Universe.InvokePendingSimThreadActions();
        Assert.IsFalse(station.Dismantling || station.DoingScrap, "and calls it off");
        Assert.IsNull(GoalOf(station), "its dismantling dropped");
    }

    [TestMethod]
    public void AnotherScrapOfOneComingApartChangesNothing()
    {
        Ship platform = Orbital(Platform);
        platform.AI.OrderScrapShip();
        Turns(platform, 1);

        platform.AI.OrderScrapShip(); // as a selection's Scrap gives every ship in it
        Assert.IsTrue(platform.Dismantling, "it still comes apart");
        AssertEqual(1, Player.AI.CountGoals(g => g is DismantleOrbital), "with its one dismantling");
        Turns(platform, 2);
        Assert.IsFalse(platform.Active, "which ends it on time");
    }

    [TestMethod]
    public void OneCountingDownToItsScuttlingIsNotTakenApart()
    {
        Ship platform = Orbital(Platform);
        platform.ScuttleTimer = 10f;
        platform.AI.OrderScrapShip();
        Assert.IsFalse(platform.Dismantling, "a scuttling goes on");
        Assert.IsNull(GoalOf(platform), "with no dismantling");
    }

    [TestMethod]
    public void ItCannotBeCalledOffOnceItsPlaceIsTaken()
    {
        Ship platform = Orbital(Platform);
        platform.AI.OrderScrapShip();
        Assert.IsTrue(DismantleOrbital.CanCallOff(platform), "setup: its place is free");
        while (Colony.OrbitalStations.Count < ShipBuilder.OrbitalsLimit)
            SpawnShip(Platform, Player, Colony.Position + new Vector2(5_000f, 0)).TetherToPlanet(Colony);

        platform.ScrapFromButton();
        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(platform.Dismantling && GoalOf(platform) != null, "with the planet's orbitals full again it comes apart on");
        Assert.IsFalse(Colony.OrbitalStations.Contains(platform), "and stays out of them");
    }

    [TestMethod]
    public void ASalvageStationsCannotBeCalledOff()
    {
        Ship station = SpawnShip(SalvageStation, Player, new Vector2(600_000f));
        station.AI.OrderScrapShip();
        Assert.IsTrue(station.Dismantling, "setup: a salvage station comes apart");
        Assert.IsFalse(DismantleOrbital.CanCallOff(station), "its work ended with its orders");

        station.ScrapFromButton();
        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(station.Dismantling && GoalOf(station) != null, "so Scrap again does not call it off");
    }

    [TestMethod]
    public void ItsPlanetTakenItStaysItsEmpiresOutOfTheCaptorsOrbitals()
    {
        Ship station = Orbital(PatrolStation);
        station.AI.OrderScrapShip();
        Colony.Construction.ClearQueue();
        Colony.SetOwner(Enemy);
        Assert.IsTrue(station.Loyalty == Player && station.Dismantling, "the planet taken, it stays the player's, coming apart");
        Assert.IsFalse(DismantleOrbital.CanCallOff(station), "and cannot be called off");

        Player.AI.ClearGoals(); // as a defeat clears the empire's goals
        Assert.IsFalse(station.Dismantling, "made whole as its dismantling goes");
        Assert.IsFalse(Colony.OrbitalStations.Contains(station), "but kept out of the captor's orbitals");
    }

    [TestMethod]
    public void BoardedItIsWholeForItsNewOwner()
    {
        Ship station = Orbital(PatrolStation);
        station.AI.OrderScrapShip();
        DismantleOrbital goal = GoalOf(station);

        station.LoyaltyChangeByGift(Enemy, addNotification: false);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(station.Loyalty == Enemy && !station.Dismantling, "its new owner gets it whole");
        Assert.IsTrue(Colony.OrbitalStations.Contains(station), "back among its planet's orbitals");

        goal.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(goal), "and its old owner's dismantling ends");
        Assert.IsTrue(station.Active && !station.Dismantling, "leaving it whole");
    }

    [TestMethod]
    public void ItsTroopsLeaveInShuttlesAsItGoes()
    {
        Ship station = Orbital(PatrolStation);
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(station);
        int troops = station.TroopCount;
        Assert.IsTrue(troops > 0, "setup: troops aboard");

        station.AI.OrderScrapShip();
        Turns(station, 8);
        Assert.IsFalse(station.Active, "setup: it is gone");
        RunObjectsSim(TestSimStep);
        Ship[] shuttles = UState.Objects.GetShips().Filter(s => s.Loyalty == Player && s.IsDefaultAssaultShuttle);
        AssertEqual(troops, shuttles.Length, "each of its troops leaves in an assault shuttle first");
    }

    [TestMethod]
    public void DestroyedComingApartItGivesNothing()
    {
        Ship platform = Orbital(Platform);
        Colony.ProdHere = 0f;
        platform.AI.OrderScrapShip();
        Turns(platform, 2);

        platform.Die(null, cleanupOnly: true);
        Turns(platform, 1);
        Assert.IsNull(GoalOf(platform), "its dismantling ends with it on its last turn");
        AssertEqual(0f, Colony.ProdHere, "and the planet gets nothing");
    }

    [TestMethod]
    public void DyingOnItsLastTurnItGivesNothing()
    {
        Ship platform = Orbital(Platform);
        Colony.ProdHere = 0f;
        platform.AI.OrderScrapShip();
        Turns(platform, 2);

        platform.Dying = true;
        Turns(platform, 1);
        Assert.IsTrue(platform.Active && GoalOf(platform) != null, "dying on its last turn, it is not taken away");
        AssertEqual(0f, Colony.ProdHere, "and the planet gets nothing");
    }

    [TestMethod]
    public void ASubspaceProjectorScrappedStillVanishesAtOnce()
    {
        Ship projector = SpawnShip(Projector, Player, Colony.Position + new Vector2(20_000f, 0));
        Assert.IsFalse(DismantleOrbital.DismantledInPlace(projector), "a projector is not taken apart");
        projector.AI.OrderScrapShip();
        Assert.IsFalse(projector.Active, "it vanishes at once");
    }

    [TestMethod]
    public void TheGovernorScrapsAnOrbitalByTakingItApart()
    {
        Colony.CType = Planet.ColonyType.Core;
        Colony.HasSpacePort = true;
        Colony.GovOrbitals = true;
        Colony.ManualOrbitals = true;
        Colony.SetWantedPlatforms(0);
        Colony.SetWantedStations(0);
        Colony.SetWantedShipyards(0);
        Colony.SetManualSpaceDefBudget(1000);
        Colony.Population = Colony.MaxPopulation;
        Colony.UpdateIncomes();
        Ship platform = Orbital(Platform);
        float stored = Colony.ProdHere;

        Colony.DoGoverning();
        Assert.IsTrue(platform.Active && platform.Dismantling, "the governor's scrapping takes it apart where it stands");
        AssertEqual(stored, Colony.ProdHere, "its refund waits for its end");
        Colony.DoGoverning();
        Assert.IsTrue(platform.Dismantling, "it comes apart on");
        AssertEqual(1, Player.AI.CountGoals(g => g is DismantleOrbital), "and the governor, its slot free, scraps it once");
    }

    [TestMethod]
    public void ASaveKeepsTheDismantlingWhereItWas()
    {
        Ship station = Orbital(PatrolStation);
        station.AI.OrderScrapShip();
        Turns(station, 3);
        RunObjectsSim(UState.P.TurnTimer * 4f);
        float percent = station.DismantlePercent;
        AssertEqual(2f, 50f, percent, "setup: half way through its 8 turns");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.DismantleOrbital", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Ship loadedStation = loaded.UState.Objects.FindShip(station.Id);
        Planet loadedColony = loaded.UState.Player.GetPlanets().First(p => p.Name == Colony.Name);
        Assert.IsTrue(loadedStation.Dismantling, "it is still coming apart");
        AssertEqual(0.01f, percent, loadedStation.DismantlePercent, "as far along");
        Assert.IsFalse(loadedColony.OrbitalStations.Contains(loadedStation), "out of its planet's orbitals");
        Assert.IsTrue(loadedStation.GetTether() == loadedColony, "though still at its planet");

        DismantleOrbital goal = GoalOf(loadedStation);
        for (int i = 0; i < 4; ++i)
            goal.Evaluate();
        Assert.IsTrue(loadedStation.Active, "its turns left kept");
        goal.Evaluate();
        Assert.IsFalse(loadedStation.Active, "and it is gone after the last of them");
    }
}
