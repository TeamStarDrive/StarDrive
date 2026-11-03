using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

[TestClass]
public class FleetFlankTests : StarDriveTest
{
    const string FrigateName = "TEST_Spearhead mk1-a";
    const float RivalsApart = 1500f;
    const float FleetOut = 60_000f;
    const string Screen = "ScreenShips", Center = "CenterShips", Left = "LeftShips", Right = "RightShips";

    readonly MilitaryTask Task;
    Vector2 GroupCenter = new(600_000f, 300_000f);
    readonly Array<TestShip> GroupShips = new();
    Ship_Game.Empire Attacker;
    Fleet AIFleet;
    TestShip[] FleetShips;

    public FleetFlankTests()
    {
        LoadStarterShips(FrigateName);
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
        UState.Objects.EnableParallelUpdate = false;
        Attacker = Enemy;
        Task = NewTask();
    }

    MilitaryTask NewTask() => new(AddDummyPlanet(new Vector2(-300_000f)), Enemy, MilitaryTaskImportance.Normal) { AORadius = 30_000f };

    static TestShip Quiet(TestShip ship)
    {
        ship.AI.SetCombatTriggerDelay(1000f);
        return ship;
    }

    // a row of the player's ships, which the AI sees through a scout beside it
    void RivalsAt(Vector2 center, int count = 3)
    {
        for (int i = 0; i < count; ++i)
        {
            TestShip rival = Quiet(SpawnShip(FrigateName, Player, center + new Vector2((i - (count - 1) * 0.5f) * RivalsApart, 0f)));
            rival.AI.OrderHoldPosition(MoveOrder.HoldPosition);
            GroupShips.Add(rival);
        }
        TestShip scout = Quiet(SpawnShip("Vulcan Scout", Attacker, center + new Vector2(0f, 5000f)));
        scout.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        GroupShips.Add(scout);
    }

    Array<Ship> FlankList(string name)
    {
        return (Array<Ship>)typeof(Fleet).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AIFleet);
    }

    int FlankPlanCount()
    {
        object plans = typeof(Fleet).GetField("FlankPlans", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AIFleet);
        return (int)plans!.GetType().GetProperty("Count")!.GetValue(plans)!;
    }

    float RowOffset(int i) => (i - (FleetShips.Length - 1) * 0.5f) * 600f;

    // the AI's ships in a row south of the group, out of its sensor range, each in the named flank list
    void FleetOf(params string[] flanks)
    {
        FleetShips = new TestShip[flanks.Length];
        for (int i = 0; i < flanks.Length; ++i)
            FleetShips[i] = Quiet(SpawnShip(FrigateName, Attacker, GroupCenter + new Vector2(RowOffset(i), FleetOut), Vectors.Up));
        AIFleet = Attacker.CreateFleet(1, null);
        AIFleet.AddShips(FleetShips);
        for (int i = 0; i < flanks.Length; ++i)
        {
            FleetShips[i].RelativeFleetOffset = new Vector2(RowOffset(i), 0f);
            FlankList(flanks[i]).Add(FleetShips[i]);
        }
        // gathered in formation, as before the fleet attacks
        AIFleet.FinalPosition = GroupCenter + new Vector2(0f, FleetOut);
        AIFleet.AssignPositions(Vectors.Up);
        AssertLessThan(FleetShips[0].SensorRange, FleetOut - 5000f, "setup: the fleet starts out of sensor range of the group");
    }

    void SeeTheGroups()
    {
        UState.Objects.Update(new FixedSimTime(2f));
        Attacker.Threats.Update(new FixedSimTime(2f));
    }

    ThreatCluster KnownGroupAt(Vector2 center)
    {
        ThreatCluster[] groups = Attacker.AI.ThreatMatrix.FindHostileClustersByDist(center, 5000f);
        AssertEqual(1, groups.Length, "setup: the AI knows the group");
        return groups[0];
    }

    // the known group as strong as so many of the fleet's ships
    ThreatCluster GroupAsStrongAs(Vector2 center, float fleetShips)
    {
        ThreatCluster group = KnownGroupAt(center);
        group.Strength = fleetShips * FleetShips[0].GetStrength() / Attacker.GetFleetStrEmpireMultiplier(group.Loyalty);
        return group;
    }

    // a fleet of the named flanks against a group as strong as the given number of its ships, which are the ones sent at it
    ThreatCluster FleetAgainstAGroup(GameDifficulty difficulty, float worth, params string[] flanks)
    {
        UState.P.Difficulty = difficulty;
        RivalsAt(GroupCenter);
        FleetOf(flanks);
        SeeTheGroups();
        Task.AO = GroupCenter;
        return GroupAsStrongAs(GroupCenter, worth);
    }

    bool Attack(params Ship[] ships) => AttackFor(Task, ships);

    bool AttackFor(MilitaryTask task, params Ship[] ships)
    {
        MethodInfo attack = typeof(Fleet).GetMethod("AttackEnemyStrengthClumpsInAO", BindingFlags.Instance | BindingFlags.NonPublic,
                                                    null, new[] { typeof(MilitaryTask), typeof(IReadOnlyList<Ship>) }, null);
        Ship[] sent = ships.Length > 0 ? ships : FleetShips.Cast<Ship>().ToArray();
        return (bool)attack!.Invoke(AIFleet, new object[] { task, sent })!;
    }

    static Vector2 Toward(Vector2 from, Vector2 to) => from.DirectionToTarget(to);

    static void AssertHeadOn(TestShip ship, Vector2 groupAt, string what)
    {
        Assert.IsFalse(ship.AI.HasPriorityOrder, $"{what} comes in head-on, free to fight on the way");
        AssertLessThan(ship.AI.MovePosition.Distance(groupAt), 5000f, $"{what} heads for the group");
    }

    // the end of a flank leg, which keeps the warp exit range from the group's edge
    static Vector2 FlankPointOf(TestShip ship, ThreatCluster group, string what)
    {
        Assert.IsTrue(ship.AI.HasPriorityOrder, $"{what} flanks on a locked move");
        Vector2 point = ship.AI.MovePosition;
        AssertGreaterThan(point.Distance(group.Position), group.Radius + FlankGeometry.WarpExitRange - 1f, $"{what} keeps the warp exit range from the group's edge");
        return point;
    }

    // the fleet comes from the south: its left is -X, its right +X, and the group's rear -Y
    static void AssertOnLeft(TestShip ship, ThreatCluster group, string what)
    {
        AssertLessThan(Toward(group.Position, FlankPointOf(ship, group, what)).X, -0.9f, $"{what} goes to the left, -X");
    }

    static void AssertOnRight(TestShip ship, ThreatCluster group, string what)
    {
        AssertGreaterThan(Toward(group.Position, FlankPointOf(ship, group, what)).X, 0.9f, $"{what} goes to the right, +X");
    }

    static void AssertAtRear(TestShip ship, ThreatCluster group, string what)
    {
        AssertLessThan(Toward(group.Position, FlankPointOf(ship, group, what)).Y, -0.9f, $"{what} goes to the rear, -Y");
    }

    [TestMethod]
    public void AtNormalTheWholeFleetComesInHeadOn()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Normal, 2.5f, Left, Right, Screen, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        for (int i = 0; i < 3; ++i)
            AssertHeadOn(FleetShips[i], group.Position, $"ship {i}");
    }

    [TestMethod]
    public void AtHardTheLeftAndRightShipsGoToTheSidesAndTheScreenComesInHeadOn()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "the screen ship");
        AssertOnLeft(FleetShips[1], group, "the left ship");
        AssertOnRight(FleetShips[2], group, "the right ship");
    }

    [TestMethod]
    public void AtHardTheLongRangeShipsComeInHeadOnAndNoneGoesToTheRear()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 3.5f, Center, Left, Left, Left, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "the long range ship");
        for (int i = 1; i <= 3; ++i)
            AssertOnLeft(FleetShips[i], group, $"left ship {i}");
    }

    [TestMethod]
    public void AtBrutalTwoSideShipsSendNoneToTheRear()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Left, Right, Screen, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertOnLeft(FleetShips[0], group, "the left ship");
        AssertOnRight(FleetShips[1], group, "the right ship");
    }

    // at Brutal the second of three side ships, the fastest, goes to the rear from where it is put
    TestShip RearShipFrom(Vector2 fromGroup, out ThreatCluster group)
    {
        group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Left, Left, Right, Screen, Screen, Screen);
        TestShip rear = FleetShips[1];
        rear.MaxSTLSpeed = FleetShips[0].MaxSTLSpeed * 2f;
        rear.Position = GroupCenter + fromGroup;
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertAtRear(rear, group, "the fastest side ship");
        return rear;
    }

    [TestMethod]
    public void AtBrutalTheFastestThirdOfTheSideShipsGoesToTheRear()
    {
        RearShipFrom(new Vector2(-300f, FleetOut), out ThreatCluster group);

        AssertOnLeft(FleetShips[0], group, "the other left ship");
        AssertOnRight(FleetShips[2], group, "the right ship");
    }

    static void AssertRoundTheSide(TestShip rear, ThreatCluster group, float sideX)
    {
        WayPoint[] legs = rear.AI.CopyWayPoints();
        AssertEqual(2, legs.Length, "the way straight to the rear passes the group, so it goes round a side");
        AssertGreaterThan(Toward(group.Position, legs[0].Position).X * sideX, 0.9f, "round the side nearer it");
        AssertGreaterThan(NearestOnLeg(legs[0].Position, legs[1].Position, group.Position), group.Radius + FlankGeometry.WarpExitRange - 1f,
                          "and the leg from the side to the rear keeps the warp exit range from the group's edge");
    }

    static float NearestOnLeg(Vector2 from, Vector2 to, Vector2 point)
    {
        Vector2 leg = to - from;
        float along = ((point - from).Dot(leg) / leg.SqLen()).Clamped(0f, 1f);
        return (from + leg * along).Distance(point);
    }

    [TestMethod]
    public void ARearShipWestOfTheGroupGoesRoundTheLeft()
    {
        TestShip rear = RearShipFrom(new Vector2(-3000f, FleetOut), out ThreatCluster group);
        AssertRoundTheSide(rear, group, sideX: -1f);
    }

    [TestMethod]
    public void ARearShipEastOfTheGroupGoesRoundTheRight()
    {
        TestShip rear = RearShipFrom(new Vector2(3000f, FleetOut), out ThreatCluster group);
        AssertRoundTheSide(rear, group, sideX: 1f);
    }

    [TestMethod]
    public void ARearShipAlreadyRoundTheSideGoesStraight()
    {
        TestShip rear = RearShipFrom(new Vector2(20_000f, -20_000f), out _);
        AssertEqual(1, rear.AI.CopyWayPoints().Length, "the way straight to the rear is clear");
    }

    [TestMethod]
    public void ARearShipGoesPastAnotherGroupBehindTheTarget()
    {
        UState.P.Difficulty = GameDifficulty.Brutal;
        Vector2 behind = GroupCenter + new Vector2(0f, -16_000f);
        RivalsAt(GroupCenter);
        RivalsAt(behind, 1);
        FleetOf(Left, Left, Right, Screen, Screen, Screen);
        SeeTheGroups();
        Task.AO = GroupCenter;
        ThreatCluster group = GroupAsStrongAs(GroupCenter, 2.5f);
        ThreatCluster other = KnownGroupAt(behind);
        TestShip rear = FleetShips[1];
        rear.MaxSTLSpeed = FleetShips[0].MaxSTLSpeed * 2f;
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertAtRear(rear, group, "the fastest side ship");
        AssertGreaterThan(rear.AI.MovePosition.Distance(other.Position), other.Radius + FlankGeometry.WarpExitRange - 1f,
                          "past the other group behind the target");
    }

    [TestMethod]
    public void AnAIRacesRearShipDoesNotGoToAPointInAGravityWell()
    {
        AssertGreaterThan(UState.P.GravityWellRange, 0f, "setup: gravity wells are on");
        Vector2 wellAt = GroupCenter + new Vector2(0f, -30_000f);
        Planet planet = AddDummyPlanet(wellAt + new Vector2(60_000f, 0f), 1f, 1f, 4f, wellAt, explored: true);
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(planet, 25_000f);
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Left, Left, Right, Screen, Screen, Screen);
        Assert.AreEqual(planet.System, group.System, "setup: the group is in the planet's system");
        TestShip rear = FleetShips[1];
        rear.MaxSTLSpeed = FleetShips[0].MaxSTLSpeed * 2f;
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(rear, group.Position, "the fastest side ship, the group's rear lying in the well,");
        AssertOnLeft(FleetShips[0], group, "the other left ship");
    }

    [TestMethod]
    public void TheRearShipsSpreadByTheirFormationPlaces()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 5.5f, Left, Left, Left, Right, Right, Right, Screen, Screen, Screen);
        foreach (int i in new[] { 1, 4 })
            FleetShips[i].MaxSTLSpeed = FleetShips[0].MaxSTLSpeed * 2f;
        AssertTrue(Attack(), "setup: the fleet attacks");

        Vector2 first = FlankPointOf(FleetShips[1], group, "the first rear ship");
        Vector2 second = FlankPointOf(FleetShips[4], group, "the second rear ship");
        AssertLessThan(Toward(group.Position, first).Y, -0.9f, "setup: the first goes to the rear");
        AssertLessThan(Toward(group.Position, second).Y, -0.9f, "setup: so does the second");
        AssertGreaterThan(first.Distance(second), 300f, "two rear ships do not leave warp on the same spot");
    }

    [TestMethod]
    public void AtBrutalTheLongRangeShipsGoHalfToEachSideAtTheirWeaponRangeAndTheCommandShipComesInHeadOn()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Center, Center, Center, Screen, Screen, Screen, Screen);
        AIFleet.SetCommandShip(FleetShips[0]);
        PropertyInfo weaponsRange = typeof(Ship).GetProperty(nameof(Ship.WeaponsMaxRange))!;
        weaponsRange.SetValue(FleetShips[1], 15_000f);
        weaponsRange.SetValue(FleetShips[2], 15_000f);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "the command ship");
        AssertOnLeft(FleetShips[1], group, "the first long range ship");
        AssertOnRight(FleetShips[2], group, "the second long range ship");
        for (int i = 1; i <= 2; ++i)
            AssertGreaterThan(FleetShips[i].AI.MovePosition.Distance(group.Position), group.Radius + 15_000f - 1f,
                              $"long range ship {i} keeps its weapon range from the group's edge");
    }

    // a group of one rival 25,000 south of a group of three, both in the area
    void TwoGroups(GameDifficulty difficulty, out ThreatCluster weakNear, out ThreatCluster strongFar)
    {
        UState.P.Difficulty = difficulty;
        Vector2 far = GroupCenter + new Vector2(0f, -25_000f);
        RivalsAt(GroupCenter, 1);
        RivalsAt(far);
        FleetOf(Screen, Screen, Screen, Screen, Screen, Screen, Screen, Screen);
        SeeTheGroups();
        Task.AO = GroupCenter;
        weakNear = GroupAsStrongAs(GroupCenter, 1.5f);
        strongFar = GroupAsStrongAs(far, 2.5f);
    }

    [TestMethod]
    public void AtHardTheNearestGroupComesFirstOneForOne()
    {
        TwoGroups(GameDifficulty.Hard, out ThreatCluster weakNear, out ThreatCluster strongFar);
        AssertTrue(Attack(), "setup: the fleet attacks");

        for (int i = 0; i < 2; ++i)
            AssertHeadOn(FleetShips[i], weakNear.Position, $"ship {i}");
        for (int i = 2; i < 5; ++i)
            AssertHeadOn(FleetShips[i], strongFar.Position, $"ship {i}");
    }

    [TestMethod]
    public void AtInsaneTheStrongestGroupComesFirstWithHalfAgainItsStrength()
    {
        TwoGroups(GameDifficulty.Insane, out ThreatCluster weakNear, out ThreatCluster strongFar);
        AssertTrue(Attack(), "setup: the fleet attacks");

        for (int i = 0; i < 4; ++i)
            AssertHeadOn(FleetShips[i], strongFar.Position, $"ship {i}");
        for (int i = 4; i < 6; ++i)
            AssertHeadOn(FleetShips[i], weakNear.Position, $"ship {i}");
    }

    // a planet with long range guns, the player's unless named, its gravity well kept small
    Planet PlanetWithGuns(Vector2 at, Ship_Game.Empire owner = null)
    {
        Planet planet = AddHomeWorldToEmpire(at + new Vector2(0f, -60_000f), owner ?? Player, at, explored: true);
        planet.TilesList.First(t => t.NoBuildingOnTile).PlaceBuilding(ResourceManager.CreateBuilding(planet, "Cloud Peak"), planet);
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(planet, 1000f);
        AssertGreaterThan(planet.GunReach(), 10_000f, "setup: the planet's guns reach far");
        return planet;
    }

    static readonly string[] SideShips = { Left, Right, Screen, Screen, Screen };

    ThreatCluster GroupNearPlanetGuns(GameDifficulty difficulty, Planet planet, Vector2 groupFromPlanet, string[] flanks)
    {
        GroupCenter = planet.Position + groupFromPlanet;
        ThreatCluster group = FleetAgainstAGroup(difficulty, 2.5f, flanks);
        Assert.AreEqual(planet.System, group.System, "setup: the group is in the planet's system");
        return group;
    }

    static bool InReach(Planet planet, Vector2 position) => position.InRadius(planet.Position, planet.GunReach());

    static Vector2 NearestSidePoint(ThreatCluster group, float sideX)
    {
        return group.Position + new Vector2(sideX * (group.Radius + FlankGeometry.WarpExitRange), 0f);
    }

    // the planet's guns reach the nearest point on one side, -X left or +X right, but not on the other
    ThreatCluster GroupWithPlanetGunsOn(float sideX, GameDifficulty difficulty, params string[] flanks)
    {
        return GroupWithPlanetGunsOn(sideX, difficulty, Player, flanks);
    }

    ThreatCluster GroupWithPlanetGunsOn(float sideX, GameDifficulty difficulty, Ship_Game.Empire owner, params string[] flanks)
    {
        Planet planet = PlanetWithGuns(new Vector2(600_000f, 300_000f), owner);
        ThreatCluster group = GroupNearPlanetGuns(difficulty, planet, new Vector2(-sideX * (planet.GunReach() + 7000f), 0f),
                                                  flanks.Length > 0 ? flanks : SideShips);
        Assert.IsTrue(InReach(planet, NearestSidePoint(group, sideX)) && !InReach(planet, NearestSidePoint(group, -sideX)),
                      "setup: the guns reach one side only");
        return group;
    }

    [TestMethod]
    public void AtInsaneASideInReachOfAnEnemyPlanetsGunsJoinsTheOtherSide()
    {
        ThreatCluster group = GroupWithPlanetGunsOn(-1f, GameDifficulty.Insane);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertOnRight(FleetShips[0], group, "the left ship");
        AssertOnRight(FleetShips[1], group, "the right ship");
    }

    [TestMethod]
    public void AtInsaneTheRightSideInReachOfPlanetGunsJoinsTheLeft()
    {
        ThreatCluster group = GroupWithPlanetGunsOn(1f, GameDifficulty.Insane);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertOnLeft(FleetShips[0], group, "the left ship");
        AssertOnLeft(FleetShips[1], group, "the right ship");
    }

    [TestMethod]
    public void AtInsaneTheFleetsOwnPlanetsGunsBarNoSide()
    {
        ThreatCluster group = GroupWithPlanetGunsOn(-1f, GameDifficulty.Insane, Enemy);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertOnLeft(FleetShips[0], group, "the left ship, beside its own planet's guns,");
    }

    [TestMethod]
    public void APlanetsGunsReachNoFurtherThanItSees()
    {
        Planet planet = AddHomeWorldToEmpire(new Vector2(600_000f, 240_000f), Player, new Vector2(600_000f, 300_000f), explored: true);
        bool hasGuns = false;
        foreach (Building b in planet.Buildings)
            hasGuns |= b.IsWeapon;
        Assert.IsFalse(hasGuns || planet.NumBuildings == 0, "setup: the planet has buildings, but no guns");
        AssertEqual(0.5f, 0f, planet.GunReach(), "a planet without guns reaches nothing");

        planet.TilesList.First(t => t.NoBuildingOnTile).PlaceBuilding(ResourceManager.CreateBuilding(planet, "Cloud Peak"), planet);
        AssertGreaterThan(planet.GunReach(), 15_000f, "setup: its guns fire far");
        typeof(Planet).GetProperty(nameof(Planet.SensorRange))!.SetValue(planet, 12_000f);
        AssertEqual(0.5f, 12_000f, planet.GunReach(), "a planet seeing less far than its guns fire reaches only as far as it sees");
    }

    [TestMethod]
    public void AtBrutalASideInReachOfAnEnemyPlanetsGunsIsKept()
    {
        ThreatCluster group = GroupWithPlanetGunsOn(-1f, GameDifficulty.Brutal);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertOnLeft(FleetShips[0], group, "the left ship");
    }

    [TestMethod]
    public void AtInsaneARearShipGoesRoundTheSideOutOfReachOfPlanetGuns()
    {
        ThreatCluster group = GroupWithPlanetGunsOn(-1f, GameDifficulty.Insane, Left, Left, Right, Screen, Screen, Screen);
        TestShip rear = FleetShips[1];
        rear.MaxSTLSpeed = FleetShips[0].MaxSTLSpeed * 2f;
        rear.Position = GroupCenter + new Vector2(-3000f, FleetOut);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertAtRear(rear, group, "the fastest side ship");
        AssertRoundTheSide(rear, group, sideX: 1f);
    }

    [TestMethod]
    public void AtInsaneBothSidesInReachOfPlanetGunsAreKept()
    {
        Planet planet = PlanetWithGuns(new Vector2(600_000f, 300_000f));
        ThreatCluster group = GroupNearPlanetGuns(GameDifficulty.Insane, planet, new Vector2(0f, 5000f), SideShips);
        Assert.IsTrue(InReach(planet, NearestSidePoint(group, -1f)) && InReach(planet, NearestSidePoint(group, 1f)),
                      "setup: the guns reach both sides");
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertOnLeft(FleetShips[0], group, "the left ship");
        AssertOnRight(FleetShips[1], group, "the right ship");
    }

    // a planet whose gravity well covers the group's left side out to twice as far, but not the group or its right
    ThreatCluster GroupWithAWellOnTheLeft()
    {
        AssertGreaterThan(UState.P.GravityWellRange, 0f, "setup: gravity wells are on");
        Vector2 wellAt = GroupCenter + new Vector2(-30_000f, 0f);
        Planet planet = AddDummyPlanet(wellAt + new Vector2(0f, -60_000f), 1f, 1f, 4f, wellAt, explored: true);
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(planet, 25_000f);
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, SideShips);
        Assert.AreEqual(planet.System, group.System, "setup: the group is in the planet's system");
        return group;
    }

    [TestMethod]
    public void AnAIRacesFleetShipDoesNotFlankToAPointInAGravityWell()
    {
        ThreatCluster group = GroupWithAWellOnTheLeft();
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "the left ship, whose side lies in the well,");
        AssertOnRight(FleetShips[1], group, "the right ship");
    }

    [TestMethod]
    public void ARemnantFleetShipFlanksToAPointInAGravityWell()
    {
        CreateAMinorFaction("Remnant");
        Attacker = Faction;
        ThreatCluster group = GroupWithAWellOnTheLeft();
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertOnLeft(FleetShips[0], group, "a Remnant left ship, held by no well,");
    }

    [TestMethod]
    public void AnAIRacesFleetShipDoesNotFlankIntoAStarsRadiation()
    {
        Vector2 sunAt = GroupCenter + new Vector2(-20_000f, 0f);
        Planet planet = AddDummyPlanet(sunAt, 1f, 1f, 4f, sunAt + new Vector2(0f, -80_000f), explored: true);
        SolarSystem system = planet.System;
        system.Sun = SunType.FindSun("star_neutron");
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, SideShips);
        Assert.AreEqual(system, group.System, "setup: the group is in the star's system");
        Assert.IsTrue(!system.InSafeDistanceFromRadiation(group.Position + new Vector2(-12_000f, 0f))
                      && system.InSafeDistanceFromRadiation(group.Position)
                      && system.InSafeDistanceFromRadiation(group.Position + new Vector2(12_000f, 0f)), "setup: the radiation covers the left side only");
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "the left ship, whose side lies in the radiation,");
        AssertOnRight(FleetShips[1], group, "the right ship");
    }

    [TestMethod]
    public void AnotherKnownGroupInTheAreaPushesTheSidePointOut()
    {
        UState.P.Difficulty = GameDifficulty.Hard;
        Vector2 aside = GroupCenter + new Vector2(-16_000f, 0f);
        RivalsAt(GroupCenter);
        RivalsAt(aside, 1);
        FleetOf(Screen, Left, Right, Screen, Screen);
        SeeTheGroups();
        Task.AO = GroupCenter;
        ThreatCluster group = GroupAsStrongAs(GroupCenter, 2.5f);
        ThreatCluster other = KnownGroupAt(aside);
        AssertTrue(Attack(), "setup: the fleet attacks");

        Vector2 point = FlankPointOf(FleetShips[1], group, "the left ship");
        AssertGreaterThan(point.Distance(other.Position), other.Radius + FlankGeometry.WarpExitRange - 1f,
                          "it keeps the warp exit range from the other group in the area too");
    }

    [TestMethod]
    public void ASideLegKeepsTheRangeFromTheGroupAllTheWay()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        TestShip flanker = FleetShips[1];
        Vector2 point = FlankPointOf(flanker, group, "the left ship");
        AssertGreaterThan(NearestOnLeg(flanker.Position, point, group.Position), group.Radius + FlankGeometry.WarpExitRange - 1f,
                          "its way there keeps the warp exit range from the group's edge");
    }

    [TestMethod]
    public void ARearShipsFirstLegKeepsTheRangeFromTheGroup()
    {
        TestShip rear = RearShipFrom(new Vector2(-1000f, 11_000f), out ThreatCluster group);

        WayPoint[] legs = rear.AI.CopyWayPoints();
        AssertEqual(2, legs.Length, "setup: from in front of the group the rear ship goes round a side");
        AssertGreaterThan(NearestOnLeg(rear.Position, legs[0].Position, group.Position), group.Radius + FlankGeometry.WarpExitRange - 1f,
                          "its way to the side keeps the warp exit range from the group's edge");
    }

    [TestMethod]
    public void TheShipsOfASideSpreadByTheirFormationPlaces()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Left, Screen, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        Vector2 first = FlankPointOf(FleetShips[1], group, "the first left ship");
        Vector2 second = FlankPointOf(FleetShips[2], group, "the second left ship");
        AssertGreaterThan(first.Distance(second), 300f, "two ships of a side do not leave warp on the same spot");
    }

    [TestMethod]
    public void ASentShipOnAnotherLockedMoveIsNotKeptOnIt()
    {
        TestShip flanker = FlankerShortOfItsPoint(out _);
        flanker.AI.OrderMoveTo(GroupCenter + new Vector2(0f, 20_000f), Vectors.Up);

        AIFleet.DoOrbitAreaRestricted(Task.TargetPlanet, Task.AO, Task.AORadius);

        Assert.AreEqual(AIState.Orbit, flanker.AI.State, "only its flank leg is kept");
    }

    [TestMethod]
    public void ASentShipWhoseLegIsOverIsNotKeptOnIt()
    {
        TestShip flanker = FlankerShortOfItsPoint(out Vector2 point);
        flanker.Position = point;
        flanker.AI.ClearPriorityOrderAndTarget();

        AIFleet.DoOrbitAreaRestricted(Task.TargetPlanet, Task.AO, Task.AORadius);

        Assert.AreEqual(AIState.Orbit, flanker.AI.State, "at its point and no longer locked, it takes the fleet's orders again");
    }

    int LegsInTheFirstPlan()
    {
        object plans = typeof(Fleet).GetField("FlankPlans", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AIFleet);
        object plan = plans!.GetType().GetProperty("Item")!.GetValue(plans, new object[] { 0 });
        object legs = plan!.GetType().GetField("Legs")!.GetValue(plan);
        return (int)legs!.GetType().GetProperty("Count")!.GetValue(legs)!;
    }

    [TestMethod]
    public void ADeadShipLeavesItsPlan()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(2, LegsInTheFirstPlan(), "setup: the plan holds the left and right ships' legs");

        FleetShips[1].Die(FleetShips[1], cleanupOnly: true);
        RunObjectsSim(TestSimStep);
        AssertTrue(Attack(), "setup: the fleet attacks again");

        AssertEqual(1, LegsInTheFirstPlan(), "the dead ship's leg is dropped");
    }

    [TestMethod]
    public void AShipBackFromItsFlankLegComesInStraight()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertOnLeft(FleetShips[1], group, "setup: the left ship");

        FleetShips[1].AI.ClearOrders();
        AssertTrue(Attack(), "setup: the fleet attacks again");

        AssertHeadOn(FleetShips[1], group.Position, "the left ship, its leg done,");
    }

    [TestMethod]
    public void TheSidesStayWherePlannedAsTheFleetMoves()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen, Left);
        TestShip newcomer = FleetShips[5];
        AssertTrue(Attack(FleetShips[0], FleetShips[1], FleetShips[2], FleetShips[3], FleetShips[4]), "setup: the fleet attacks without the last ship");
        foreach (int i in new[] { 0, 3, 4 })
        {
            FleetShips[i].Position = GroupCenter + new Vector2(120_000f, FleetOut);
            FleetShips[i].AI.ClearOrders();
        }
        AssertLessThan(Toward(AIFleet.AveragePosition(force: true), group.Position).X, -0.5f, "setup: the fleet now lies south-east of the group");

        AssertTrue(Attack(newcomer, FleetShips[0], FleetShips[3], FleetShips[4]), "setup: the fleet attacks again");

        AssertOnLeft(newcomer, group, "a left ship joining later goes to the left as planned");
    }

    [TestMethod]
    public void APlanIsDroppedOnceItsGroupHasMovedOff()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");

        group.Position += new Vector2(20_000f, 0f);
        AssertTrue(Attack(), "setup: the fleet attacks again, too few ships free to split");

        AssertEqual(0, FlankPlanCount(), "the plan round where the group was is dropped");
    }

    [TestMethod]
    public void APlanStaysWhileItsGroupDriftsLessThanTheJoinRadius()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        foreach (int i in new[] { 1, 3, 4 })
            FleetShips[i].AI.ClearOrders();

        group.Position += new Vector2(5000f, 0f);
        AssertTrue(Attack(), "setup: the fleet attacks again");

        AssertHeadOn(FleetShips[1], group.Position, "the left ship, its leg done, still in the plan");
        AssertEqual(1, FlankPlanCount(), "the plan stays");
    }

    [TestMethod]
    public void ANewTaskDropsTheOldPlans()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");

        MilitaryTask next = NewTask();
        next.AO = GroupCenter;
        AssertTrue(AttackFor(next), "setup: the fleet attacks for its next task, too few ships free to split");

        AssertEqual(0, FlankPlanCount(), "the old task's plans are dropped");
    }

    // the left ship put sublight 5,000 short of its point, where it sees the group
    TestShip FlankerShortOfItsPoint(out Vector2 point)
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        TestShip flanker = FleetShips[1];
        point = FlankPointOf(flanker, group, "setup: the left ship");
        flanker.Position = point + new Vector2(0f, 5000f);
        return flanker;
    }

    [TestMethod]
    public void AFlankLegKeepsItsLockThroughTheFleetsCombatCheck()
    {
        TestShip flanker = FlankerShortOfItsPoint(out Vector2 point);
        RunObjectsSim(1.5f);
        Assert.IsTrue(flanker.AI.BadGuysNear && !flanker.IsSpoolingOrInWarp, "setup: sublight, it sees the group");

        Assert.AreEqual(ShipGroup.CombatStatus.InCombat, AIFleet.FleetInAreaInCombat(Task.AO, Task.AORadius),
                        "the only ship near the group, a flanker on its leg into the fight, counts as fighting");

        Assert.IsTrue(flanker.AI.HasPriorityOrder, "the fleet's combat check leaves its leg locked");
        AssertLessThan(flanker.AI.MovePosition.Distance(point), 1f, "on its way to its point");
    }

    [TestMethod]
    public void AFlankerThatSeesNoEnemyLeavesTheAreaClear()
    {
        TestShip flanker = FlankerShortOfItsPoint(out _);
        Assert.IsFalse(flanker.AI.BadGuysNear, "setup: put there but not yet looked round, it sees no enemy");

        AssertEqual(ShipGroup.CombatStatus.ClearSpace, AIFleet.FleetInAreaInCombat(Task.AO, Task.AORadius),
                    "a flanker on its leg is in the fight only once it sees the enemy");
    }

    [TestMethod]
    public void APlanIsDroppedEvenWhenNoShipIsFree()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");

        group.Position += new Vector2(20_000f, 0f);
        MethodInfo attack = typeof(Fleet).GetMethod("AttackEnemyStrengthClumpsInAO", BindingFlags.Instance | BindingFlags.NonPublic,
                                                    null, new[] { typeof(MilitaryTask), typeof(IReadOnlyList<Ship>) }, null);
        Assert.IsFalse((bool)attack!.Invoke(AIFleet, new object[] { Task, System.Array.Empty<Ship>() })!, "setup: no ship is free");

        AssertEqual(0, FlankPlanCount(), "the plan round where the group was is dropped all the same");
    }

    [TestMethod]
    public void AResetFleetDropsItsPlans()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");

        AIFleet.Reset(fleeIfInCombat: false);

        AssertEqual(0, FlankPlanCount(), "a reset fleet keeps no plans");
    }

    [TestMethod]
    public void ARearShipOnItsFirstLegIsKeptOnIt()
    {
        TestShip rear = RearShipFrom(new Vector2(-3000f, FleetOut), out _);
        WayPoint[] legs = rear.AI.CopyWayPoints();
        AssertEqual(2, legs.Length, "setup: it goes round a side");
        rear.Position = legs[0].Position + new Vector2(0f, 5000f);

        AIFleet.DoOrbitAreaRestricted(Task.TargetPlanet, Task.AO, Task.AORadius);

        Assert.IsTrue(rear.AI.HasPriorityOrder && rear.AI.State != AIState.Orbit, "on its way round the side the rear ship keeps its leg");
    }

    [TestMethod]
    public void AFlankerWithItsEnginesKnockedOutIsNotKeptOnItsLeg()
    {
        TestShip flanker = FlankerShortOfItsPoint(out _);
        flanker.EnginesKnockedOut = true;

        AIFleet.DoOrbitAreaRestricted(Task.TargetPlanet, Task.AO, Task.AORadius);

        Assert.AreEqual(AIState.Orbit, flanker.AI.State, "a flanker that cannot fly its leg takes the fleet's orders again");
    }

    [TestMethod]
    public void AFlankLegKeepsItsLockThroughTheAreaOrbitOrder()
    {
        TestShip flanker = FlankerShortOfItsPoint(out Vector2 point);
        TestShip screen = FleetShips[0];
        screen.Position = GroupCenter + new Vector2(0f, 15_000f);

        AIFleet.DoOrbitAreaRestricted(Task.TargetPlanet, Task.AO, Task.AORadius);

        Assert.AreEqual(AIState.Orbit, screen.AI.State, "setup: a ship in the area is sent to orbit");
        Assert.IsTrue(flanker.AI.HasPriorityOrder, "the flanker keeps its locked leg");
        AssertLessThan(flanker.AI.MovePosition.Distance(point), 1f, "to its point");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AShipOnAFlankLegIsNotSentOnWithTheShipsLeftOver(bool withRearShip)
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        if (withRearShip)
            RearShipOfTheFleet();
        AssertTrue(Attack(), "setup: the whole fleet attacks");
        Vector2 left = FlankPointOf(FleetShips[1], group, "setup: the left ship");
        Vector2 right = FlankPointOf(FleetShips[2], group, "setup: the right ship");

        group.Strength = 0.5f * FleetShips[0].GetStrength() / Attacker.GetFleetStrEmpireMultiplier(group.Loyalty);
        AssertTrue(Attack(), "setup: the whole fleet attacks again, and the screen ship alone now covers the group");

        AssertLessThan(FlankPointOf(FleetShips[1], group, "the left ship").Distance(left), 1f, "the left ship keeps its leg");
        AssertLessThan(FlankPointOf(FleetShips[2], group, "the right ship").Distance(right), 1f, "the right ship keeps its leg");
    }

    TestShip RearShipOfTheFleet()
    {
        TestShip rear = Quiet(SpawnShip(FrigateName, Attacker, GroupCenter + new Vector2(0f, FleetOut + 3000f), Vectors.Up));
        AIFleet.AddShip(rear);
        FlankList("RearShips").Add(rear);
        return rear;
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AShipAlreadyFightingIsNotSentOnWithTheShipsLeftOver(bool withRearShip)
    {
        FleetAgainstAGroup(GameDifficulty.Normal, 0.5f, Screen, Screen, Screen, Screen);
        TestShip rear = withRearShip ? RearShipOfTheFleet() : null;
        TestShip fighter = FleetShips[1];
        fighter.InCombat = true;
        Assert.IsFalse(fighter.AI.HasPriorityOrder, "setup: the fighting ship is on no locked move");

        AssertTrue(Attack(), "setup: the whole fleet attacks, and the first ship alone covers the group");
        if (withRearShip)
            AssertEqual(rear, FleetShips[2].AI.EscortTarget, "setup: the next ship left over escorts the rear ship");
        else
            Assert.IsTrue(FleetShips[2].AI.HasPriorityOrder, "setup: the next ship left over is locked onto a move to the area");

        Assert.IsFalse(fighter.AI.HasPriorityOrder, "a ship already fighting is not locked onto a move to the area");
        Assert.AreNotEqual(AIState.Escort, fighter.AI.State, "nor sent to escort the rear ships");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ARearShipLeftOverInAWholeFleetPassTakesNoOrder(bool firstLeftOver)
    {
        FleetAgainstAGroup(GameDifficulty.Normal, 0.5f, Screen, Screen, Screen);
        TestShip rear = RearShipOfTheFleet();
        Assert.IsFalse(rear.AI.HasPriorityOrder || rear.InCombat, "setup: the rear ship is free to take orders");
        Ship[] ships = firstLeftOver
            ? new Ship[] { FleetShips[0], rear, FleetShips[1], FleetShips[2] }
            : new Ship[] { FleetShips[0], FleetShips[1], rear, FleetShips[2] };

        AssertTrue(Attack(ships), "setup: the first ship alone covers the group");

        Assert.IsNull(rear.AI.EscortTarget, "the rear ship is not told to escort, least of all itself");
        Assert.IsFalse(rear.AI.HasPriorityOrder, "nor locked onto a move to the area");
        AssertEqual(rear, FleetShips[1].AI.EscortTarget, "a free ship left over escorts it instead");
    }

    [TestMethod]
    public void OneShipSentAtAGroupComesInHeadOn()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 0.5f, SideShips);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "a left ship sent alone");
    }

    [TestMethod]
    public void TheCommandShipComesInHeadOnFromAnyList()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, SideShips);
        AIFleet.SetCommandShip(FleetShips[0]);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "the command ship, a left ship,");
        AssertOnRight(FleetShips[1], group, "the right ship");
    }

    [TestMethod]
    public void AtBrutalACenterShipShortOfTheFleetsAverageRangeComesInHeadOn()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Center, Center, Screen, Screen, Screen, Screen);
        typeof(Ship).GetProperty(nameof(Ship.WeaponsMaxRange))!.SetValue(FleetShips[1], 15_000f);
        AIFleet.AverageFleetAttributes = new ShipAI.TargetParameterTotals { MaxRange = 10_000f };
        AssertLessThan(FleetShips[0].WeaponsMaxRange, 10_000f, "setup: the first center ship's range is short of the average");
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "a center ship without long range, such as a former command ship,");
        AssertOnLeft(FleetShips[1], group, "the long range ship");
    }

    [TestMethod]
    public void TheShipsSentAtAnEarlierGroupAreNotFreeForTheNext()
    {
        UState.P.Difficulty = GameDifficulty.Hard;
        Vector2 far = GroupCenter + new Vector2(0f, -25_000f);
        RivalsAt(GroupCenter);
        RivalsAt(far);
        FleetOf(Screen, Left, Right, Left, Right, Screen);
        SeeTheGroups();
        Task.AO = GroupCenter;
        ThreatCluster near = GroupAsStrongAs(GroupCenter, 2.5f);
        ThreatCluster next = GroupAsStrongAs(far, 2.1f);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertOnLeft(FleetShips[1], near, "setup: the first group's left ship");
        AssertHeadOn(FleetShips[3], next.Position, "with three ships left free for a group as strong as 2.1, the next group's left ship");
    }

    // four ships, one busy, against a group as strong as three: the free three are short of half again its strength
    void AssertABusyShipIsNotFree(System.Action<TestShip> busy, string what)
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Left, Right, Screen, Screen);
        busy(FleetShips[3]);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, $"with {what}, the left ship");
        AssertHeadOn(FleetShips[1], group.Position, "and the right ship");
    }

    [TestMethod]
    public void AFleetWithoutHalfAgainTheGroupsStrengthInFreeShipsComesInHeadOn()
    {
        AssertABusyShipIsNotFree(s => s.InCombat = true, "its fourth ship already fighting");
    }

    [TestMethod]
    public void AShipOnALockedMoveIsNotFree()
    {
        AssertABusyShipIsNotFree(s => s.AI.OrderMoveTo(s.Position + new Vector2(0f, 20_000f), Vectors.Up), "its fourth ship on a locked move");
    }

    [TestMethod]
    public void ABombingShipIsNotFree()
    {
        AssertABusyShipIsNotFree(s => s.AI.ChangeAIState(AIState.Bombard), "its fourth ship bombing");
    }

    [TestMethod]
    public void AShipLandingTroopsIsNotFree()
    {
        AssertABusyShipIsNotFree(s => s.AI.ChangeAIState(AIState.AssaultPlanet), "its fourth ship landing troops");
    }

    [TestMethod]
    public void AnUnarmedShipComesInHeadOn()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, SideShips);
        FleetShips[0].Weapons.Clear();
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertHeadOn(FleetShips[0], group.Position, "an unarmed left ship");
        AssertOnRight(FleetShips[1], group, "the armed right ship");
    }

    [TestMethod]
    public void ThePlansAreDroppedWhenNoGroupIsLeftInTheArea()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");

        Task.AO = GroupCenter + new Vector2(0f, 200_000f);
        Assert.IsFalse(Attack(), "setup: no group is known in the area");

        AssertEqual(0, FlankPlanCount(), "the plans are dropped");
    }

    [TestMethod]
    public void AFlankShipFliesItsLegWithoutTurningToFight()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        TestShip flanker = FleetShips[1];
        Vector2 point = FlankPointOf(flanker, group, "setup: the left ship");

        bool warped = false;
        bool fought = false;
        float nearest = float.MaxValue;
        RunSimWhile((120, fatal: true), () => !warped || flanker.IsInWarp, body: () =>
        {
            warped |= flanker.IsInWarp;
            fought |= flanker.InCombat;
            nearest = System.Math.Min(nearest, flanker.Position.Distance(group.Position));
        });

        AssertLessThan(nearest, flanker.SensorRange, "setup: on the way it saw the group");
        Assert.IsFalse(fought, "the flanker flies its leg without turning to fight");
        AssertLessThan(flanker.Position.Distance(point), flanker.WarpOutDistance + 1000f, "it leaves warp short of its point by its warp out distance");
        AssertLessThan(Toward(group.Position, flanker.Position).X, -0.8f, "at the group's left");
        AssertGreaterThan(flanker.Position.Distance(group.Position), group.Radius + FlankGeometry.WarpExitRange, "out of the group's reach");
    }
}
