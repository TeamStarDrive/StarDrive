using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A salvageable derelict holds a few techs, picked at random at its death from those that unlock its design's modules
/// and its hull, root techs left out: 2 for a frigate, 3 for a destroyer, 4 for a cruiser, 5 for a battleship, 6 for a
/// capital and 3 for an untethered station; a Remnant ship holds none. Each load its platform sends to the station
/// advances each of those techs the empire has not researched and sees on its tree by a fifth of what the tech has left
/// to research, times the load's share of the derelict's production, so the second of two loads pays a fifth of what
/// is left by then, and salvage never completes a tech. A load the station takes only part of pays for that part, and
/// the empire's module health bonus leaves the payout alone.
/// </summary>
[TestClass]
public class SalvageTechTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 Wreck = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 PlayerStation = SystemSite + new Vector2(33_000f, 0);
    readonly SolarSystem WreckSystem;
    readonly IShipDesign Station;

    public SalvageTechTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Terran Basic Salvage Station", ShipDesign.SalvagePlatformName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Basic Salvage Station", out Station), "setup: the station is loaded");
        Player.data.CurrentConstructor = "Terran Constructor";
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    Derelict Forms(Empire owner, Vector2 at)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        return UState.Derelicts.Current.Last();
    }

    Ship BuiltStation()
    {
        var order = new SalvageOps(Player, WreckSystem, PlayerStation, Station);
        Player.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, Player, PlayerStation);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order.TargetShip;
    }

    // the player's order with its platform built beside the derelict, about to strip
    SalvageDerelict AtWork(Derelict d, float hold)
    {
        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: ordered");
        SalvageDerelict order = SalvageDerelict.OrderOf(Player, d);
        Ship constructor = order.FinishedShip;
        Assert.IsTrue(d.TryClaim(Player), "setup: the constructor claims the derelict as it starts to build");
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, order.BuildPosition);
        platform.CargoSpaceMax = hold;
        constructor.QueueTotalRemoval();
        order.OnPlatformBuilt(platform, constructor);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the platform is built");
        return order;
    }

    // a tech of the derelict's, unresearched and on the player's tree, with this much of its cost researched
    TechEntry Learnable(string uid, float researched)
    {
        TechEntry tech = Player.GetTechEntry(uid);
        tech.Unlocked = false;
        tech.Discovered = true;
        tech.Progress = researched * tech.TechCost;
        Assert.IsFalse(tech.IsHidden(Player), $"setup: {uid} is not hidden from the player");
        return tech;
    }

    // two turns fill a hold of 24 and the third sends it to the station
    static void SendsALoadOf24(SalvageDerelict order, Ship station)
    {
        float before = station.GetProduction();
        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, before + 24f, station.GetProduction(), "setup: a load of 24 reaches the station");
    }

    [TestMethod]
    public void AWreckHoldsAsManyTechsAsItsHullRoleAtRandomFromThoseOfItsModulesAndHull()
    {
        AssertEqual(2, Derelict.TechsHeld(RoleName.frigate), "a frigate's wreck holds 2 techs");
        AssertEqual(3, Derelict.TechsHeld(RoleName.destroyer), "a destroyer's 3");
        AssertEqual(4, Derelict.TechsHeld(RoleName.cruiser), "a cruiser's 4");
        AssertEqual(5, Derelict.TechsHeld(RoleName.battleship), "a battleship's 5");
        AssertEqual(6, Derelict.TechsHeld(RoleName.capital), "a capital's 6");
        AssertEqual(3, Derelict.TechsHeld(RoleName.station), "an untethered station's 3");

        Array<string> techs = Derelict.TechsOf(ResourceManager.Ships.GetDesign("Dreadnought mk1-a"));
        AssertEqual(17, techs.Count, "the Dreadnought's modules and hull come from 17 techs past the roots");
        Assert.IsTrue(techs.Contains("Titans"), "its hull's tech among them");
        Assert.IsTrue(techs.Contains("CrystalArmor"), "and its armour's");
        Assert.IsFalse(techs.Contains("StarshipConstruction") || techs.Contains("SpaceWeapons"), "but no root tech, though two unlock its modules");

        var picks = new Array<string>();
        for (int i = 0; i < 5; ++i)
        {
            Derelict d = Forms(Player, Wreck + new Vector2(0, i * 5_000f));
            AssertEqual(6, d.Techs.Length, "a capital's wreck holds 6 of them");
            AssertEqual(6, d.Techs.Distinct().ToArray().Length, "each once");
            Assert.IsTrue(d.Techs.All(t => techs.Contains(t)), "all among its design's");
            picks.Add(string.Join(",", d.Techs.OrderBy(t => t)));
        }
        AssertGreaterThan(picks.Distinct().ToArray().Length, 1, "picked at random, so five wrecks do not all hold the same");
    }

    [TestMethod]
    public void ARemnantWreckHoldsNoTechs()
    {
        CreateAMinorFaction("Remnant");
        Assert.IsTrue(Faction.WeAreRemnants, "setup: the faction is the Remnants");
        AssertEqual(0, Forms(Faction, Wreck).Techs.Length, "a Remnant ship's wreck holds no tech");
    }

    [TestMethod]
    public void EachLoadAdvancesEachTechTheEmpireCanLearnByAFifthOfWhatIsLeftTimesTheLoadsShareOfTheWreck()
    {
        Derelict d = Forms(Player, Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry fresh = Learnable(d.Techs[0], 0f);
        TechEntry begun = Learnable(d.Techs[1], 0.4f);
        TechEntry researched = Learnable(d.Techs[2], 0f);
        researched.Unlocked = true;
        TechEntry undiscovered = Learnable(d.Techs[3], 0f);
        undiscovered.Discovered = false;
        float share = 24f / d.Production;

        SendsALoadOf24(order, station);
        AssertEqual(0.01f, 0.2f * share * fresh.TechCost, fresh.Progress, "a fifth of a fresh tech's cost, times the load's share of the wreck");
        AssertEqual(0.01f, (0.4f + 0.2f * share * 0.6f) * begun.TechCost, begun.Progress, "a fifth of what a begun one has left");
        AssertEqual(0f, researched.Progress, "nothing to a tech already researched");
        AssertEqual(0f, undiscovered.Progress, "nor to one not yet on the empire's tree");

        fresh.Progress = 0.6f * fresh.TechCost; // its research went on meanwhile
        SendsALoadOf24(order, station);
        AssertEqual(0.01f, (0.6f + 0.2f * share * 0.4f) * fresh.TechCost, fresh.Progress, "the next load pays a fifth of what is left by then");
    }

    [TestMethod]
    public void SalvageNeverCompletesATech()
    {
        Derelict d = Forms(Player, Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry tech = Learnable(d.Techs[0], 0.9f);

        SendsALoadOf24(order, station);
        AssertEqual(0.01f, (0.9f + 0.2f * 24f / d.Production * 0.1f) * tech.TechCost, tech.Progress, "a tech nearly done gains a fifth of the little it has left");
        AssertLessThan(tech.Progress, tech.TechCost, "so it is never complete");
        Assert.IsTrue(tech.Locked, "and stays unresearched");
    }

    [TestMethod]
    public void APartialLoadPaysOnlyForWhatTheStationTook()
    {
        Derelict d = Forms(Player, Wreck);
        Ship station = BuiltStation();
        station.CargoSpaceMax = 10f;
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry tech = Learnable(d.Techs[0], 0f);

        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 10f, station.GetProduction(), "setup: the station takes 10 of the load of 24");
        AssertEqual(0.01f, 14f, order.Platform.GetProduction(), "setup: the rest stays aboard");
        AssertEqual(0.01f, 0.2f * 10f / d.Production * tech.TechCost, tech.Progress, "the tech is paid for the 10 the station took");
    }

    [TestMethod]
    public void AnEmpiresModuleHealthBonusLeavesThePayoutAlone()
    {
        Derelict d = Forms(Player, Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry tech = Learnable(d.Techs[0], 0f);
        Player.data.Traits.ModHpModifier = 2.1f; // Skilled Engineers with the module health techs researched

        SendsALoadOf24(order, station);
        AssertEqual(0.01f, 0.2f * 24f / d.Production * tech.TechCost, tech.Progress, "every empire is paid a flat fifth");
    }

    [TestMethod]
    public void ATechHiddenFromTheEmpiresRaceIsNeverAdvanced()
    {
        Derelict d = Forms(Player, Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        TechEntry tech = Learnable(d.Techs[0], 0f);
        Array<Technology.RaceRequirements> hiddenFrom = tech.Tech.HiddenFrom;
        hiddenFrom.Add(new() { ShipType = Player.data.Traits.ShipType });
        try
        {
            Assert.IsTrue(tech.IsHidden(Player), "setup: the tech is hidden from the player's race");
            SendsALoadOf24(order, station);
            AssertEqual(0f, tech.Progress, "a tech hidden from the empire's race gains nothing");
        }
        finally
        {
            hiddenFrom.RemoveAt(hiddenFrom.Count - 1);
        }
    }
}
