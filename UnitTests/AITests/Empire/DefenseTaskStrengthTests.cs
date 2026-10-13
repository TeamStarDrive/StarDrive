using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Empires.Components;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire;

[TestClass]
public class DefenseTaskStrengthTests : StarDriveTest
{
    const string Warship = "Vulcan Scout";
    static readonly MethodInfo RequisitionVsRemnants =
        typeof(MilitaryTask).GetMethod("RequisitionDefendVsRemnants", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public DefenseTaskStrengthTests()
    {
        LoadStarterShips(Warship);
        CreateUniverseAndPlayerEmpire();
    }

    Planet AddRemnantsAndAColony()
    {
        CreateAMinorFaction("Remnant");
        UState.Remnants = Faction;
        Enemy.InitFleetEmpireStrMultiplier();
        return AddDummyPlanetToEmpire(new Vector2(200_000f), Enemy);
    }

    void RaidOn(Planet colony)
    {
        var raid = new RemnantEngageEmpire(Faction) { TargetPlanet = colony };
        raid.Fleet = new Fleet(UState, Faction) { TaskStep = 2 };
        Faction.AI.AddGoal(raid);
        Assert.AreEqual(1, Faction.AI.GetRemnantEngagementGoalsFor(colony).Length, "setup: a raid is on its way to the colony");
    }

    void Requisition(MilitaryTask task, int times = 1)
    {
        Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips()); // as the empire's update does
        for (int i = 0; i < times; ++i)
            RequisitionVsRemnants.Invoke(task, null);
    }

    [TestMethod]
    public void ADefenseAgainstARaidKeepsAskingForTheRaidsStrengthWhileTheRaidIsOnItsWay()
    {
        Planet colony = AddRemnantsAndAColony();
        RaidOn(colony);
        MilitaryTask task = MilitaryTask.CreateDefendVsRemnant(colony, Enemy, 50_000f);
        float wanted = task.MinimumTaskForceStrength;
        Assert.IsTrue(wanted >= 50_000f, "setup: the task asks for at least the raid's strength");

        Requisition(task, times: 2);

        Assert.IsNull(task.Fleet, "setup: the empire has no ships to send");
        AssertEqual(1f, wanted, task.MinimumTaskForceStrength, "a failed attempt does not shrink the next one to the nothing seen at the planet");
    }

    [TestMethod]
    public void ABigEmpiresDefenseAgainstARaidDoesNotShrinkWithEachAttempt()
    {
        Planet colony = AddRemnantsAndAColony();
        RaidOn(colony);
        typeof(Ship_Game.Empire).GetProperty(nameof(Ship_Game.Empire.TotalPopBillion))!.SetValue(Enemy, 90f);
        MilitaryTask task = MilitaryTask.CreateDefendVsRemnant(colony, Enemy, 50_000f);
        float wanted = task.MinimumTaskForceStrength;

        Requisition(task, times: 3);

        AssertEqual(1f, wanted, task.MinimumTaskForceStrength, "an empire of 90 billion asks for a third each time, not a third of a third");
    }

    [TestMethod]
    public void ADefenseAgainstAHugeRaidAsksForNoMoreThanHalfTheNavy()
    {
        Planet colony = AddRemnantsAndAColony();
        RaidOn(colony);
        var ships = new Array<Ship>();
        for (int i = 0; i < 4; ++i)
            ships.Add(SpawnShip(Warship, Enemy, colony.Position + new Vector2(5_000f + i * 300f, 0f)));
        UState.Objects.Update(new(time: 1f));
        Enemy.UpdateMilitaryStrengths();
        float navy = Enemy.OffensiveStrength;
        Assert.IsTrue(navy > 0f, "setup: the empire has a navy");
        MilitaryTask task = MilitaryTask.CreateDefendVsRemnant(colony, Enemy, navy * 10f);
        Enemy.AI.AddPendingTask(task);

        Requisition(task);

        Assert.IsNotNull(task.Fleet, "the empire sends what half its navy can do against a raid ten times its size");
    }

    [TestMethod]
    public void ADefenseAgainstARaidThatHasMovedOnEnds()
    {
        Planet colony = AddRemnantsAndAColony();
        MilitaryTask task = MilitaryTask.CreateDefendVsRemnant(colony, Enemy, 50_000f);

        Requisition(task);

        Assert.IsTrue(task.QueuedForRemoval, "with no raid heading for the colony, the defense is called off");
    }

    // the empire's military planner finds the task's fleet empty, the same turn before any goal looks at it
    float MultiplierAfterTheFleetIsLostAt(int taskStep)
    {
        Planet colony = AddRemnantsAndAColony();
        MilitaryTask task = MilitaryTask.CreateDefendVsRemnant(colony, Enemy, 50_000f);
        var fleet = new Fleet(UState, Enemy) { FleetTask = task, TaskStep = taskStep };
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(task, fleet);
        float before = Enemy.GetFleetStrEmpireMultiplier(Faction);

        Assert.IsFalse(task.Evaluate(Enemy), "setup: the task ends with its fleet gone");

        return Enemy.GetFleetStrEmpireMultiplier(Faction) - before;
    }

    [TestMethod]
    public void ADefenseFleetDestroyedWhileDefendingRaisesTheStrengthAskedForNextTime()
    {
        Assert.IsTrue(MultiplierAfterTheFleetIsLostAt(taskStep: 2) > 0f, "a defense lost at its post counts, as a won one does");
    }

    [TestMethod]
    public void ADefenseFleetGoneBeforeItTookItsPostIsNotCountedAsALoss()
    {
        AssertEqual(0.0001f, 0f, MultiplierAfterTheFleetIsLostAt(taskStep: 1), "a fleet that never reached the fight did not lose it");
    }

    [TestMethod]
    public void ADefenseTaskKeepsTheStrengthItWasRaisedForBeforeTheAttackersArrive()
    {
        SolarSystem system = AddDummyPlanetToEmpire(new Vector2(200_000f), Enemy).System;
        var task = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, Enemy, system.Position, system.Radius,
                                    system, 20_000f, MilitaryTaskImportance.Normal);
        AssertEqual(0f, Enemy.KnownEnemyStrengthIn(system), "setup: no attacker has arrived yet");

        typeof(MilitaryTask).GetMethod("UpdateMinimumTaskForceStrength", BindingFlags.Instance | BindingFlags.NonPublic)!
                            .Invoke(task, new object[] { 0f });

        Assert.IsTrue(task.MinimumTaskForceStrength >= 20_000f,
                      $"the fleet is still sized for the expected attack ({task.MinimumTaskForceStrength} for 20000)");
    }

    [TestMethod]
    public void AnIncomingAttackIsDefendedAgainstWithTheAttackersOwnStrength()
    {
        SolarSystem system = AddDummyPlanetToEmpire(new Vector2(200_000f), Enemy).System;
        Fleet attackers = Player.CreateFleet(1, null);
        for (int i = 0; i < 12; ++i)
            attackers.AddShip(SpawnShip(Warship, Player, system.Position + new Vector2(80_000f + i * 300f, 0f)));
        var threat = new IncomingThreat(Enemy, system, new[] { attackers });
        Assert.IsTrue(threat.HighPriority, "setup: a player fleet is a priority threat");
        Assert.IsTrue(threat.Strength > 500f, "setup: a threat big enough to be taken at its strength");
        Enemy.ThreatDetector.SystemsWithThreat = new[] { threat };

        new EmpireDefense(Enemy).Evaluate();

        var defense = (DefendSystem)Enemy.AI.FindGoal(g => g is DefendSystem);
        Assert.IsNotNull(defense, "setup: the empire raises a defense");
        AssertEqual(1f, attackers.GetStrength(), defense.Task.EnemyStrength,
                    "the task holds the attackers' strength; it applies the empire's caution once, when it asks for ships");
        Assert.AreSame(Player, defense.Task.TargetEmpire, "that caution is the one toward the attacker, known before its fleet arrives");
    }
}
