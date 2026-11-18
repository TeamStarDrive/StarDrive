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
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

/// <summary>
/// An AI fleet claiming a planet for colonization that forms inside the planet's system goes straight to the planet
/// instead of gathering at the system's edge, and the colonization goal gives up on its claim fleet only when the fleet
/// is neither moving in nor in the system. No claim fleet is formed against a force in the system that it cannot take on
/// whole; a fleet already on its claim weighs only a share of the force it finds there, and gives up when even that
/// share is more than it can take on. The lost fight counts against the empire whose ships it found. When it goes in,
/// its carriers and bombers go in with the warships and are sent at the enemy, a bombarding bomber keeps bombarding,
/// and a stranded ship does not hold up the rest; with no hostile left in the system and no troops to land, it orbits
/// the planet at once. Its troop ships hold back until the fight is won and a ship that went in is at the planet;
/// troop ships left alone give up the claim unless they are landing. On a planet with no owner, a building left there
/// and troops of an empire it is at peace with are no enemy on the ground, so the fleet lands troops only when troops
/// of an empire it is at war with are on it. A colonization goal that starts over while an enemy is still there calls
/// off its old claim for the new one, and keeps it when none is left. With a claim under way, the goal gives up when the
/// known force is more than the whole navy, weighing only the share a fleet on its task weighs once the claim's fleet
/// has formed. A guard fleet that has formed weighs that share too.
/// </summary>
[TestClass]
public class ClaimFleetTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string Constructor = "Terran Constructor";
    const string ColonyShip = "Colony Ship";
    const string TroopShip = "Proton Troopship  Mk1";
    const string Carrier = "Heavy Carrier mk5-b";
    const string Bomber = "Neutron Bomber Mk1";
    readonly Planet Target;

    public ClaimFleetTests()
    {
        LoadStarterShips(Warship, Constructor, ColonyShip, TroopShip, Carrier, Bomber);
        CreateUniverseAndPlayerEmpire();
        Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Constructor)); // a claim fleet sends a constructor for its projector
        UState.Objects.EnableParallelUpdate = false;
        Target = AddDummyPlanet(new Vector2(400_000));
    }

    Vector2 InSystem => Target.Position + new Vector2(20_000f, 0f);
    Vector2 OutsideSystem => Target.System.Position + new Vector2(Target.System.Radius * 3f, 0f);

    // the AI's claim fleet for the planet, at the given step of its task
    Fleet ClaimFleet(Vector2 at, int taskStep, out MilitaryTask task, int troopShips = 0, bool carrier = false, int warships = 4, int bombers = 0)
    {
        task = MilitaryTask.CreateClaimTask(Enemy, Target, 100f, Player, fleetCount: 1);
        var fleet = new Fleet(UState, Enemy) { FleetTask = task, TaskStep = taskStep };
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(task, fleet);
        for (int i = 0; i < warships; ++i)
            fleet.AddShip(SpawnShip(Warship, Enemy, at + new Vector2(0f, 1000f * i)));
        for (int i = 0; i < troopShips; ++i)
            fleet.AddShip(SpawnShip(TroopShip, Enemy, at + new Vector2(2000f, 1000f * i)));
        if (carrier)
            fleet.AddShip(SpawnShip(Carrier, Enemy, at + new Vector2(-2000f, 0f)));
        for (int i = 0; i < bombers; ++i)
            fleet.AddShip(SpawnShip(Bomber, Enemy, at + new Vector2(-4000f, 1000f * i)));
        Assert.AreEqual(at.InRadius(Target.System.Position, Target.System.Radius), fleet.IsInSystem(Target.System),
                        "setup: the fleet is where it was put");
        return fleet;
    }

    static void ClaimDefense(Fleet fleet, MilitaryTask task)
        => typeof(Fleet).GetMethod("DoClaimDefense", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, new object[] { task });

    static Ship[] TroopShips(Fleet fleet) => fleet.Ships.Filter(s => s.IsTroopShip);
    static Ship[] GoingIn(Fleet fleet) => fleet.Ships.Filter(s => !s.IsTroopShip);

    static bool AllAre(Ship[] ships, AIState state)
    {
        foreach (Ship ship in ships)
            if (ship.AI.State != state)
                return false;
        return ships.Length > 0;
    }

    static float Surface(Ship[] ships)
    {
        float surface = 0f;
        foreach (Ship ship in ships)
            surface += ship.SurfaceArea;
        return surface;
    }

    // player warships at the planet that the AI sees
    void SeenAtThePlanet(int ships)
    {
        for (int i = 0; i < ships; ++i)
        {
            Ship guard = SpawnShip(Warship, Player, Target.Position + new Vector2(10_000f, 1000f * i));
            guard.SetSystem(Target.System);
            Enemy.Threats.SetSeen(guard, fromBackgroundThread: false);
        }
        Enemy.Threats.Update(new FixedSimTime(2f));
    }

    // a player force in the system that the AI now sees, more than its claim fleet can stay in a fight against
    void StrongerForceSeenInTheSystem(Fleet fleet)
    {
        SeenAtThePlanet(ships: 12);
        AssertGreaterThan(Enemy.Threats.GetWeightedHostileStrengthAt(Target.System.Position, Target.System.Radius) * Fleet.StayInFightEnemyShare,
                          fleet.GetStrength() * Enemy.PersonalityModifiers.CanWeTakeThisFightMultiplier,
                          "setup: the force the AI now sees in the system is more than its fleet can stay against");
    }

    [TestMethod]
    public void AClaimFleetThatFindsAStrongerForceInTheSystemGivesUpItsClaim()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 5, out MilitaryTask task);
        StrongerForceSeenInTheSystem(fleet);
        Assert.AreEqual(0f, task.EnemyStrength, "setup: the claim was made with no enemy known there");

        ClaimDefense(fleet, task);

        Assert.IsNull(fleet.FleetTask, "a claim fleet that finds a stronger force in the system gives up its claim");
    }

    [TestMethod]
    public void AClaimFleetStaysAgainstAForceTooStrongToGoInAgainstThatItCanStillHold()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 5, out MilitaryTask task);
        Enemy.FleetStrEmpireMultiplier[Player.Id] = 1f;
        float canTake = fleet.GetStrength() * Enemy.PersonalityModifiers.CanWeTakeThisFightMultiplier;
        float playerShip = SpawnShip(Warship, Player, new Vector2(900_000f)).GetStrength();
        SeenAtThePlanet(ships: (int)(canTake / playerShip) + 1);
        float hostiles = Enemy.Threats.GetWeightedHostileStrengthAt(Target.System.Position, Target.System.Radius);
        Assert.IsFalse(Fleet.CanTakeWeightedFight(Enemy, fleet.GetStrength(), hostiles), "setup: weighed whole, the force is too much to go in against");
        Assert.IsTrue(Fleet.CanTakeWeightedFight(Enemy, fleet.GetStrength(), hostiles * Fleet.StayInFightEnemyShare),
                      "setup: at the share a fleet on its task weighs, the fleet can hold against it");

        ClaimDefense(fleet, task);

        Assert.AreEqual(task, fleet.FleetTask, "a fleet already on its claim stays against a force it could not have gone in against");
    }

    // the claim asks for the raw strength known there; weighted, the force is more than the pool can go in against but less than it can stay against
    [TestMethod]
    public void AClaimFormsNoFleetAgainstAForceItCannotTakeOnWhole()
    {
        var ready = new Ship[4];
        for (int i = 0; i < ready.Length; ++i)
            ready[i] = SpawnShip(Warship, Enemy, OutsideSystem + new Vector2(0f, 1000f * i));
        UState.Objects.Update(new FixedSimTime(2f));
        SeenAtThePlanet(ships: 4);
        Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips()); // as the empire's update does
        float pool = Enemy.ShipsReadyForFleet.AccumulatedStrength;
        float canTake = pool * Enemy.PersonalityModifiers.CanWeTakeThisFightMultiplier;
        float between = (1f + Fleet.StayInFightEnemyShare) / 2f;
        float known = Enemy.KnownEnemyStrengthIn(Target.System);
        float weakest = float.MaxValue;
        foreach (Ship ship in ready)
            weakest = ship.GetStrength() < weakest ? ship.GetStrength() : weakest;
        AssertLessThan(pool - weakest, known, "setup: the claim needs every ready ship to match the known strength, so all of them go in");
        Enemy.FleetStrEmpireMultiplier[Player.Id] = canTake / (between * known);
        float hostiles = Enemy.Threats.GetWeightedHostileStrengthAt(Target.System.Position, Target.System.Radius);
        Assert.IsFalse(Fleet.CanTakeWeightedFight(Enemy, pool, hostiles),
                       "setup: whole, the force the AI knows in the system is more than all its ready ships can take on");
        Assert.IsTrue(Fleet.CanTakeWeightedFight(Enemy, pool, hostiles * Fleet.StayInFightEnemyShare),
                      "setup: at the share a fleet on its task weighs, they could stay against it");
        MilitaryTask task = MilitaryTask.CreateClaimTask(Enemy, Target, 100f, targetEmpire: null, fleetCount: 1);

        task.Evaluate(Enemy);

        Assert.IsTrue(task.QueuedForRemoval, "the claim ends instead of forming a fleet against a force it cannot take on whole");
        Assert.IsNull(task.Fleet, "no fleet is formed");
    }

    // the fleet went into its task's fight whole and lost half its warships there
    static void LostHalfItsShipsInTheFight(Fleet fleet, MilitaryTask task)
    {
        Assert.IsTrue(fleet.CanTakeThisFight(0f, task, inCombat: true), "setup: whole, the fleet can take on what it went in against");
        AssertEqual(0.01f, fleet.GetStrength(), task.PeakFleetStrength, "setup: its task keeps the strength it went into the fight with");
        Ship[] goingIn = GoingIn(fleet);
        for (int i = 0; i < goingIn.Length / 2; ++i)
            fleet.RemoveShip(goingIn[i], clearOrders: true);
        AssertLessThan(fleet.GetStrength(), task.PeakFleetStrength * 0.75f, "setup: the fleet lost more than a quarter of its strength in the fight");
    }

    [TestMethod]
    public void AClaimFleetThatOnlyFindsAStrongerForceInTheSystemHasNotLostAFight()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 5, out MilitaryTask task);
        StrongerForceSeenInTheSystem(fleet);
        Enemy.InitFleetEmpireStrMultiplier(); // as at the start of a game
        float before = Enemy.GetFleetStrEmpireMultiplier(Player);

        ClaimDefense(fleet, task);

        Assert.IsTrue(task.QueuedForRemoval || fleet.FleetTask == null, "setup: the fleet gives up its claim");
        Assert.AreEqual(before, Enemy.GetFleetStrEmpireMultiplier(Player), "a fleet that only found a stronger force, with its ships whole, asks for no more strength next time");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AFleetThatGivesUpAFightAsksForMoreStrengthNextTimeOnlyWhenItLostShipsInIt(bool lostShips)
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 5, out MilitaryTask task);
        task.TargetEmpire = Player;
        Enemy.InitFleetEmpireStrMultiplier(); // as at the start of a game
        if (lostShips)
            LostHalfItsShipsInTheFight(fleet, task);
        float before = Enemy.GetFleetStrEmpireMultiplier(Player);

        Assert.IsFalse(fleet.CanTakeThisFight(1_000_000f, task, inCombat: true), "setup: the fleet cannot take on the enemy");

        if (lostShips)
            AssertGreaterThan(Enemy.GetFleetStrEmpireMultiplier(Player), before, "a fleet that lost ships in the fight it gives up asks for more strength next time");
        else
            Assert.AreEqual(before, Enemy.GetFleetStrEmpireMultiplier(Player), "one that only found the odds against it does not");
    }

    [TestMethod]
    public void AClaimFleetThatClearsTheAreaCountsItsNextFightOnItsOwn()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 6, out MilitaryTask task);
        task.PeakFleetPlanet = task.TargetPlanet;
        task.PeakFleetStrength = fleet.GetStrength() * 2f; // as after the losses of the fight it won there

        ClaimDefense(fleet, task);

        AssertGreaterThan(fleet.TaskStep, 6, "setup: with no hostile left in the area the fleet moves on");
        Assert.AreEqual(0f, task.PeakFleetStrength, "the fight won, a later one is counted on its own");
    }

    [TestMethod]
    public void AClearAreaFleetThatClearsItsAreaCountsItsNextFightOnItsOwn()
    {
        SolarSystem system = Target.System;
        var task = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, Enemy, system.Position, system.Radius,
                                    system, 20_000f, MilitaryTaskImportance.Normal);
        var fleet = new Fleet(UState, Enemy) { FleetTask = task, TaskStep = 3 };
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(task, fleet);
        fleet.AddShip(SpawnShip(Warship, Enemy, InSystem));
        task.PeakFleetStrength = fleet.GetStrength() * 2f; // as after the losses of the fight it won

        typeof(Fleet).GetMethod("DoClearAreaOfEnemies", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, new object[] { task });

        Assert.AreEqual(4, fleet.TaskStep, "setup: with no hostile left to attack in its area the fleet moves on");
        Assert.AreEqual(0f, task.PeakFleetStrength, "the fight won, a later one is counted on its own");
    }

    [TestMethod]
    [DataRow(false, 5, DisplayName = "no threat on its way: the fleet looks for what else is left")]
    [DataRow(true, 6, DisplayName = "a threat on its way: the fleet hunts it")]
    public void AClearAreaFleetThatWinsTheFightAtItsPostCountsTheNextOneOnItsOwn(bool threatIncoming, int nextStep)
    {
        SolarSystem system = Target.System;
        var task = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, Enemy, system.Position, system.Radius,
                                    system, 20_000f, MilitaryTaskImportance.Normal);
        var fleet = new Fleet(UState, Enemy) { FleetTask = task, TaskStep = 4 };
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(task, fleet);
        fleet.AddShip(SpawnShip(Warship, Enemy, InSystem));
        if (threatIncoming)
        {
            var threat = (IncomingThreat)System.Activator.CreateInstance(typeof(IncomingThreat), nonPublic: true);
            typeof(IncomingThreat).GetField(nameof(IncomingThreat.TargetSystem))!.SetValue(threat, system);
            typeof(IncomingThreat).GetProperty(nameof(IncomingThreat.ThreatTimer))!.GetSetMethod(nonPublic: true)!.Invoke(threat, new object[] { 30f });
            Enemy.ThreatDetector.SystemsWithThreat = new[] { threat };
        }
        Assert.AreEqual(threatIncoming, Enemy.IsSystemUnderThreatForUs(system), "setup: a threat is on its way to the system, or none");
        task.PeakFleetStrength = fleet.GetStrength() * 2f; // as after the losses of the fight it won at its post

        typeof(Fleet).GetMethod("DoClearAreaOfEnemies", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, new object[] { task });

        Assert.AreEqual(nextStep, fleet.TaskStep, "setup: with no hostile left at its post the fleet moves on");
        Assert.AreEqual(0f, task.PeakFleetStrength, "the fight won, a later one is counted on its own");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ATaskThatTakesANewTargetPlanetCountsTheFightThereOnItsOwn(bool bySetTargetPlanet)
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 5, out MilitaryTask task);
        Assert.IsTrue(fleet.CanTakeThisFight(0f, task, inCombat: true), "setup: the fleet keeps its strength in the fight at its target");
        float whole = fleet.GetStrength();
        task.PeakFleetStrength = whole * 2f; // as after losses there

        fleet.CanTakeThisFight(0f, task, inCombat: true);
        AssertEqual(0.01f, whole * 2f, task.PeakFleetStrength, "the same target is the same fight");

        Planet next = AddDummyPlanet(new Vector2(600_000));
        if (bySetTargetPlanet)
            task.SetTargetPlanet(next);
        else
            task.ChangeTargetPlanet(next); // sets the target directly, as a Remnant defense's retarget and gathering more troops do
        fleet.CanTakeThisFight(0f, task, inCombat: true);

        AssertEqual(0.01f, whole, task.PeakFleetStrength, "a new target planet is a new fight, however the task got it");
    }

    [TestMethod]
    public void AClaimFleetsLostFightCountsAgainstTheEmpireWhoseShipsItFound()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 5, out MilitaryTask task);
        task.TargetEmpire = null;
        LostHalfItsShipsInTheFight(fleet, task);
        StrongerForceSeenInTheSystem(fleet);
        Enemy.InitFleetEmpireStrMultiplier(); // as at the start of a game
        float before = Enemy.GetFleetStrEmpireMultiplier(Player);

        ClaimDefense(fleet, task);

        AssertGreaterThan(Enemy.GetFleetStrEmpireMultiplier(Player), before,
                          "a claim made with no enemy known there counts the lost fight against the empire whose ships the fleet found");
    }

    [TestMethod]
    public void AClaimFleetsLostFightCountsAgainstTheEmpireWhoseShipsItFoundNotTheOneItWasSentAgainst()
    {
        CreateThirdMajorEmpire();
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 5, out MilitaryTask task);
        task.TargetEmpire = ThirdMajor;
        LostHalfItsShipsInTheFight(fleet, task);
        StrongerForceSeenInTheSystem(fleet);
        Enemy.InitFleetEmpireStrMultiplier(); // as at the start of a game
        float found = Enemy.GetFleetStrEmpireMultiplier(Player);
        float sentAgainst = Enemy.GetFleetStrEmpireMultiplier(ThirdMajor);

        ClaimDefense(fleet, task);

        AssertGreaterThan(Enemy.GetFleetStrEmpireMultiplier(Player), found, "the lost fight counts against the empire whose ships the fleet found");
        Assert.AreEqual(sentAgainst, Enemy.GetFleetStrEmpireMultiplier(ThirdMajor), "not against the empire the claim was made against");
    }

    [TestMethod]
    public void AClaimFleetSendsItsCarrierAtTheEnemy()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 6, out MilitaryTask task, carrier: true, warships: 0);
        SeenAtThePlanet(ships: 1);
        Ship carrier = GoingIn(fleet)[0];

        ClaimDefense(fleet, task);

        Assert.IsNotNull(fleet.FleetTask, "setup: the fleet can take on the enemy it sees");
        Assert.AreEqual(AIState.MoveTo, carrier.AI.State, "the carrier is sent at the enemy like a warship");
    }

    [TestMethod]
    public void AClaimFleetsBombersFightWithoutLosingTheirBombardOrders()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 6, out MilitaryTask task, warships: 0, bombers: 2);
        SeenAtThePlanet(ships: 1);
        Ship[] bombers = GoingIn(fleet);
        Assert.AreEqual(RoleType.WarSupport, bombers[0].DesignRoleType, "setup: a bomber is not a warship");
        bombers[0].AI.OrderBombardPlanet(Target, clearOrders: true);
        Assert.AreEqual(AIState.Bombard, bombers[0].AI.State, "setup: one bomber is bombarding");

        ClaimDefense(fleet, task);

        Assert.AreEqual(AIState.Bombard, bombers[0].AI.State, "a bombarding bomber is not pulled off to fight");
        Assert.AreEqual(AIState.MoveTo, bombers[1].AI.State, "the other bomber is sent at the enemy");
        Assert.IsFalse(bombers[1].AI.HasPriorityOrder, "with no priority order, so it can still be ordered to bombard");
    }

    [TestMethod]
    public void AClaimFleetGoingInLeavesItsTroopShipsWaiting()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 4, out MilitaryTask task, troopShips: 3, carrier: true);
        Ship carrier = fleet.Ships.Filter(s => s.Name == Carrier)[0];
        Assert.AreEqual(RoleType.WarSupport, carrier.DesignRoleType, "setup: the carrier is not a warship, like the troop ships");

        ClaimDefense(fleet, task);

        Assert.AreEqual(5, fleet.TaskStep, "setup: the fleet goes in");
        Assert.IsTrue(AllAre(GoingIn(fleet), AIState.FormationMoveTo), "its warships go in");
        Assert.AreEqual(AIState.FormationMoveTo, carrier.AI.State, "and so does its carrier");
        Assert.IsTrue(AllAre(TroopShips(fleet), AIState.HoldPosition), "its troop ships wait where the fleet gathered");
    }

    [TestMethod]
    public void AClaimFleetsShipsAtThePlanetHaveArrivedWithoutItsTroopShips()
    {
        SeenAtThePlanet(ships: 1); // a hostile still in the system, so the fleet takes its steps going in
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 4, out MilitaryTask task, troopShips: 3);
        ClaimDefense(fleet, task);
        AssertGreaterThan(Surface(TroopShips(fleet)) / Surface(fleet.Ships.ToArray()), 1f - ShipGroup.MajorityAssembledSurfaceRatio,
                          "setup: the troop ships are more of the fleet than may be missing when it arrives");

        foreach (Ship ship in GoingIn(fleet))
            ship.Position = fleet.GetFinalPos(ship);
        ClaimDefense(fleet, task);

        Assert.AreEqual(6, fleet.TaskStep, "the ships at the planet have arrived, the troop ships waiting behind do not hold them up");
    }

    [TestMethod]
    public void AClaimFleetOfTroopShipsAloneGivesUpItsClaimRatherThanGoIn()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 4, out MilitaryTask task, troopShips: 3, warships: 0);
        AssertGreaterThan(fleet.GetStrength(), 0f, "setup: armed troop ships pass the strength check");

        ClaimDefense(fleet, task);

        Assert.IsNull(fleet.FleetTask, "troop ships alone do not go in to take a claim");
    }

    [TestMethod]
    public void AClaimFleetLeftWithOnlyItsWaitingTroopShipsGivesUpItsClaim()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 4, out MilitaryTask task, troopShips: 3);
        ClaimDefense(fleet, task);
        Assert.IsTrue(AllAre(TroopShips(fleet), AIState.HoldPosition), "setup: the troop ships wait while the warships go in");

        foreach (Ship warship in GoingIn(fleet))
            fleet.RemoveShip(warship, clearOrders: true);
        AssertGreaterThan(fleet.GetStrength(), 0f, "setup: the armed troop ships left pass the strength check");
        ClaimDefense(fleet, task);

        Assert.IsNull(fleet.FleetTask, "with its warships gone, the fleet gives up its claim instead of waiting for troop ships that hold back");
    }

    // the fleet's ships that went in, at their places at the planet, its troop ships still waiting
    Fleet ShipsAtThePlanet(out MilitaryTask task)
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 4, out task, troopShips: 3);
        ClaimDefense(fleet, task);
        Assert.IsTrue(AllAre(TroopShips(fleet), AIState.HoldPosition), "setup: the troop ships wait");
        foreach (Ship ship in GoingIn(fleet))
            ship.Position = task.AO + ship.FleetOffset;
        fleet.TaskStep = 9;
        return fleet;
    }

    [TestMethod]
    public void AClaimFleetsTroopShipsJoinItInOrbitOnceTheAreaIsClear()
    {
        Fleet fleet = ShipsAtThePlanet(out MilitaryTask task);

        ClaimDefense(fleet, task);

        Assert.IsTrue(AllAre(TroopShips(fleet), AIState.Orbit), "with no enemy left, the troop ships join the fleet in orbit");
    }

    [TestMethod]
    public void AClaimFleetsTroopShipsWaitWhileItsShipsAtThePlanetFight()
    {
        Fleet fleet = ShipsAtThePlanet(out MilitaryTask task);
        GoingIn(fleet)[0].InCombat = true;

        ClaimDefense(fleet, task);

        Assert.IsTrue(AllAre(TroopShips(fleet), AIState.HoldPosition), "while the fleet fights at the planet, the troop ships wait");
    }

    [TestMethod]
    public void AClaimFleetsTroopShipsWaitWhileNoShipThatWentInIsThere()
    {
        Fleet fleet = ShipsAtThePlanet(out MilitaryTask task);
        foreach (Ship ship in GoingIn(fleet))
            ship.Position = OutsideSystem;

        ClaimDefense(fleet, task);

        Assert.IsTrue(AllAre(TroopShips(fleet), AIState.HoldPosition), "with no ship of the fleet at the planet, the troop ships wait");
    }

    [TestMethod]
    public void AClaimFleetWhoseShipsThatWentInAreAllStrandedMovesOn()
    {
        SeenAtThePlanet(ships: 1); // a hostile still in the system, so the fleet takes its steps going in
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 4, out MilitaryTask task);
        ClaimDefense(fleet, task);
        foreach (Ship ship in GoingIn(fleet))
            ship.EnginesKnockedOut = true;

        ClaimDefense(fleet, task);

        Assert.AreEqual(6, fleet.TaskStep, "with every ship that went in stranded, there is no ship left to wait for");
    }

    [TestMethod]
    public void AClaimFleetDoesNotSendAStrandedShipAtTheEnemy()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 6, out MilitaryTask task, carrier: true, warships: 2);
        foreach (Ship warship in GoingIn(fleet).Filter(s => s.Name == Warship))
            warship.EnginesKnockedOut = true;
        SeenAtThePlanet(ships: 1);
        Ship carrier = GoingIn(fleet).Filter(s => s.Name == Carrier)[0];

        ClaimDefense(fleet, task);

        Assert.AreEqual(AIState.MoveTo, carrier.AI.State, "the carrier is sent at the enemy, the stranded warships do not take its place");
        foreach (Ship warship in GoingIn(fleet).Filter(s => s.Name == Warship))
            Assert.AreNotEqual(AIState.MoveTo, warship.AI.State, "a stranded warship is not sent");
    }

    [TestMethod]
    public void AClaimFleetWithEveryShipThatWentInStrandedWaitsWhileHostilesRemain()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 6, out MilitaryTask task, troopShips: 2);
        foreach (Ship ship in GoingIn(fleet))
            ship.EnginesKnockedOut = true;
        SeenAtThePlanet(ships: 1);

        ClaimDefense(fleet, task);

        Assert.AreEqual(6, fleet.TaskStep, "with no ship free to send, the fleet does not move on while hostiles remain");
        ClaimDefense(fleet, task);
        Assert.AreEqual(6, fleet.TaskStep, "and it still waits on its next update");
    }

    [TestMethod]
    public void AClaimFleetLeftWithOnlyTroopShipsAtThePlanetGivesUpItsClaim()
    {
        Fleet fleet = ShipsAtThePlanet(out MilitaryTask task);
        foreach (Ship ship in GoingIn(fleet))
            fleet.RemoveShip(ship, clearOrders: true);
        AssertGreaterThan(fleet.GetStrength(), 0f, "setup: the armed troop ships left pass the strength check");

        ClaimDefense(fleet, task);

        Assert.IsNull(fleet.FleetTask, "troop ships alone do not hold a claim");
    }

    [TestMethod]
    public void AClaimFleetLeftWithOnlyTroopShipsKeepsItsClaimWhileTheyLand()
    {
        Fleet fleet = ShipsAtThePlanet(out MilitaryTask task);
        foreach (Ship ship in GoingIn(fleet))
            fleet.RemoveShip(ship, clearOrders: true);
        TroopShips(fleet)[0].AI.ChangeAIState(AIState.AssaultPlanet);

        ClaimDefense(fleet, task);

        Assert.IsNotNull(fleet.FleetTask, "troop ships landing keep the claim");
    }

    [TestMethod]
    public void AClaimFleetsShipResupplyingAtThePlanetKeepsItsResupply()
    {
        Fleet fleet = ShipsAtThePlanet(out MilitaryTask task);
        Ship resupplying = GoingIn(fleet)[0];
        resupplying.AI.ChangeAIState(AIState.Resupply);
        Ship toSupplyShip = GoingIn(fleet)[1];
        toSupplyShip.AI.ChangeAIState(AIState.ResupplyEscort);

        ClaimDefense(fleet, task);

        Assert.AreEqual(AIState.Resupply, resupplying.AI.State, "the fleet's orbit order does not call off a ship's resupply");
        Assert.AreEqual(AIState.ResupplyEscort, toSupplyShip.AI.State, "nor that of a ship heading to a supply ship");
        Assert.AreEqual(AIState.Orbit, GoingIn(fleet)[2].AI.State, "setup: the fleet's other ships at the planet orbit it");
    }

    [TestMethod]
    [DataRow(AIState.Resupply)]
    [DataRow(AIState.ResupplyEscort)]
    public void AClaimFleetsTroopShipsWaitWhileTheOnlyShipAtThePlanetIsLeavingToResupply(AIState leaving)
    {
        Fleet fleet = ShipsAtThePlanet(out MilitaryTask task);
        Ship[] goingIn = GoingIn(fleet);
        for (int i = 1; i < goingIn.Length; ++i)
            goingIn[i].Position = OutsideSystem;
        goingIn[0].AI.ChangeAIState(leaving);

        ClaimDefense(fleet, task);

        Assert.IsTrue(AllAre(TroopShips(fleet), AIState.HoldPosition), "a ship at the planet that is leaving to resupply, at a planet or a supply ship, does not cover the troop ships");
    }

    [TestMethod]
    public void AClaimFleetsStrandedShipDoesNotHoldUpTheRestArriving()
    {
        SeenAtThePlanet(ships: 1); // a hostile still in the system, so the fleet takes its steps going in
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 4, out MilitaryTask task);
        ClaimDefense(fleet, task);
        Ship[] warships = GoingIn(fleet);
        warships[0].EnginesKnockedOut = true;
        for (int i = 1; i < warships.Length; ++i)
            warships[i].Position = fleet.GetFinalPos(warships[i]);
        AssertGreaterThan(warships[0].SurfaceArea / Surface(warships), 1f - ShipGroup.MajorityAssembledSurfaceRatio,
                          "setup: the stranded ship is more of the fleet than may be missing when it arrives");

        ClaimDefense(fleet, task);

        Assert.AreEqual(6, fleet.TaskStep, "the ships that can move have arrived, the stranded one does not hold them up");
    }

    // the planet's tiles, and an outpost left by a colony that died there, which still fights troops that land
    Building LeftoverOutpost()
    {
        float well = Target.GravityWellRadius;
        Target.GenerateNewFromPlanetType(new SeededRandom(1), ResourceManager.Planets.PlanetOrRandom(1), 1f);
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(Target, well);
        Building outpost = ResourceManager.CreateBuilding(Target, Building.OutpostId);
        outpost.CombatStrength = 18;
        Target.TilesList.Find(t => t.NoBuildingOnTile).PlaceBuilding(outpost, Target);
        Assert.IsNull(Target.Owner, "setup: the planet has no owner");
        Assert.IsTrue(outpost.IsAttackable, "setup: the outpost fights troops that land");
        return outpost;
    }

    [TestMethod]
    public void ABuildingLeftOnAPlanetWithNoOwnerIsNoEnemyOnTheGround()
    {
        LeftoverOutpost();

        Assert.AreEqual(0f, Target.GetGroundStrengthOther(Enemy), "a colony ship takes over the outpost, so it is no ground strength to beat");
        Assert.IsFalse(MarkForColonization.PositiveEnemyPresence(Enemy, Target, out _), "and no reason to claim the planet with a fleet");

        Target.SetOwner(Player);
        AssertGreaterThan(Target.GetGroundStrengthOther(Enemy), 0f, "the same outpost on a planet with an owner defends it");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void OnAPlanetWithNoOwnerOnlyTroopsAtWarWithTheEmpireAreAnEnemyOnTheGround(bool atWar)
    {
        CreateThirdMajorEmpire();
        LeftoverOutpost();
        var troopOwner = atWar ? Player : ThirdMajor;
        Assert.AreEqual(atWar, Enemy.IsAtWarWith(troopOwner), "setup: the claiming empire is at war with the troop's empire, or at peace");
        Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", troopOwner).TryLandTroop(Target), "setup: the troop is on the planet");

        Assert.AreEqual(atWar, Target.GetGroundStrengthOther(Enemy) > 0f,
                        atWar ? "troops at war with the empire are ground strength to beat"
                              : "troops at peace with it are not; a colony ship sends them off when it settles");
        Assert.AreEqual(atWar, MarkForColonization.PositiveEnemyPresence(Enemy, Target, out _),
                        "and only troops at war with it are a reason to claim the planet with a fleet");
    }

    [TestMethod]
    public void AClaimFleetLandsNoTroopsOnAPlanetWithNoOwnerAndOnlyALeftoverBuilding()
    {
        LeftoverOutpost();
        Fleet fleet = ClaimFleet(InSystem, taskStep: 7, out MilitaryTask task, troopShips: 3);

        ClaimDefense(fleet, task);

        Assert.AreEqual(8, fleet.TaskStep, "setup: the fleet has taken its step at the planet");
        Assert.IsFalse(TroopShips(fleet).Any(s => s.AI.State == AIState.AssaultPlanet),
                       "no troop ship lands to fight a building the colony ship takes over");
    }

    [TestMethod]
    public void AClaimFleetLandsTroopsOnAPlanetWithNoOwnerWhereTroopsItIsAtWarWithAre()
    {
        LeftoverOutpost();
        Assert.IsTrue(Enemy.IsAtWarWith(Player), "setup: the claiming empire is at war with the player");
        Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", Player).TryLandTroop(Target), "setup: a player troop is on the planet");
        Fleet fleet = ClaimFleet(InSystem, taskStep: 7, out MilitaryTask task, troopShips: 3);

        ClaimDefense(fleet, task);

        Assert.IsTrue(TroopShips(fleet).Any(s => s.AI.State == AIState.AssaultPlanet), "the troop ships land to fight the enemy troops");
    }

    [TestMethod]
    public void AClaimFleetLandsNoTroopsOnAPlanetWithNoOwnerWhereOnlyTroopsAtPeaceWithItAre()
    {
        CreateThirdMajorEmpire();
        LeftoverOutpost();
        Assert.IsFalse(Enemy.IsAtWarWith(ThirdMajor), "setup: the claiming empire is at peace with the third empire");
        Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", ThirdMajor).TryLandTroop(Target), "setup: a troop of the third empire is on the planet");
        Fleet fleet = ClaimFleet(InSystem, taskStep: 7, out MilitaryTask task, troopShips: 3);

        ClaimDefense(fleet, task);

        Assert.AreEqual(8, fleet.TaskStep, "setup: the fleet has taken its step at the planet");
        Assert.IsFalse(TroopShips(fleet).Any(s => s.AI.State == AIState.AssaultPlanet),
                       "troops it cannot fight give no reason to land; a colony ship sends them off when it settles");
    }

    [TestMethod]
    public void AClaimFleetFormedInsideTheSystemGoesStraightToThePlanet()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 0, out MilitaryTask task);

        ClaimDefense(fleet, task);
        ClaimDefense(fleet, task);

        Assert.AreEqual(5, fleet.TaskStep, "the fleet skips gathering at the system's edge");
        AssertLessThan(fleet.FinalPosition.Distance(Target.Position), Target.GravityWellRadius * 1.5f + 1f, "and moves to the planet");
    }

    [TestMethod]
    public void AClaimFleetOutsideTheSystemGathersBeforeItGoesIn()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 0, out MilitaryTask task);

        ClaimDefense(fleet, task);
        ClaimDefense(fleet, task);

        AssertLessThan(fleet.TaskStep, 4, "a fleet coming from outside still gathers before it goes in");
    }

    [TestMethod]
    public void AClaimFleetGoingIntoASystemWithNoHostileLeftOrbitsThePlanetStraightAway()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 5, out MilitaryTask task);
        AssertEqual(0, Enemy.AI.ThreatMatrix.FindHostileClustersByDist(task.AO, task.AORadius).Length, "setup: no hostile known in the system");

        ClaimDefense(fleet, task);

        AssertEqual(8, fleet.TaskStep, "the fleet skips its fight steps");
        Assert.IsTrue(AllAre(GoingIn(fleet), AIState.Orbit), "and orbits the planet now instead of holding position");
    }

    [TestMethod]
    public void AClaimFleetWithTroopsToLandTakesItsStepsGoingInThoughNoHostileIsLeft()
    {
        LeftoverOutpost();
        Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", Player).TryLandTroop(Target), "setup: a troop the claim must fight is on the planet");
        Fleet fleet = ClaimFleet(InSystem, taskStep: 5, out MilitaryTask task, troopShips: 2);
        AssertEqual(0, Enemy.AI.ThreatMatrix.FindHostileClustersByDist(task.AO, task.AORadius).Length, "setup: no hostile known in the system");

        ClaimDefense(fleet, task);

        Assert.AreEqual(task, fleet.FleetTask, "the fleet keeps its claim");
        AssertLessThan(fleet.TaskStep, 7, "and the troop ships wait until the warships are at the planet");
    }

    [TestMethod]
    public void AGuardFleetWeighsTheShareOfTheForceInTheSystem()
    {
        SeenAtThePlanet(ships: 1);
        float known = Enemy.KnownEnemyStrengthIn(Target.System);
        MilitaryTask guard = MilitaryTask.CreateGuardTask(Enemy, Target);
        guard.MinimumTaskForceStrength = known * 0.9f; // more than the share a fleet on its task weighs, less than the whole force
        var fleet = new Fleet(UState, Enemy) { FleetTask = guard, TaskStep = 2 };
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(guard, fleet);
        fleet.AddShip(SpawnShip(Warship, Enemy, InSystem));
        AssertEqual(1f, Enemy.GetFleetStrEmpireMultiplier(guard.TargetEmpire), "setup: the guard weighs the force by no multiplier");

        typeof(Fleet).GetMethod("DoPreColonizationGuard", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, new object[] { guard });

        Assert.AreEqual(guard, fleet.FleetTask, "a formed guard fleet keeps its post against a force whose share it was formed for");
    }

    [TestMethod]
    public void AColonizationGoalWhoseClaimHasNoFleetYetWeighsTheEnemyWhole()
    {
        for (int i = 0; i < 4; ++i)
            SpawnShip(Warship, Enemy, OutsideSystem + new Vector2(0f, 1000f * i));
        UState.Objects.Update(new FixedSimTime(2f));
        Enemy.UpdateMilitaryStrengths();
        float navy = Enemy.OffensiveStrength;
        float playerShip = SpawnShip(Warship, Player, new Vector2(900_000f)).GetStrength();
        SeenAtThePlanet(ships: (int)(navy / playerShip) + 1);
        float known = Enemy.KnownEnemyStrengthIn(Target.System);
        AssertGreaterThan(known, navy, "setup: the force known in the system is more than the whole navy");
        AssertLessThan(known * Fleet.StayInFightEnemyShare, navy, "setup: but its share that a fleet on its task weighs is not");
        MilitaryTask claim = MilitaryTask.CreateClaimTask(Enemy, Target, 100f, Player, fleetCount: 1);
        var goal = new MarkForColonization(Enemy) { TargetPlanet = Target, Task = claim };
        Enemy.AI.AddGoal(goal);
        goal.AdvanceToNextStep();
        Assert.AreEqual("CheckIfColonizationIsSafe", goal.StepName, "setup: the goal checks its claim under way");
        Assert.IsNull(claim.Fleet, "setup: the claim's fleet has not formed yet");

        GoalStep step = goal.Evaluate();

        Assert.AreEqual(GoalStep.GoalFailed, step, "before a fleet forms, the goal weighs the force whole and gives up");
    }

    [TestMethod]
    public void AColonizationGoalKeepsItsClaimAgainstAForceItsNavyCanStayAgainst()
    {
        ClaimFleet(InSystem, taskStep: 5, out MilitaryTask task);
        UState.Objects.Update(new FixedSimTime(2f));
        Enemy.UpdateMilitaryStrengths();
        float navy = Enemy.OffensiveStrength;
        float playerShip = SpawnShip(Warship, Player, new Vector2(900_000f)).GetStrength();
        SeenAtThePlanet(ships: (int)(navy / playerShip) + 1);
        float known = Enemy.KnownEnemyStrengthIn(Target.System);
        AssertGreaterThan(known, navy, "setup: the force known in the system is more than the whole navy");
        AssertLessThan(known * Fleet.StayInFightEnemyShare, navy, "setup: but its share that a fleet on its task weighs is not");
        var goal = new MarkForColonization(Enemy) { TargetPlanet = Target, Task = task };
        Enemy.AI.AddGoal(goal);
        goal.AdvanceToNextStep();
        Assert.AreEqual("CheckIfColonizationIsSafe", goal.StepName, "setup: the goal checks its claim under way");

        GoalStep step = goal.Evaluate();

        Assert.AreNotEqual(GoalStep.GoalFailed, step, "the goal keeps its colony target");
        Assert.IsFalse(task.QueuedForRemoval, "and its claim goes on");
    }

    [TestMethod]
    public void AColonizationGoalThatRestartsCallsOffItsOldClaimFirst()
    {
        SeenAtThePlanet(ships: 1);
        Fleet fleet = ClaimFleet(InSystem, taskStep: 9, out MilitaryTask old);
        var goal = new MarkForColonization(Enemy) { TargetPlanet = Target, Task = old };
        Enemy.AI.AddGoal(goal);
        Assert.AreEqual("CreateClaimTask", goal.StepName, "setup: the goal starts over, as when its colony ship's queue item is lost");

        goal.Evaluate();

        Assert.IsTrue(old.QueuedForRemoval, "the old claim is called off");
        Assert.AreNotEqual(old, fleet.FleetTask, "so its fleet no longer waits on it");
        Assert.IsNotNull(goal.Task, "setup: a new claim is made against the enemy still there");
        Assert.AreNotSame(old, goal.Task, "in its place");
    }

    [TestMethod]
    public void AColonizationGoalThatRestartsWithNoEnemyLeftKeepsItsClaim()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 9, out MilitaryTask old);
        var goal = new MarkForColonization(Enemy) { TargetPlanet = Target, Task = old };
        Enemy.AI.AddGoal(goal);
        Assert.IsFalse(MarkForColonization.PositiveEnemyPresence(Enemy, Target, out _), "setup: the claim's fight is won");

        goal.Evaluate();

        Assert.IsFalse(old.QueuedForRemoval, "the claim goes on");
        Assert.AreEqual(old, fleet.FleetTask, "so its fleet still guards the planet for the colony ship");
        Assert.AreSame(old, goal.Task, "and the goal keeps it");
    }

    [TestMethod]
    [DataRow(5, true,  false)]
    [DataRow(5, false, false)]
    [DataRow(2, true,  false)]
    [DataRow(2, false, true)]
    public void AColonizationGoalGivesUpOnItsClaimFleetOnlyWhenItIsNeitherMovingInNorThere(int taskStep, bool inSystem, bool givesUp)
    {
        Ship enemy = SpawnShip(Warship, Player, Target.Position + new Vector2(10_000f, 0f));
        enemy.SetSystem(Target.System);
        Enemy.Threats.SetSeen(enemy, fromBackgroundThread: false);
        Enemy.Threats.Update(new FixedSimTime(2f));
        AssertGreaterThan(Enemy.KnownEnemyStrengthIn(Target.System), 10f, "setup: the AI knows of enemies in the system");

        ClaimFleet(inSystem ? InSystem : OutsideSystem, taskStep, out MilitaryTask task);
        var goal = new MarkForColonization(Enemy) { TargetPlanet = Target, Task = task };
        Ship colonyShip = SpawnShip(ColonyShip, Enemy, Target.Position + new Vector2(5000f, 0f));
        goal.FinishedShip = colonyShip;
        colonyShip.AI.OrderColonization(Target, goal);
        for (int i = 0; i < 5; ++i)
            goal.AdvanceToNextStep();
        Assert.AreEqual("WaitForColonizationComplete", goal.StepName, "setup: the colony ship is on its way to settle");

        GoalStep result = goal.Evaluate();

        Assert.AreEqual(givesUp, result == GoalStep.GoalFailed,
                        givesUp ? "a claim fleet neither sent in nor there is no cover, and the goal gives up"
                                : "a claim fleet moving in or already in the system still covers the colony ship");
    }
}
