using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A salvage site lies in the system, clear of its star and planets' orbits and 2000 units from its salvageable
/// derelicts and from the salvage ships and salvage stations there, any empire's. The search for one starts within
/// 1000 of the derelicts' average spot and widens until a point is clear, and gives up when no point in the system is.
/// The salvage ships and stations an empire keeps clear of are its own and those in its sensors, while they last. A
/// salvage platform costs its builder trust with each AI that has a colony in its system and knows it, unless the two
/// have open borders, an alliance or a war: the AI personality's value, 5 for all, times a difficulty multiplier when
/// the builder is the player. The value survives a save, 0 included, and a loaded empire passes over its own colony.
/// </summary>
[TestClass]
public class SalvageSiteTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 InSystem = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 BuildPos = SystemSite + new Vector2(40_000f, 0);
    const string SalvageShip = "Terran Salvage Ship";
    const string OldStation = "TEST_Old Salvage Station";
    readonly SolarSystem WreckSystem;

    public SalvageSiteTests()
    {
        LoadStarterShips("Dreadnought mk1-a", SalvageShip, OldStation, ShipDesign.SalvagePlatformName);
        CreateUniverseAndPlayerEmpire();
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        Player.UpdateRallyPoints();
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    void Forms(Empire owner, Vector2 at)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
    }

    [TestMethod]
    public void ASiteIsInTheSystemAndClearOfItsStarPlanetsDerelictsAndSalvageShipsAndStations()
    {
        Forms(Player, InSystem);
        Planet planet = WreckSystem.PlanetList[0];
        Ship rival = SpawnShip(SalvageShip, Enemy, SystemSite + new Vector2(0, -40_000f));
        Ship station = SpawnShip(OldStation, Enemy, SystemSite + new Vector2(-40_000f, 0));
        Ship other = SpawnShip("Dreadnought mk1-a", Enemy, SystemSite + new Vector2(0, 40_000f));
        Ship[] ships = { rival, station, other };
        bool Valid(Vector2 pos) => SalvageMission.IsValidSite(WreckSystem, pos, ships);

        Assert.IsTrue(Valid(BuildPos), "in the system, clear of everything");
        float near = SalvageMission.Clearance - 100f, clear = SalvageMission.Clearance + 100f;
        Assert.IsFalse(Valid(InSystem + new Vector2(near, 0)), "too near a salvageable derelict");
        Assert.IsTrue(Valid(InSystem + new Vector2(clear, 0)), "just clear of it");
        Assert.IsFalse(Valid(rival.Position + new Vector2(near, 0)), "too near a salvage ship, any empire's");
        Assert.IsTrue(Valid(rival.Position + new Vector2(clear, 0)), "just clear of it");
        Assert.IsFalse(Valid(station.Position + new Vector2(0, near)), "too near a salvage station");
        Assert.IsTrue(Valid(other.Position + new Vector2(near, 0)), "other ships do not count");
        Assert.IsFalse(Valid(planet.Position + new Vector2(DeepSpaceBuildingWindow.TetherRadius(planet) - 100f, 0)), "in a planet's orbit");
        Assert.IsFalse(Valid(SystemSite + new Vector2(DeepSpaceBuildingWindow.MinimumBuildDistanceFromSun - 100f, 0)), "too near the star");
        Assert.IsFalse(Valid(SystemSite + new Vector2(WreckSystem.Radius * DeepSpaceBuildingWindow.SystemPlacementRadius + 100f, 0)),
                       "outside the system's placement area");
    }

    [TestMethod]
    public void TheSiteSearchStartsWithin1000OfTheDerelictsAverageSpot()
    {
        Forms(Player, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Vector2 average = InSystem + new Vector2(0, 5_000f);

        Assert.IsTrue(SalvageMission.FindSite(WreckSystem, Empty<Ship>.Array, new SeededRandom(1), out Vector2 site), "a site is found");
        Assert.IsTrue(site.InRadius(average, SalvageMission.FirstSearchRadius), "within 1000 of the derelicts' average spot, where it is clear");
    }

    [TestMethod]
    public void TheSiteSearchWidensUntilAPointIsClear()
    {
        Forms(Player, InSystem);
        Ship rival = SpawnShip(SalvageShip, Enemy, InSystem + new Vector2(3_000f, 0));
        for (int seed = 1; seed <= 20; ++seed)
        {
            Assert.IsTrue(SalvageMission.FindSite(WreckSystem, new[] { rival }, new SeededRandom(seed), out Vector2 site), $"seed {seed}: a site is found");
            Assert.IsTrue(SalvageMission.IsValidSite(WreckSystem, site, new[] { rival }), $"seed {seed}: and it is a valid one");
            Assert.IsTrue(site.OutsideRadius(InSystem, SalvageMission.Clearance), $"seed {seed}: clear of the derelict, which sits on the average spot");
        }

        SolarSystem empty = AddDummyPlanet(SystemSite + new Vector2(0, 400_000f), 1f, 1f, 1f, SystemSite + new Vector2(0, 460_000f), explored: true).System;
        Assert.IsFalse(SalvageMission.FindSite(empty, Empty<Ship>.Array, new SeededRandom(1), out _), "no site in a system without derelicts");
    }

    [TestMethod]
    public void TheSiteSearchGivesUpWhenNoPointInTheSystemIsClear()
    {
        Forms(Player, InSystem);
        WreckSystem.Radius = DeepSpaceBuildingWindow.MinimumBuildDistanceFromSun / DeepSpaceBuildingWindow.SystemPlacementRadius;
        Assert.IsFalse(SalvageMission.FindSite(WreckSystem, Empty<Ship>.Array, new SeededRandom(1), out _),
                       "a system whose placement area lies within the star's 20000 has no site, and the search ends");
    }

    [TestMethod]
    public void ASalvageShipOrStationStillThereIsSeenWhenItIsOursOrInOurSensors()
    {
        Ship ours = SpawnShip(SalvageShip, Player, SystemSite + new Vector2(0, -40_000f));
        Ship unseen = SpawnShip(SalvageShip, Enemy, SystemSite + new Vector2(0, -50_000f));
        Ship seen = SpawnShip(SalvageShip, Enemy, SystemSite + new Vector2(0, -60_000f));
        Ship station = SpawnShip(OldStation, Enemy, SystemSite + new Vector2(0, -80_000f));
        Ship warship = SpawnShip("Dreadnought mk1-a", Player, SystemSite + new Vector2(0, -70_000f));
        seen.KnownByEmpires.SetSeen(Player);
        station.KnownByEmpires.SetSeen(Player);

        Ship[] sites = SalvageMission.SitesSeenBy(Player, new[] { ours, unseen, seen, station, warship });
        Assert.IsTrue(sites.Contains(ours) && sites.Contains(seen), "our own salvage ship and one in our sensors");
        Assert.IsTrue(sites.Contains(station), "and a salvage station in our sensors");
        Assert.IsFalse(sites.Contains(unseen), "not one we do not see");
        Assert.IsFalse(sites.Contains(warship), "and no other ship");

        ours.Die(null, cleanupOnly: true);
        Assert.IsFalse(SalvageMission.SitesSeenBy(Player, new[] { ours, seen }).Contains(ours), "nor one that is gone");
    }

    [TestMethod]
    public void OnlyAnAIThatKnowsTheBuilderWithNoOpenBordersAllianceOrWarLosesTrust()
    {
        Assert.IsTrue(ResourceManager.DiplomaticTraits.DiplomaticTraitsList.All(t => t.SalvageStationTrustLoss == 5f),
                      "every personality loses 5, as none sets its own");
        CreateThirdMajorEmpire();
        WreckSystem.PlanetList[0].SetOwner(Enemy);
        Relationship aiToPlayer = Enemy.GetRelations(Player), aiToAI = Enemy.GetRelations(ThirdMajor);
        aiToPlayer.AtWar = Player.GetRelations(Enemy).AtWar = false;
        aiToAI.AtWar = ThirdMajor.GetRelations(Enemy).AtWar = false;
        float Loss(Empire builder)
        {
            Relationship rel = Enemy.GetRelations(builder);
            float before = rel.Trust;
            SalvageMission.CostTrustOfColonyOwners(builder, WreckSystem);
            return before - rel.Trust;
        }

        UState.P.Difficulty = GameDifficulty.Insane;
        Enemy.TestInitModifiers();
        AssertEqual(5f * 1.75f, Loss(Player), "the player loses more at a harder difficulty");
        AssertEqual(5f, Loss(ThirdMajor), "another AI does not");

        Empire stranger = UState.CreateEmpire(ResourceManager.MajorRaces.First(e => UState.GetEmpireByName(e.Name) == null), isPlayer: false);
        Enemy.GetRelations(stranger).AtWar = false;
        Assert.IsFalse(Enemy.IsKnown(stranger), "setup: an empire the AI has not met");
        AssertEqual(0f, Loss(stranger), "none from an empire the AI has not met");

        aiToPlayer.Treaty_OpenBorders = true;
        AssertEqual(0f, Loss(Player), "none with open borders");
        aiToPlayer.Treaty_OpenBorders = false;
        aiToPlayer.Treaty_Alliance = true;
        AssertEqual(0f, Loss(Player), "nor with an alliance");
        aiToPlayer.Treaty_Alliance = false;
        aiToPlayer.AtWar = true;
        AssertEqual(0f, Loss(Player), "nor at war");
        aiToPlayer.AtWar = false;

        GlobalStats.RestrictAIPlayerInteraction = true;
        try
        {
            AssertEqual(0f, Loss(Player), "nor with the AI kept away from the player");
        }
        finally
        {
            GlobalStats.RestrictAIPlayerInteraction = false;
        }

        WreckSystem.PlanetList[0].SetOwner(ThirdMajor);
        AssertEqual(0f, Loss(Player), "and none once the AI has no colony there");

        WreckSystem.PlanetList[0].SetOwner(Player);
        Relationship playerToAI = Player.GetRelations(ThirdMajor);
        float playerTrust = playerToAI.Trust;
        SalvageMission.CostTrustOfColonyOwners(ThirdMajor, WreckSystem);
        AssertEqual(playerTrust, playerToAI.Trust, "and a player's colony costs nothing, as the player keeps no trust");
    }

    [TestMethod]
    public void InALoadedGameAnAIBuilderPassesOverItsOwnColonyAndATrustLossOf0Holds()
    {
        CreateThirdMajorEmpire();
        WreckSystem.PlanetList[0].SetOwner(ThirdMajor);
        Enemy.TestSetPersonality("Cunning");
        Enemy.data.DiplomaticPersonality.SalvageStationTrustLoss = 0; // as a mod might set it
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageTrust", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire builder = loaded.UState.GetEmpireByName(ThirdMajor.Name);
        SolarSystem system = loaded.UState.Systems.First(s => s.Position == WreckSystem.Position);
        Assert.IsTrue(builder.IsKnown(builder) && system.HasPlanetsOwnedBy(builder), "setup: a loaded empire knows itself, and has a colony there");
        SalvageMission.CostTrustOfColonyOwners(builder, system); // it has no relationship with itself to ask about
        AssertEqual(0f, loaded.UState.GetEmpireByName(Enemy.Name).data.DiplomaticPersonality.SalvageStationTrustLoss,
                    "and a personality's loss of 0 survives the save");
    }
}
