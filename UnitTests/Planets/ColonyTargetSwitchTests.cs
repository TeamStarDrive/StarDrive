using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

/// <summary>
/// Auto colonize at its colony-goal cap: a planet our ships explore is compared a turn later with the colony targets,
/// and takes the place of the weakest one it beats by more than a quarter, when no enemy is known there.
/// </summary>
[TestClass]
public class ColonyTargetSwitchTests : StarDriveTest
{
    const float Distance = 300_000f;
    readonly Planet Home;
    readonly Vector2 Center;
    int NumPlanets;

    public ColonyTargetSwitchTests()
    {
        CreateUniverseAndPlayerEmpire();
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Home = AddHomeWorldToEmpire(new Vector2(400_000), Player, new Vector2(405_000), explored: true);
        Player.CalcWeightedCenter(calcNow: true);
        Center = Player.WeightedCenter;
        AssertLessThan(Center.Distance(Home.Position), 1f, "setup: the empire is centered on its homeworld");
        Player.AutoColonize = true;
    }

    ExpansionPlanner Planner => Player.AI.ExpansionAI;

    float Value(Planet p) => p.ColonyPotentialValue(Player);

    // planets lie the same distance from the empire and have no tiles, so their minerals alone set their values
    Planet AddPlanet(string name, float minerals, bool explored = true)
    {
        Vector2 dir = (NumPlanets++ * 40f).AngleToDirection();
        Vector2 pos = Center + dir * Distance;
        Planet p = AddDummyPlanet(pos - dir * 5000f, 1f, minerals, 4f, pos, explored);
        p.Name = name;
        return p;
    }

    Planet AddWeak() => AddPlanet("Weak", 5f);
    Planet AddBetter(bool explored = true) => AddPlanet("Better", 16f, explored);

    MarkForColonization Target(Planet p)
    {
        var goal = new MarkForColonization(p, Player);
        Player.AI.AddGoal(goal);
        return goal;
    }

    MarkForColonization[] TargetMids(int count)
    {
        var goals = new MarkForColonization[count];
        for (int i = 0; i < count; ++i)
            goals[i] = Target(AddPlanet($"Mid {i}", 8f));
        return goals;
    }

    // the goal picks up the idle colony ship and sends it toward its target
    MarkForColonization TargetWithShip(Planet p, Vector2 shipAt, out Ship colonyShip)
    {
        colonyShip = SpawnShip("Colony Ship", Player, shipAt);
        UState.Objects.Update(TestSimStep);
        UState.Objects.Update(TestSimStep);
        var goal = new MarkForColonization(p, Player);
        Player.AI.AddGoalAndEvaluate(goal);
        AssertEqual(colonyShip, goal.FinishedShip, "setup: the goal takes the idle colony ship");
        AssertEqual(p, ColonizeTargetOf(colonyShip), "setup: the colony ship heads for the target");
        return goal;
    }

    void AssertThePlannerWouldTake(Planet p)
        => Assert.IsTrue(new PlanetRanker(Player, p, Distance, Center).CanColonize, "setup: the planner's ranker accepts the found planet");

    static Planet ColonizeTargetOf(Ship ship)
        => ship.AI.FindGoal(ShipAI.Plan.Colonize, out ShipAI.ShipGoal goal) ? goal.TargetPlanet : null;

    // a planet found during one turn is compared at the next
    void CompareATurnLater()
    {
        Planner.SwitchColonyGoalsToFoundPlanets();
        Planner.SwitchColonyGoalsToFoundPlanets();
    }

    void FoundAndCompared(Planet p)
    {
        Planner.AddFoundPlanet(p);
        CompareATurnLater();
    }

    [TestMethod]
    public void APlanetAScoutFindsAtTheCapTakesTheWeakestTargetAndTurnsItsColonyShip()
    {
        TargetMids(4);
        Planet weak = AddWeak();
        MarkForColonization weakGoal = TargetWithShip(weak, Home.Position + new Vector2(3000, 0), out Ship colonyShip);
        Planet better = AddBetter(explored: false);
        AssertGreaterThan(Value(better), Value(weak) * 1.25f, "setup: the found planet is worth more than a quarter more");

        SpawnShipNoCombatHoldPos("Vulcan Scout", Player, better.Position + new Vector2(1000, 0));
        RunSimWhile((simTimeout: 5, fatal: true), () => !better.IsExploredBy(Player));

        int notifications = Universe.NotificationManager.NumberOfNotifications;
        Planner.SwitchColonyGoalsToFoundPlanets();
        AssertEqual(weak, weakGoal.TargetPlanet, "a found planet waits a turn, so the threat memory sees what the scout saw");

        Planner.SwitchColonyGoalsToFoundPlanets();
        AssertEqual(better, weakGoal.TargetPlanet, "the found planet takes the place of the weakest target");
        AssertEqual(better, ColonizeTargetOf(colonyShip), "the target's colony ship turns toward the new planet");
        AssertEqual(notifications + 1, Universe.NotificationManager.NumberOfNotifications, "the player is told of the switch");
    }

    [TestMethod]
    public void APlanetLessThanAQuarterBetterTakesNoTarget()
    {
        Planet weak = AddWeak();
        MarkForColonization weakGoal = Target(weak);
        TargetMids(4);
        Planet slightlyBetter = AddPlanet("Slightly Better", 5.5f);
        AssertGreaterThan(Value(slightlyBetter), Value(weak), "setup: the found planet is worth more than the weakest target");
        AssertLessThan(Value(slightlyBetter), Value(weak) * 1.25f, "setup: but not a quarter more");
        AssertThePlannerWouldTake(slightlyBetter);

        FoundAndCompared(slightlyBetter);

        AssertEqual(weak, weakGoal.TargetPlanet, "a planet less than a quarter better takes no target");
    }

    [TestMethod]
    public void ATargetWhoseColonyShipIsInItsSystemKeepsIt()
    {
        Planet weak = AddWeak();
        MarkForColonization weakGoal = TargetWithShip(weak, weak.Position + new Vector2(6000, 0), out Ship colonyShip);
        AssertEqual(weak.System, colonyShip.System, "setup: the colony ship is already in its target's system");
        MarkForColonization[] mids = TargetMids(4);
        Planet better = AddBetter();
        AssertGreaterThan(Value(better), Value(mids[0].TargetPlanet) * 1.25f, "setup: the found planet beats the other targets too");

        FoundAndCompared(better);

        AssertEqual(weak, weakGoal.TargetPlanet, "a target whose colony ship is in its system keeps it");
        AssertEqual(weak, ColonizeTargetOf(colonyShip), "and its colony ship flies on");
        Assert.IsTrue(mids.Any(g => g.TargetPlanet == better), "the found planet takes the next weakest target instead");
    }

    [TestMethod]
    public void ATargetOrderedByHandIsNeverSwitched()
    {
        Planet weak = AddWeak();
        var manual = new MarkForColonization(weak, Player, isManual: true);
        Player.AI.AddGoal(manual);
        MarkForColonization[] mids = TargetMids(4);
        Planet better = AddBetter();
        AssertGreaterThan(Value(better), Value(mids[0].TargetPlanet) * 1.25f, "setup: the found planet beats the other targets too");

        FoundAndCompared(better);

        AssertEqual(weak, manual.TargetPlanet, "a target you ordered yourself is never switched");
        Assert.IsTrue(mids.Any(g => g.TargetPlanet == better), "the found planet takes the weakest auto colonize target instead");
    }

    [TestMethod]
    public void AQueuedColonyShipIsRenamedForItsNewTarget()
    {
        Player.UpdateRallyPoints();
        Planet weak = AddWeak();
        var weakGoal = new MarkForColonization(weak, Player);
        Player.AI.AddGoalAndEvaluate(weakGoal);
        weakGoal.Evaluate();
        weakGoal.Evaluate();
        QueueItem queued = Home.ConstructionQueue.FirstOrDefault(q => q.Goal == weakGoal);
        Assert.IsNotNull(queued, "setup: the goal queues its colony ship at the homeworld");
        AssertEqual($"{queued.ShipData.Name} ({weak.Name})", queued.DisplayName, "setup: the queued ship is named for its target");
        queued.QType = QueueItemType.ColonyShipClaim;
        TargetMids(4);
        Planet better = AddBetter();

        FoundAndCompared(better);
        for (int i = 0; i < 3; ++i)
            weakGoal.Evaluate();

        AssertEqual(better, weakGoal.TargetPlanet, "setup: the found planet takes the weakest target");
        AssertEqual($"{queued.ShipData.Name} ({better.Name})", queued.DisplayName);
        AssertEqual(QueueItemType.ColonyShip, queued.QType, "a safe target has no claim to wait for, so its colony ship is queued as any other");
        AssertEqual(1, Home.ConstructionQueue.Count(q => q.Goal == weakGoal), "the colony ship already queued is kept, not queued twice");
    }

    [TestMethod]
    public void APlanetWithAnEnemyInSightTakesNoTarget()
    {
        Planet weak = AddWeak();
        MarkForColonization weakGoal = Target(weak);
        TargetMids(4);
        Planet better = AddBetter(explored: false);
        Ship guard = SpawnShipNoCombatHoldPos("Vulcan Scout", Enemy, better.Position + new Vector2(2000, 0));
        guard.AI.SetCombatTriggerDelay(1000f);
        Ship scout = SpawnShipNoCombatHoldPos("Vulcan Scout", Player, better.Position + new Vector2(1000, 0));
        scout.AI.SetCombatTriggerDelay(1000f);
        SpawnShipNoCombatHoldPos("Terran-Prototype", Player, Home.Position + new Vector2(0, -50_000)).AI.SetCombatTriggerDelay(1000f);

        RunSimWhile((simTimeout: 5, fatal: true), () => !better.IsExploredBy(Player));
        RunObjectsSim(2f);
        Player.UpdateMilitaryStrengths();
        float enemyStrength = Player.KnownEnemyStrengthIn(better.System);
        AssertGreaterThan(enemyStrength, 10f, "setup: the scout saw the enemy beside the planet");
        AssertLessThan(enemyStrength * Player.GetFleetStrEmpireMultiplier(Enemy), Player.OffensiveStrength,
                       "setup: the planner would still claim the system");
        AssertThePlannerWouldTake(better);
        AssertGreaterThan(new PlanetRanker(Player, better, Distance, Center).Value, new PlanetRanker(Player, weak, Distance, Center).Value * 1.25f,
                          "setup: ranked with the enemy known, the planet still beats the weakest target by more than a quarter");

        CompareATurnLater();

        AssertEqual(weak, weakGoal.TargetPlanet, "a planet with an enemy known in its system takes no target");
        Assert.IsFalse(Planner.GetColonizationGoalPlanets().Contains(better), "no target gives way to it");
    }

    [TestMethod]
    public void APlanetFoundBelowTheCapTakesNoTarget()
    {
        Planet weak = AddWeak();
        MarkForColonization weakGoal = Target(weak);
        TargetMids(3);
        Planet better = AddBetter();
        AssertThePlannerWouldTake(better);

        FoundAndCompared(better);

        AssertEqual(weak, weakGoal.TargetPlanet, "below the cap no target gives way to a found planet");
    }

    [TestMethod]
    public void ATargetWhoseColonyShipIsLandingKeepsIt()
    {
        Planet weak = AddWeak();
        MarkForColonization weakGoal = TargetWithShip(weak, Home.Position + new Vector2(3000, 0), out Ship colonyShip);
        colonyShip.InitLanding(LandPlan.Colonize, weak);
        Assert.IsTrue(colonyShip.IsLanding && colonyShip.System != weak.System,
                      "setup: the colony ship lands outside its target's system, so only its landing can keep the target");
        MarkForColonization[] mids = TargetMids(4);
        Planet better = AddBetter();

        FoundAndCompared(better);

        AssertEqual(weak, weakGoal.TargetPlanet, "a target whose colony ship is landing keeps it");
        Assert.IsTrue(mids.Any(g => g.TargetPlanet == better), "the found planet takes the next weakest target instead");
    }

    static void ApplyPendingTasks(Empire empire)
        => typeof(EmpireAI).GetMethod("ApplyPendingChanges", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(empire.AI, null);

    static bool ChecksNewOwners(MarkForColonization goal)
        => (bool)typeof(MarkForColonization).GetField("CheckNewOwnersInSystem", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(goal);

    static void SetFleet(MilitaryTask task, Fleet fleet)
        => typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(task, fleet);

    static int GuardsFor(Empire empire, Planet p)
        => empire.AI.GetTasks().Count(t => t.Type == MilitaryTask.TaskType.GuardBeforeColonize && t.TargetPlanet == p);

    [TestMethod]
    public void AnAISwitchOnHardCallsOffTheOldTargetsFleetsAndAsksOneGuardForTheNewTarget()
    {
        UState.P.Difficulty = GameDifficulty.Hard;
        Planet old = AddPlanet("Old", 5f);
        old.System.OwnerList.Add(Player);
        var goal = new MarkForColonization(old, Enemy);
        Enemy.AI.AddGoal(goal);
        goal.Task = MilitaryTask.CreateClaimTask(Enemy, old, 20f, Player, 1);
        Enemy.AI.AddPendingTask(goal.Task);
        Enemy.AI.AddPendingTask(MilitaryTask.CreateGuardTask(Enemy, old));
        ApplyPendingTasks(Enemy);
        Enemy.AI.AddPendingTask(MilitaryTask.CreateGuardTask(Enemy, old));
        Assert.IsFalse(ChecksNewOwners(goal), "setup: another empire holds the old target's system");

        SetFleet(goal.Task, new Fleet(UState, Enemy));
        Assert.IsFalse(goal.CanSwitchTarget, "a goal whose claim fleet has formed keeps its target");
        SetFleet(goal.Task, null);
        Assert.IsTrue(goal.CanSwitchTarget, "setup: before its claim fleet forms the goal may switch");

        Planet target = AddPlanet("New", 16f);
        goal.SwitchTargetPlanet(target);
        ApplyPendingTasks(Enemy);

        AssertEqual(target, goal.TargetPlanet);
        Assert.IsNull(goal.Task, "the claim for the old target is called off");
        Assert.IsFalse(Enemy.AI.GetTasks().Any(t => t.TargetPlanet == old), "so are the fleets asked to guard it, the one not added yet too");
        AssertEqual(1, GuardsFor(Enemy, target), "one fleet is asked to guard the new target");
        Assert.IsTrue(ChecksNewOwners(goal), "the AI watches the new target's empty system for settlers");

        Planet guarded = AddPlanet("Guarded", 16f);
        Enemy.AI.AddPendingTask(MilitaryTask.CreateGuardTask(Enemy, guarded));
        var other = new MarkForColonization(AddPlanet("Other", 5f), Enemy);
        Enemy.AI.AddGoal(other);
        other.SwitchTargetPlanet(guarded);
        ApplyPendingTasks(Enemy);

        AssertEqual(1, GuardsFor(Enemy, guarded), "a planet a fleet is already asked to guard gets no second one");
    }
}
