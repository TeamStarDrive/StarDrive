using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
using Kind = Ship_Game.Ships.DerelictSalvageButton.Kind;

namespace UnitTests.Ships;

/// <summary>
/// Every few turns the planner orders an AI empire's salvage, or the player's with Auto Salvage on, in the systems it
/// has explored with derelicts it can order there, near its colonies, outside enemy borders unless its own reach there,
/// with no colony of a rival it is at peace with and has no open borders or alliance with; where it knows of hostile
/// strength the AI asks once for a fleet to clear the system instead, the player for none. A derelict with less than a
/// fifth of its turns left is not ordered; the rest go the fewest turns left first and the closest system first, the
/// ships the empire would recover recovered and the rest salvaged. A run orders as many as its salvage ships take, idle
/// ones whatever their number, and builds one at most, a system with no free site spending none of it, none while the
/// empire has as many salvage ships, built or queued, as one for every five planets, at least one and at most five;
/// the AI then scraps its idle salvage ships beyond that, the player's never. On automatic, the salvage ship is queued
/// at the shipyard that would finish it soonest, though another is nearer, and a mission whose orders all end while
/// its ship is queued stays in the queue, queued again at the next shipyard when its own is lost, the next order
/// anywhere that finds no idle ship taking it before a new build, and built with no order its ship waits idle at the
/// nearest colony. The AI scraps no idle salvage ship while an order whose ship was lost has yet to take another, and
/// on automatic such an order ends where the planner would stay out, while the player's own starts again.
/// With Auto Salvage on, a standing order's lit button calls nothing off, while
/// ordering and switching still click. The switch and the planner's timer survive a save. The Automation window's Auto
/// Salvage list resets a choice that can no longer be built to the race's, as it does one short of a load a unit. The
/// AI's salvage ships join no fleet, are neither scrapped as support ships beyond its budget nor refitted, and none
/// counts as a support ship it can build or is its pick for a support or defense ship.
/// </summary>
[TestClass]
public class SalvagePlannerTests : SalvageOrderTest
{
    const string EnemysShip = "Cordrazine Salvage Ship";
    readonly Planet Local; // in the derelicts' system
    readonly Planet EnemyHome;

    public SalvagePlannerTests()
    {
        Local = WreckSystem.PlanetList[0];
        WreckSystem.SetExploredBy(Enemy);
        Player.GetRelations(Enemy).AtWar = Enemy.GetRelations(Player).AtWar = false;
        EnemyHome = AddHomeWorldToEmpire(new Vector2(-1_000_000f, 0), Enemy);
        Enemy.UpdateRallyPoints();
        Assert.IsTrue(Enemy.ShipsWeCanBuildSnapshot.Any(s => s.Name == EnemysShip), "setup: the AI can build its race's salvage ship");
    }

    // the empire's planner on its turn, the AI's unless given, near enough wherever the system lies
    void Plans(Empire empire = null, bool ignoreDistance = true)
    {
        empire ??= Enemy;
        SalvageOpsPlanner planner = empire.AI.SalvageOpsAI ??= new SalvageOpsPlanner(empire);
        planner.TurnTimer = 1;
        planner.RunSalvageOpsPlanner(ignoreDistance);
    }

    static bool IsOrdered(Empire empire, Derelict d) => SalvageDerelict.OrderOf(empire, d) != null;

    // a salvageable derelict the AI knows of, its turns run down by this many
    Derelict Sinking(Vector2 at, int turnsGone)
    {
        Derelict d = Forms(at, Enemy);
        for (int i = 0; i < turnsGone; ++i)
            d.UpdateTurn();
        return d;
    }

    // the AI's colonies, its homeworld among them, in rows across the north of the galaxy
    void ColoniesOfTheAI(int count)
    {
        for (int i = Enemy.NumPlanets - 1; i < count - 1; ++i)
            AddDummyPlanetToEmpire(new Vector2(-1_750_000f + 350_000f * (i % 11), 1_050_000f + 350_000f * (i / 11)), Enemy);
        AssertEqual(count, Enemy.NumPlanets, $"setup: {count} colonies");
    }

    Ship IdleShipOfTheAI(float x) => IdleShip(EnemyHome.Position + new Vector2(x, 0), Enemy, EnemysShip);

    static QueueItem QueuedFor(Planet shipyard, Goal mission) => shipyard.ConstructionQueue.FirstOrDefault(q => q.Goal == mission);

    // two dreadnoughts in its queue, so another shipyard would finish a salvage ship sooner
    static void Busy(Planet shipyard)
    {
        IShipDesign dreadnought = ResourceManager.Ships.GetDesign("Dreadnought mk1-a");
        shipyard.Construction.Enqueue(dreadnought, QueueItemType.CombatShip);
        shipyard.Construction.Enqueue(dreadnought, QueueItemType.CombatShip);
        AssertEqual(2, shipyard.ConstructionQueue.Count, "setup: the shipyard is busy");
    }

    // a hostile, the player's unless given, the empire knows of in the system
    void HostileThere(Empire empire, Empire hostile = null)
    {
        hostile ??= Player;
        empire.GetRelations(hostile).AtWar = hostile.GetRelations(empire).AtWar = true;
        SpawnShipNoCombatHoldPos("Vulcan Scout", hostile, SystemSite + new Vector2(0, -20_000f));
        SpawnShipNoCombatHoldPos("Vulcan Scout", empire, SystemSite + new Vector2(0, -21_000f));
        UState.Objects.Update(new(time: 2f));
        empire.Threats.Update(new(time: 2f));
        AssertGreaterThan(empire.KnownEnemyStrengthNoResearchStationsIn(WreckSystem), 0f, "setup: the empire knows of hostile strength there");
    }

    [TestMethod]
    public void OnItsTurnThePlannerOrdersTheAIsDerelictBuildingItsRacesSalvageShipButNotWithSalvageOpsOff()
    {
        Derelict d = Forms(Wreck, Enemy);
        SalvageOpsPlanner planner = Enemy.AI.SalvageOpsAI;
        Assert.IsNotNull(planner, "setup: its planner comes with its AI");
        planner.TurnTimer = 2;
        planner.RunSalvageOpsPlanner(ignoreDistance: true);
        Assert.IsFalse(IsOrdered(Enemy, d), "not before its turn");

        UState.P.DisableSalvageOps = true;
        Plans();
        Assert.IsFalse(IsOrdered(Enemy, d), "nor with salvage ops off");

        UState.P.DisableSalvageOps = false;
        Plans();
        SalvageDerelict order = SalvageDerelict.OrderOf(Enemy, d);
        Assert.IsNotNull(order, "on its turn it orders the derelict");
        Assert.IsTrue(MissionOf(order).Building, "building a salvage ship for it");
        AssertEqual(EnemysShip, MissionOf(order).Design.Name, "its race's");
    }

    [TestMethod]
    public void ThePlannerRecoversTheShipsTheAIWouldRecoverAndSalvagesTheRest()
    {
        Derelict d = Forms(Wreck, Enemy);
        Assert.IsTrue(SalvageDerelict.WouldRecover(Enemy, d), "setup: the AI builds no ship of its hull role, so it would recover it");
        Plans();
        Assert.IsTrue(SalvageDerelict.OrderOf(Enemy, d) is { Recovering: true }, "and orders it recovered");

        IdleShipOfTheAI(5_000f);
        Derelict other = Forms(Wreck + new Vector2(0, 8_000f), Enemy);
        Assert.IsTrue(Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign("Dreadnought mk1-a")), "setup: the AI builds the same design");
        Assert.IsFalse(SalvageDerelict.WouldRecover(Enemy, other), "setup: so it would not recover it");
        Plans();
        Assert.IsTrue(SalvageDerelict.OrderOf(Enemy, other) is { Recovering: false }, "and orders it salvaged");
    }

    [TestMethod]
    public void ARunBuildsOneSalvageShipAtMostForTheDerelictWithTheFewestTurnsLeftAndNoneBeyondTheCap()
    {
        ColoniesOfTheAI(6);
        AssertEqual(2, SalvageOpsPlanner.Cap(Enemy), "setup: room for two salvage ships");
        Derelict last = Sinking(Wreck, turnsGone: 0);
        Derelict next = Sinking(Wreck + new Vector2(0, 8_000f), turnsGone: 50);
        Derelict first = Sinking(Wreck + new Vector2(0, -8_000f), turnsGone: 100);

        Plans();
        Assert.IsTrue(IsOrdered(Enemy, first), "a run builds a salvage ship for the derelict with the fewest turns left");
        Assert.IsFalse(IsOrdered(Enemy, next) || IsOrdered(Enemy, last), "and only one");
        Plans();
        Assert.IsTrue(IsOrdered(Enemy, next), "the next run another, for the next");
        Plans();
        Assert.IsFalse(IsOrdered(Enemy, last), "but with its two salvage ships queued, none for the third");
    }

    [TestMethod]
    public void ARunOrdersAsManyDerelictsAsTheShipItBuildsHasUnits()
    {
        CanBuild(Enemy, TwinShip);
        Derelict a = Forms(Wreck, Enemy);
        Derelict b = Forms(Wreck + new Vector2(0, 8_000f), Enemy);
        Derelict c = Forms(Wreck + new Vector2(0, -8_000f), Enemy);
        Plans();
        SalvageDerelict[] orders = new[] { a, b, c }.Select(d => SalvageDerelict.OrderOf(Enemy, d)).Where(o => o != null).ToArray();
        AssertEqual(2, orders.Length, "two of the three derelicts are ordered");
        AssertEqual(TwinShip, MissionOf(orders[0]).Design.Name, "for the ship with two units it builds");
        Assert.AreSame(MissionOf(orders[0]), MissionOf(orders[1]), "both on its mission");
    }

    [TestMethod]
    public void AnEmpireKeepsOneSalvageShipForEveryFivePlanetsAtLeastOneAndAtMostFive()
    {
        CreateThirdMajorEmpire();
        AssertEqual(0, ThirdMajor.NumPlanets, "setup: an empire with no planet");
        AssertEqual(1, SalvageOpsPlanner.Cap(ThirdMajor), "keeps one");
        ColoniesOfTheAI(5);
        AssertEqual(1, SalvageOpsPlanner.Cap(Enemy), "as does one with five");
        ColoniesOfTheAI(6);
        AssertEqual(2, SalvageOpsPlanner.Cap(Enemy), "one with six keeps two");
        ColoniesOfTheAI(26);
        AssertEqual(5, SalvageOpsPlanner.Cap(Enemy), "and none keeps more than five");
    }

    [TestMethod]
    public void IdleSalvageShipsTakeOrdersWhateverTheCapAndTheAIScrapsThoseLeftIdleBeyondIt()
    {
        Ship[] ships = { IdleShipOfTheAI(11_000f), IdleShipOfTheAI(8_000f), IdleShipOfTheAI(5_000f) }; // the one farthest from the system, left idle, last among the AI's ships
        Derelict a = Forms(Wreck, Enemy);
        Derelict b = Forms(Wreck + new Vector2(0, 8_000f), Enemy);
        AssertEqual(1, SalvageOpsPlanner.Cap(Enemy), "setup: room for one salvage ship");

        Plans();
        Assert.IsTrue(IsOrdered(Enemy, a) && IsOrdered(Enemy, b), "idle ships take orders beyond the cap");
        Assert.IsFalse(Enemy.AI.HasGoal(g => g is SalvageMission { Building: true }), "so none is built");
        AssertEqual(2, ships.Count(s => s.WorksForSalvage), "setup: two ships at work");
        Ship left = ships.Single(s => !s.WorksForSalvage);
        Assert.IsTrue(left.DoingScrap, "the one left idle beyond the cap is scrapped");
        Assert.IsFalse(ships.Any(s => s.WorksForSalvage && s.DoingScrap), "and none at work");
    }

    [TestMethod]
    public void OnAutomaticAnOrderWhoseShipWasLostWhereThePlannerWouldStayOutEndsWhileThePlayersOwnStartsAgain()
    {
        SalvageDerelict order = Ordered(Forms(Wreck, Enemy), empire: Enemy);
        Ship ship = ShipBuilt(order);
        Parked(order);
        HostileThere(Enemy);
        ship.Die(null, cleanupOnly: true);
        MissionOf(order).Evaluate();
        order.Evaluate();
        Assert.IsFalse(Enemy.AI.HasGoal(order), "on automatic, its ship lost where the planner would stay out, the order ends");

        SalvageDerelict players = Ordered(Forms(Wreck + new Vector2(0, 8_000f)));
        Ship playersShip = ShipBuilt(players);
        Parked(players);
        HostileThere(Player, hostile: Enemy);
        playersShip.Die(null, cleanupOnly: true);
        MissionOf(players).Evaluate();
        players.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(players) && MissionOf(players).Building, "the player's own order, off Auto Salvage, starts again");
    }

    [TestMethod]
    public void TheAIScrapsNoIdleSalvageShipBeyondTheCapWhileAnOrderWhoseShipWasLostHasYetToTakeOne()
    {
        AssertEqual(1, SalvageOpsPlanner.Cap(Enemy), "setup: room for one salvage ship");
        SalvageDerelict order = Ordered(Forms(Wreck, Enemy), empire: Enemy);
        Ship ship = ShipBuilt(order);
        Parked(order);
        Ship[] idle = { IdleShipOfTheAI(5_000f), IdleShipOfTheAI(8_000f) };
        ship.Die(null, cleanupOnly: true);
        MissionOf(order).Evaluate();

        Plans();
        Assert.IsFalse(idle.Any(s => s.DoingScrap), "no idle ship is scrapped while the order whose ship was lost has yet to take one");
        order.Evaluate();
        Assert.IsTrue(idle.Any(s => s.WorksForSalvage), "then the order takes one");
        Plans();
        Assert.IsTrue(idle.Single(s => !s.WorksForSalvage).DoingScrap, "and the one left idle beyond the cap is scrapped");
    }

    [TestMethod]
    public void WithAutoSalvageOnThePlannerOrdersThePlayersDerelictsButScrapsNoneOfItsShips()
    {
        Derelict d = Forms(Wreck);
        Ship[] ships = { IdleShip(Homeworld.Position + new Vector2(5_000f, 0)), IdleShip(Homeworld.Position + new Vector2(8_000f, 0)) };
        Plans(Player);
        Assert.IsFalse(IsOrdered(Player, d), "with Auto Salvage off the planner orders nothing for the player");

        Player.AutoSalvage = true;
        Plans(Player);
        Assert.IsTrue(IsOrdered(Player, d), "with it on it orders the player's derelict");
        Assert.IsTrue(ships.Any(s => s.WorksForSalvage), "setup: one ship at work");
        Assert.IsFalse(ships.Any(s => s.DoingScrap), "and scraps no ship of the player's left idle beyond the cap");
    }

    [TestMethod]
    public void WhereItKnowsOfHostileStrengthTheAIAsksOnceForAFleetInsteadAndThePlayerForNone()
    {
        HostileThere(Enemy);
        Derelict d = Forms(Wreck, Enemy);
        Plans();
        Assert.IsFalse(IsOrdered(Enemy, d), "no order where the AI knows of hostile strength");
        Assert.IsTrue(Enemy.AI.HasGoal(g => g is DefendSystem s && s.TargetSystem == WreckSystem), "it asks for a fleet to clear the system");
        Plans();
        AssertEqual(1, Enemy.AI.CountGoals(g => g is DefendSystem), "and asks once, though the fleet's task is not raised yet");

        Player.AutoSalvage = true;
        HostileThere(Player, hostile: Enemy);
        Derelict theirs = Forms(Wreck + new Vector2(0, 8_000f));
        Plans(Player);
        Assert.IsFalse(IsOrdered(Player, theirs), "nor for the player");
        Assert.IsFalse(Player.AI.HasGoal(g => g is DefendSystem), "which asks for no fleet");
    }

    [TestMethod]
    public void ThePlannerKeepsToSystemsTheEmpireHasExplored()
    {
        Vector2 farSite = new(-600_000f, 600_000f);
        SolarSystem far = AddDummyPlanet(farSite, 1f, 1f, 1f, farSite + new Vector2(0, 60_000f), explored: false).System;
        Derelict there = Forms(farSite + new Vector2(30_000f, 0), Enemy);
        Plans();
        Assert.IsFalse(IsOrdered(Enemy, there), "not in a system it has not explored");
        far.SetExploredBy(Enemy);
        Plans();
        Assert.IsTrue(IsOrdered(Enemy, there), "but once it has");
    }

    [TestMethod]
    public void ThePlannerKeepsToSystemsNearTheEmpiresColonies()
    {
        Derelict d = Forms(Wreck, Enemy);
        Enemy.WeightedCenter = EnemyHome.Position;
        Plans(ignoreDistance: false);
        Assert.IsFalse(IsOrdered(Enemy, d), "not in a system far from its colonies");
        Local.SetOwner(Enemy);
        WreckSystem.UpdateOwnerList();
        Plans(ignoreDistance: false);
        Assert.IsTrue(IsOrdered(Enemy, d), "but in one where it has a colony");
    }

    [TestMethod]
    public void ARivalsColonyKeepsThePlannerOutUnlessItHasOpenBordersOrAnAllianceWithThem()
    {
        Derelict d = Forms(Wreck, Enemy);
        Local.SetOwner(Player);
        WreckSystem.UpdateOwnerList();
        Plans();
        Assert.IsFalse(IsOrdered(Enemy, d), "the player's colony keeps out an AI at peace with it and with no treaty");

        Enemy.SignTreatyWith(Player, TreatyType.OpenBorders);
        Assert.IsTrue(Enemy.IsOpenBordersTreaty(Player), "setup: open borders");
        Plans();
        Assert.IsTrue(IsOrdered(Enemy, d), "with open borders it orders the derelict");
    }

    [TestMethod]
    public void AnAllianceLetsThePlannerInBesideAnAllysColony()
    {
        Derelict d = Forms(Wreck, Enemy);
        Local.SetOwner(Player);
        WreckSystem.UpdateOwnerList();
        Enemy.SignTreatyWith(Player, TreatyType.Alliance);
        Assert.IsTrue(Enemy.IsAlliedWith(Player) && !Enemy.IsOpenBordersTreaty(Player), "setup: an alliance with no open borders");
        Plans();
        Assert.IsTrue(IsOrdered(Enemy, d), "an ally's colony does not keep it out");
    }

    [TestMethod]
    public void AColonyOfAnEmpireItIsAtWarWithKeepsThePlannerOutByItsBordersUnlessItsOwnReachThere()
    {
        Derelict d = Forms(Wreck, Enemy);
        Local.SetOwner(Player);
        WreckSystem.UpdateOwnerList();
        Enemy.GetRelations(Player).AtWar = Player.GetRelations(Enemy).AtWar = true;
        AssertEqual(InfluenceStatus.Enemy, UState.Influence.GetInfluenceStatus(Enemy, WreckSystem.Position), "setup: the system lies in an enemy's borders");
        Plans();
        Assert.IsFalse(IsOrdered(Enemy, d), "no order inside an enemy's borders");

        SpawnShip("Subspace Projector", Enemy, SystemSite + new Vector2(10_000f, 0));
        RunObjectsSim(TestSimStep);
        AssertEqual(InfluenceStatus.Friendly, UState.Influence.GetInfluenceStatus(Enemy, WreckSystem.Position), "setup: its own projector's borders come first");
        Plans();
        Assert.IsTrue(IsOrdered(Enemy, d), "where its own borders reach, an enemy's colony is no rival's");
    }

    [TestMethod]
    public void ADerelictWithLessThanAFifthOfItsTurnsLeftIsNotOrdered()
    {
        Derelict d = Sinking(Wreck, turnsGone: 161);
        AssertEqual(39, d.TurnsLeft, "setup: 39 of its 200 turns left");
        Plans();
        Assert.IsFalse(IsOrdered(Enemy, d), "with less than a fifth of its turns left, it is not ordered");

        Derelict later = Sinking(Wreck + new Vector2(0, 8_000f), turnsGone: 160);
        AssertEqual(40, later.TurnsLeft, "setup: 40 of its 200 turns left");
        Plans();
        Assert.IsTrue(IsOrdered(Enemy, later), "one with a fifth left is");
    }

    [TestMethod]
    public void ARunsOneBuildGoesToTheClosestSystemAndIdleShipsToEveryOther()
    {
        Vector2 farSite = SystemSite * 1.5f;
        SolarSystem far = AddDummyPlanet(farSite, 1f, 1f, 1f, farSite + new Vector2(0, 60_000f), explored: false).System;
        far.SetExploredBy(Enemy);
        Derelict farOne = Forms(farSite + new Vector2(-30_000f, 0), Enemy);
        Derelict near = Forms(Wreck, Enemy);
        Enemy.WeightedCenter = Vector2.Zero;
        AssertLessThan(WreckSystem.Position.SqDist(Enemy.WeightedCenter), far.Position.SqDist(Enemy.WeightedCenter), "setup: one system is closer");

        Plans();
        Assert.IsTrue(IsOrdered(Enemy, near), "the run's build goes to the closer system");
        Assert.IsFalse(IsOrdered(Enemy, farOne), "and none to the farther");

        Derelict nearTwo = Forms(Wreck + new Vector2(0, 8_000f), Enemy);
        IdleShipOfTheAI(5_000f);
        IdleShipOfTheAI(8_000f);
        Plans();
        Assert.IsTrue(IsOrdered(Enemy, nearTwo) && IsOrdered(Enemy, farOne), "idle ships take orders in every system in one run");
    }

    [TestMethod]
    public void ASystemWithNoFreeSiteSpendsNoneOfTheRunsBuild()
    {
        Vector2 farSite = SystemSite * 1.5f;
        SolarSystem far = AddDummyPlanet(farSite, 1f, 1f, 1f, farSite + new Vector2(0, 60_000f), explored: false).System;
        far.SetExploredBy(Enemy);
        Derelict farOne = Forms(farSite + new Vector2(-30_000f, 0), Enemy);
        Derelict near = Forms(Wreck, Enemy);
        Enemy.WeightedCenter = Vector2.Zero;
        AssertLessThan(WreckSystem.Position.SqDist(Enemy.WeightedCenter), far.Position.SqDist(Enemy.WeightedCenter), "setup: one system is closer");
        WreckSystem.Radius = 10_000f; // too small for a site clear of its star
        Assert.IsFalse(SalvageMission.FindSite(WreckSystem, Empty<Ship>.Array, Enemy.Random, out _), "setup: the closer system has no free site");

        Plans();
        Assert.IsFalse(IsOrdered(Enemy, near), "the closer system, with no site for a salvage ship, is not ordered");
        Assert.IsTrue(IsOrdered(Enemy, farOne), "and the run's build goes to the farther");
    }

    [TestMethod]
    public void OnAutomaticTheShipIsQueuedAtTheShipyardThatWouldFinishItSoonestThoughAnotherIsNearer()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Enemy);
        Enemy.UpdateRallyPoints();
        Busy(near);
        SalvageMission mission = Queued(Ordered(Forms(Wreck, Enemy), empire: Enemy));
        Assert.IsNotNull(QueuedFor(EnemyHome, mission), "the AI queues its salvage ship at the shipyard that would finish it soonest");
        Assert.IsNull(QueuedFor(near, mission), "not at the busy one nearer the site");

        Player.AutoSalvage = true;
        Planet playersNear = AddHomeWorldToEmpire(SystemSite + new Vector2(0, -100_000f), Player);
        Player.UpdateRallyPoints();
        Busy(playersNear);
        SalvageMission players = Queued(Ordered(Forms(Wreck + new Vector2(0, 8_000f))));
        Assert.IsNotNull(QueuedFor(Homeworld, players), "as does the player with Auto Salvage on");
    }

    [TestMethod]
    public void OnAutomaticAQueuedShipWithNoOrderIsQueuedAgainWhenItsShipyardIsLost()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Enemy);
        Enemy.UpdateRallyPoints();
        Busy(EnemyHome);
        Derelict taken = Forms(Wreck, Enemy);
        SalvageDerelict order = Ordered(taken, empire: Enemy);
        SalvageMission mission = Queued(order);
        Assert.IsNotNull(QueuedFor(near, mission), "setup: its ship is queued at the shipyard that would finish it soonest");
        Assert.IsTrue(taken.TryClaim(Player), "setup: a rival claims the derelict first");
        order.Evaluate();
        mission.Evaluate();
        Assert.IsTrue(Enemy.AI.HasGoal(mission), "setup: on automatic the mission stays with no order");

        near.Construction.ClearQueue();
        near.SetOwner(Player);
        Enemy.UpdateRallyPoints();
        mission.Evaluate();
        mission.Evaluate();
        Assert.IsNotNull(QueuedFor(EnemyHome, mission), "its shipyard lost, the ship is queued again at the next");
    }

    [TestMethod]
    public void OnAutomaticAQueuedShipWhoseOrdersEndStaysQueuedForTheNextOrderWhichItTakesWhereverItIs()
    {
        Derelict taken = Forms(Wreck, Enemy);
        SalvageDerelict order = Ordered(taken, empire: Enemy);
        SalvageMission mission = Queued(order);
        Assert.IsNotNull(QueuedFor(EnemyHome, mission), "setup: its ship is queued");
        Assert.IsTrue(taken.TryClaim(Player), "setup: a rival claims the derelict first");
        order.Evaluate();
        Assert.IsFalse(Enemy.AI.HasGoal(order), "setup: the order ends");

        mission.Evaluate();
        Assert.IsTrue(Enemy.AI.HasGoal(mission) && QueuedFor(EnemyHome, mission) != null, "on automatic the mission stays, its ship queued");

        Vector2 elsewhere = new(-600_000f, 600_000f);
        SolarSystem other = AddDummyPlanet(elsewhere, 1f, 1f, 1f, elsewhere + new Vector2(0, 60_000f), explored: true).System;
        Ship idle = IdleShipOfTheAI(5_000f);
        SalvageDerelict sent = Ordered(Forms(elsewhere + new Vector2(30_000f, 0), Enemy), empire: Enemy);
        Assert.AreSame(idle, sent.Mission.SalvageShip, "the next order, in another system, takes an idle ship first");
        Assert.AreNotSame(mission, sent.Mission, "not the queued one");

        SalvageDerelict next = Ordered(Forms(elsewhere + new Vector2(30_000f, 8_000f), Enemy), empire: Enemy);
        Assert.AreSame(mission, next.Mission, "with none idle left, the order after takes it before a build");
        AssertEqual(other, mission.TargetSystem, "moving it there");
        Assert.IsTrue(SalvageMission.IsValidSite(other, mission.Site, Empty<Ship>.Array), "to a site among that system's derelicts");

        Assert.IsTrue(next.Derelict.TryClaim(Player), "setup: a rival claims that one too");
        next.Evaluate();
        Ship built = SpawnShip(EnemysShip, Enemy, EnemyHome.Position + new Vector2(8_000f, 0));
        mission.ReportShipComplete(built);
        mission.Evaluate();
        Assert.IsFalse(Enemy.AI.HasGoal(mission), "built with no order, it ends");
        Assert.AreSame(EnemyHome, built.AI.OrbitTarget, "its ship waiting at the nearest colony");
        Assert.IsTrue(SalvageMission.IsIdle(built, Enemy), "idle");
    }

    [TestMethod]
    public void ThePlannersNextOrderTakesAQueuedShipLeftWithNoOrderThoughItFillsTheCap()
    {
        Derelict taken = Forms(Wreck, Enemy);
        SalvageDerelict order = Ordered(taken, empire: Enemy);
        SalvageMission mission = Queued(order);
        Assert.IsTrue(taken.TryClaim(Player), "setup: a rival claims the derelict first");
        order.Evaluate();
        mission.Evaluate();
        Assert.IsTrue(Enemy.AI.HasGoal(mission), "setup: its queued ship waits for the next order");
        AssertEqual(1, SalvageOpsPlanner.Cap(Enemy), "setup: and fills the AI's cap");

        Vector2 elsewhere = new(-600_000f, 600_000f);
        AddDummyPlanet(elsewhere, 1f, 1f, 1f, elsewhere + new Vector2(0, 60_000f), explored: false).System.SetExploredBy(Enemy);
        Derelict next = Forms(elsewhere + new Vector2(30_000f, 0), Enemy);
        Plans();
        Assert.AreSame(mission, SalvageDerelict.OrderOf(Enemy, next)?.Mission, "the planner's order in another system takes it");
    }

    [TestMethod]
    public void WithAutoSalvageOnThePlayersQueuedShipWhoseOrdersEndStaysQueued()
    {
        Player.AutoSalvage = true;
        Derelict taken = Forms(Wreck);
        SalvageDerelict order = Ordered(taken);
        SalvageMission mission = Queued(order);
        Assert.IsTrue(taken.TryClaim(Enemy), "setup: a rival claims the derelict first");
        order.Evaluate();
        mission.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(mission) && QueuedFor(Homeworld, mission) != null, "the mission stays, its ship queued");
    }

    [TestMethod]
    public void WithAutoSalvageOnAStandingOrdersLitButtonCallsNothingOffWhileOrderingAndSwitchingStillClick()
    {
        Player.AutoSalvage = true;
        Derelict d = Forms(Wreck);
        Assert.IsTrue(DerelictSalvageButton.Clickable(Player, Block.None, lit: false), "a derelict with no order lights under the cursor");
        DerelictSalvageButton.Click(Player, d, Kind.Salvage, Block.None, lit: false);
        SalvageDerelict order = SalvageDerelict.OrderOf(Player, d);
        Assert.IsNotNull(order, "and is ordered by hand");

        Assert.IsFalse(DerelictSalvageButton.Clickable(Player, Block.Ordered, lit: true), "its lit button does not light");
        DerelictSalvageButton.Click(Player, d, Kind.Salvage, Block.Ordered, lit: true);
        Assert.IsTrue(Player.AI.HasGoal(order), "nor calls the order off");
        Assert.IsTrue(DerelictSalvageButton.Clickable(Player, Block.Ordered, lit: false), "the other one lights");
        DerelictSalvageButton.Click(Player, d, Kind.Recover, Block.Ordered, lit: false);
        Assert.IsTrue(order.Recovering, "and switches the order's work");

        Player.AutoSalvage = false;
        Assert.IsTrue(DerelictSalvageButton.Clickable(Player, Block.Ordered, lit: true), "control: with it off the lit button lights");
        DerelictSalvageButton.Click(Player, d, Kind.Recover, Block.Ordered, lit: true);
        Assert.IsFalse(Player.AI.HasGoal(order), "and calls the order off");
    }

    [TestMethod]
    public void AutoSalvageAndThePlannersTimerSurviveSaveAndLoad()
    {
        Player.AutoSalvage = true;
        Enemy.AI.SalvageOpsAI.TurnTimer = 7;
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvagePlanner", throwOnError: true);
        UniverseScreen game = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(game, "setup: the save loads");

        Assert.IsTrue(game.UState.Player.AutoSalvage, "Auto Salvage stays on");
        AssertEqual(7, game.UState.GetEmpireById(Enemy.Id).AI.SalvageOpsAI.TurnTimer, "and the planner keeps its count of turns");
    }

    [TestMethod]
    public void TheAutoSalvageListResetsAChoiceThatCanNoLongerBeBuiltToTheRacesAndLeavesOutAShipShortOfALoadAUnit()
    {
        TwinShips();
        var window = new AutomationWindow(Universe);
        window.LoadContent();
        AssertEqual(TwinShip, Player.data.CurrentSalvageShip, "a salvage ship the player can build stays chosen");

        Player.RemoveBuildableShip(ResourceManager.Ships.GetDesign(TwinShip));
        window.UpdateDropDowns();
        AssertEqual("Terran Salvage Ship", Player.data.CurrentSalvageShip, "one that can no longer be built gives way to the race's");

        IShipDesign from = ResourceManager.Ships.GetDesign("Terran Salvage Ship");
        ShipDesign shortHold = ((ShipDesign)from).GetClone("Short Salvage Ship");
        shortHold.SetDesignSlots(from.GetOrLoadDesignSlots().Filter(s => ResourceManager.GetModuleTemplate(s.ModuleUID) is { CargoCapacity: <= 0f } or { Salvage: true }),
                                 updateRole: false);
        Assert.IsTrue(Player.AddBuildableShip(shortHold) && shortHold.BaseCargoSpace < ShipResupply.SalvageLoad, "setup: the player can build a salvage ship short of a load");
        Player.data.CurrentSalvageShip = shortHold.Name;
        window.UpdateDropDowns();
        AssertEqual("Terran Salvage Ship", Player.data.CurrentSalvageShip, "as does one short of a load a unit");

        UState.P.DisableSalvageOps = true;
        Player.data.CurrentSalvageShip = "No Such Salvage Ship";
        window.LoadContent();
        AssertEqual("No Such Salvage Ship", Player.data.CurrentSalvageShip, "with salvage ops off the window leaves the choice alone");
    }

    [TestMethod]
    public void TheAIsSalvageShipJoinsNoFleetAndIsNoSupportShipItCanBuildOrItsPickForOne()
    {
        Ship ship = IdleShipOfTheAI(5_000f);
        Assert.IsFalse(Enemy.AllFleetReadyShips().Contains(ship), "its fleets never draft a salvage ship");
        Assert.IsFalse(Enemy.CanBuildSupportShips, "its salvage ship is no support ship it can build");
        Assert.IsNull(ShipBuilder.PickFromCandidates(RoleName.support, Enemy), "nor its pick for a support ship");
        Assert.IsNull(ShipBuilder.PickCostEffectiveShipToBuild(RoleName.support, Enemy, float.MaxValue, float.MaxValue), "or for a defense ship");
    }

    [TestMethod]
    public void TheAIScrapsNoIdleSalvageShipAsASupportShipBeyondItsBudget()
    {
        Ship[] ships = { IdleShipOfTheAI(5_000f), IdleShipOfTheAI(8_000f) };
        AssertGreaterThan(ships[0].GetMaintCost(Enemy), 0f, "setup: an idle salvage ship pays upkeep");
        _ = new EmpireAI.RoleBuildInfo(capacity: 0f, Enemy.AI, ignoreDebt: false); // as its military planner weighs its ships with no budget for any
        Assert.IsFalse(ships.Any(s => s.DoingScrap), "its salvage ships are no support ships to scrap");
    }

    [TestMethod]
    public void TheAIRefitsNoSalvageShipThoughAStrongerSupportShipOnItsHullCanBeBuilt()
    {
        Ship ship = IdleShipOfTheAI(5_000f);
        IShipDesign from = ResourceManager.Ships.GetDesign(EnemysShip);
        ShipDesign armed = ((ShipDesign)from).GetClone("Armed Support Ship");
        armed.SetDesignSlots(from.GetOrLoadDesignSlots().Where(s => !ResourceManager.GetModuleTemplate(s.ModuleUID).Salvage)
                                 .Select(s => s.ModuleUID == "BasicCargoHold" ? new DesignSlot(s.Pos, "HVVulcanCannon", s.Size, 0, ModuleOrientation.Normal, null) : s)
                                 .ToArray(), updateRole: false);
        Assert.IsTrue(!armed.IsSalvageShip && Enemy.AddBuildableShip(armed), "setup: the AI can build an armed support ship on the salvage ship's hull");
        AssertEqual(armed, ShipBuilder.PickShipToRefit(ship, Enemy), "setup: which a refit would pick, as the stronger");
        Enemy.AI.TriggerRefit();
        Assert.IsFalse(Enemy.AI.HasGoal(g => g is RefitShip && g.OldShip == ship), "the AI refits no salvage ship");
    }
}
