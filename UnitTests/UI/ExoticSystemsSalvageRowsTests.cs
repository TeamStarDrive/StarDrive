using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.UI;

/// <summary>
/// The Exotic Systems screen gives each system with salvageable derelicts the player knows of a salvage row of its
/// own, beside any research or Dyson swarm row of that system, with how many such derelicts there are and the
/// production left in them. Derelicts the player does not know of, those in other systems and those that ran out of
/// turns do not count. The salvage row is never a research or Dyson swarm row.
/// </summary>
[TestClass]
public class ExoticSystemsSalvageRowsTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 OtherSite = new(-600_000f, 600_000f);
    static readonly Vector2 UnknownSite = new(600_000f, -600_000f);
    static readonly Vector2 InSystem = SystemSite + new Vector2(30_000f, 0);
    readonly SolarSystem WreckSystem;
    readonly SolarSystem OtherSystem;
    readonly SolarSystem UnknownSystem;

    public ExoticSystemsSalvageRowsTests()
    {
        LoadStarterShips("Dreadnought mk1-a");
        CreateUniverseAndPlayerEmpire();
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        OtherSystem = AddDummyPlanet(OtherSite, 1f, 1f, 1f, OtherSite + new Vector2(0, 60_000f), explored: true).System;
        UnknownSystem = AddDummyPlanet(UnknownSite, 1f, 1f, 1f, UnknownSite + new Vector2(0, 60_000f), explored: true).System;
        AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        Universe.EmpireUI ??= new EmpireUIOverlay(Player, null, Universe);
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
        return UState.Derelicts.Current.Last();
    }

    ExoticSystemsListScreenItem[] OpenRows()
    {
        var screen = new ExoticSystemsListScreen(Universe, Universe.EmpireUI);
        Game.Manager.AddScreenAndLoadContent(screen);
        try
        {
            screen.ResetList();
            return screen.GetElements().OfType<ScrollList<ExoticSystemsListScreenItem>>().Single().AllEntries.ToArray();
        }
        finally
        {
            Game.Manager.RemoveScreen(screen);
        }
    }

    [TestMethod]
    public void ASystemWithDerelictsThePlayerKnowsOfGetsASalvageRowWithTheirCountAndProduction()
    {
        Derelict ranOut = Forms(Player, InSystem);
        while (ranOut.TurnsLeft > 1)
            UState.Derelicts.UpdateTurn();
        Derelict first = Forms(Player, InSystem + new Vector2(0, 10_000f));
        Derelict second = Forms(Enemy, InSystem + new Vector2(0, 20_000f));
        UState.Derelicts.UpdateTurn();
        Assert.IsFalse(ranOut.CanBeSalvaged, "setup: the first one ran out of turns");
        Assert.IsTrue(first.CanBeSalvaged && second.KnownByPlayer, "setup: the player knows of every other one there");

        Derelict elsewhere = Forms(Player, OtherSite + new Vector2(30_000f, 0));
        Derelict unknown = Forms(Enemy, UnknownSite + new Vector2(30_000f, 0));
        Assert.IsTrue(elsewhere.System == OtherSystem && unknown.System == UnknownSystem, "setup: one in each other system");
        Assert.IsFalse(unknown.KnownByPlayer, "setup: the player does not know of the one in the third system");

        ExoticSystemsListScreenItem[] salvageRows = OpenRows().Where(r => r.IsForSalvage).ToArray();
        AssertEqual(2, salvageRows.Length, "a row for each system with derelicts the player knows of");
        ExoticSystemsListScreenItem salvage = salvageRows.Single(r => r.System == WreckSystem);
        AssertEqual(2, salvage.NumDerelicts, "the ones there that can still be salvaged");
        Assert.AreSame(first, salvage.FirstDerelict, "the row shows the first of them");
        AssertEqual(0.01f, first.ProductionLeft + second.ProductionLeft, salvage.SalvageProduction, "and the production left in them");
        AssertEqual(1, salvageRows.Single(r => r.System == OtherSystem).NumDerelicts, "each system counts its own");
        Assert.IsFalse(salvage.IsForResearch || salvage.IsForMining || salvage.IsForDysonSwarm, "a salvage row is no other kind");
    }

    [TestMethod]
    public void ASystemWithAResearchRowGetsItsOwnSalvageRowToo()
    {
        WreckSystem.SetResearchable(true, UState);
        Forms(Player, InSystem);

        ExoticSystemsListScreenItem[] rows = OpenRows().Where(r => r.IsStar && r.System == WreckSystem).ToArray();
        AssertEqual(2, rows.Length, "one research row and one salvage row");
        Assert.IsTrue(rows.Any(r => r.IsForResearch && !r.IsForSalvage), "the research row");
        Assert.IsTrue(rows.Any(r => r.IsForSalvage && !r.IsForResearch), "the salvage row");
        Assert.IsFalse(OpenRows().Any(r => r.System == OtherSystem), "no row for a system with nothing in it");
    }

    [TestMethod]
    public void ASalvageRowIsNeitherAResearchNorADysonSwarmRow()
    {
        WreckSystem.SetResearchable(true, UState);
        WreckSystem.SetDysonSwarmType(1);
        Forms(Player, InSystem);

        ExoticSystemsListScreenItem salvage = OpenRows().Single(r => r.IsForSalvage);
        Assert.IsFalse(salvage.IsForResearch || salvage.IsForDysonSwarm, "a researchable Dyson swarm system's salvage row is neither");
    }
}
