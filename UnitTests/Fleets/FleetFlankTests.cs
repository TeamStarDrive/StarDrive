using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;
using Round = Ship_Game.AI.ShipAI.FleetFlankRound;

namespace UnitTests.Fleets;

[TestClass]
public class FleetFlankTests : StarDriveTest
{
    const string FrigateName = "TEST_Spearhead mk1-a";
    const float RivalsApart = 1500f;
    const float FleetOut = 60_000f;
    const float SideReach = 7500f; // how far past a group's edge the fleet looks for planet guns on each side
    const string Screen = "ScreenShips", Center = "CenterShips", Left = "LeftShips", Right = "RightShips";

    // the fleet comes from the south: its left is -X, its right +X, and the group's rear -Y
    static readonly Vector2 ToLeft = new(-1f, 0f), ToRight = new(1f, 0f), ToRear = new(0f, -1f);

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

    // its guns hold their fire
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

    object FlankPlans() => typeof(Fleet).GetField("FlankPlans", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(AIFleet);

    int FlankPlanCount()
    {
        object plans = FlankPlans();
        return (int)plans!.GetType().GetProperty("Count")!.GetValue(plans)!;
    }

    // a row south of the group, out of its sensor range, the Left ships west of the fleet's center and the Right ships east, each in the named flank list
    void FleetOf(params string[] flanks)
    {
        FleetShips = new TestShip[flanks.Length];
        var offsets = new Vector2[flanks.Length];
        for (int i = 0; i < flanks.Length; ++i)
        {
            float sideways = flanks[i] == Left ? -3000f : flanks[i] == Right ? 3000f : 0f;
            offsets[i] = new Vector2((i - (flanks.Length - 1) * 0.5f) * 600f + sideways, 0f);
            FleetShips[i] = Quiet(SpawnShip(FrigateName, Attacker, GroupCenter + offsets[i] + new Vector2(0f, FleetOut), Vectors.Up));
        }
        AIFleet = Attacker.CreateFleet(1, null);
        AIFleet.AddShips(FleetShips);
        for (int i = 0; i < flanks.Length; ++i)
        {
            FleetShips[i].RelativeFleetOffset = offsets[i];
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

    bool SendFree(MilitaryTask task, Ship[] ships)
    {
        MethodInfo attack = typeof(Fleet).GetMethod("AttackEnemyStrengthClumpsInAO", BindingFlags.Instance | BindingFlags.NonPublic,
                                                    null, new[] { typeof(MilitaryTask), typeof(IReadOnlyList<Ship>) }, null);
        return (bool)attack!.Invoke(AIFleet, new object[] { task, ships })!;
    }

    bool Attack(params Ship[] ships) => AttackFor(Task, ships);

    bool AttackFor(MilitaryTask task, params Ship[] ships) => SendFree(task, ships.Length > 0 ? ships : FleetShips.Cast<Ship>().ToArray());

    bool AttackWithNoShipFree() => SendFree(Task, System.Array.Empty<Ship>());

    static Vector2 Toward(Vector2 from, Vector2 to) => from.DirectionToTarget(to);

    static Round RoundOf(Ship ship) => ship.AI.FleetRound;

    static void AssertHeadOn(TestShip ship, Vector2 groupAt, string what)
    {
        Assert.IsFalse(ship.AI.HasPriorityOrder, $"{what} comes in head-on, free to fight on the way");
        AssertLessThan(ship.AI.MovePosition.Distance(groupAt), 5000f, $"{what} heads for the group");
    }

    static void AssertStaysHeadOn(TestShip ship, Vector2 groupAt, string what)
    {
        AssertHeadOn(ship, groupAt, what);
        Assert.IsNull(RoundOf(ship), $"{what} goes round nowhere");
    }

    // sent head-on like every ship, to go round to its place once its target is in reach
    static void AssertGoesRoundTo(Vector2 place, TestShip ship, Vector2 groupAt, string what)
    {
        AssertHeadOn(ship, groupAt, what);
        Round round = RoundOf(ship);
        Assert.IsNotNull(round, $"{what} is to go round the group");
        Assert.AreEqual(Round.Stage.Waiting, round.At, $"{what} waits for its target to come in reach");
        AssertGreaterThan(round.Place.Dot(place), 0.99f, $"{what} goes round to its place");
    }

    [TestMethod]
    public void AtNormalNoShipGoesRound()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Normal, 2.5f, Left, Right, Screen, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        for (int i = 0; i < 3; ++i)
            AssertStaysHeadOn(FleetShips[i], group.Position, $"ship {i}");
    }

    [TestMethod]
    public void AtHardEveryShipComesInHeadOnAndTheLeftAndRightShipsAreToGoRoundToTheSides()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertStaysHeadOn(FleetShips[0], group.Position, "the screen ship");
        AssertGoesRoundTo(ToLeft, FleetShips[1], group.Position, "the left ship");
        AssertGoesRoundTo(ToRight, FleetShips[2], group.Position, "the right ship");
    }

    [TestMethod]
    public void AtHardTheLongRangeShipsStayHeadOnAndNoneGoesToTheRear()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 3.5f, Center, Left, Left, Left, Screen, Screen);
        typeof(Ship).GetProperty(nameof(Ship.WeaponsMaxRange))!.SetValue(FleetShips[0], 15_000f);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertStaysHeadOn(FleetShips[0], group.Position, "the long range ship");
        for (int i = 1; i <= 3; ++i)
            AssertGoesRoundTo(ToLeft, FleetShips[i], group.Position, $"left ship {i}");
    }

    [TestMethod]
    public void AtBrutalTwoSideShipsSendNoneToTheRear()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Left, Right, Screen, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertGoesRoundTo(ToLeft, FleetShips[0], group.Position, "the left ship");
        AssertGoesRoundTo(ToRight, FleetShips[1], group.Position, "the right ship");
    }

    [TestMethod]
    public void AtBrutalTheFastestThirdOfTheSideShipsGoesRoundToTheRear()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Left, Left, Right, Screen, Screen, Screen);
        FleetShips[1].MaxSTLSpeed = FleetShips[0].MaxSTLSpeed * 2f;
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertGoesRoundTo(ToRear, FleetShips[1], group.Position, "the fastest side ship");
        AssertGoesRoundTo(ToLeft, FleetShips[0], group.Position, "the other left ship");
        AssertGoesRoundTo(ToRight, FleetShips[2], group.Position, "the right ship");
    }

    [TestMethod]
    public void AtBrutalTheLongRangeShipsGoRoundToTheSideNearerThemAndTheCommandShipStaysHeadOn()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Center, Center, Center, Screen, Screen, Screen, Screen);
        AIFleet.SetCommandShip(FleetShips[0]);
        PropertyInfo weaponsRange = typeof(Ship).GetProperty(nameof(Ship.WeaponsMaxRange))!;
        weaponsRange.SetValue(FleetShips[1], 15_000f);
        weaponsRange.SetValue(FleetShips[2], 15_000f);
        FleetShips[2].Position = GroupCenter + new Vector2(1500f, FleetOut);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertStaysHeadOn(FleetShips[0], group.Position, "the command ship");
        AssertGoesRoundTo(ToLeft, FleetShips[1], group.Position, "the long range ship west of the fleet's line");
        AssertGoesRoundTo(ToRight, FleetShips[2], group.Position, "the long range ship east of it");
    }

    [TestMethod]
    public void ASideShipGoesRoundToTheSideNearerWhereItIs()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        FleetShips[1].Position = GroupCenter + new Vector2(8000f, FleetOut);
        FleetShips[2].Position = GroupCenter + new Vector2(-8000f, FleetOut);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertGoesRoundTo(ToRight, FleetShips[1], group.Position, "a left ship now east of the fleet's line");
        AssertGoesRoundTo(ToLeft, FleetShips[2], group.Position, "a right ship now west of it");
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
        return group.Position + new Vector2(sideX * (group.Radius + SideReach), 0f);
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

        AssertGoesRoundTo(ToRight, FleetShips[0], group.Position, "the left ship");
        AssertGoesRoundTo(ToRight, FleetShips[1], group.Position, "the right ship");
        AssertGreaterThan(RoundOf(FleetShips[1]).Plan.BarredSide.Dot(ToLeft), 0.99f, "and neither passes the left");
    }

    [TestMethod]
    public void AtInsaneTheRightSideInReachOfPlanetGunsJoinsTheLeft()
    {
        ThreatCluster group = GroupWithPlanetGunsOn(1f, GameDifficulty.Insane);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertGoesRoundTo(ToLeft, FleetShips[0], group.Position, "the left ship");
        AssertGoesRoundTo(ToLeft, FleetShips[1], group.Position, "the right ship");
    }

    [TestMethod]
    public void AtInsaneTheFleetsOwnPlanetsGunsBarNoSide()
    {
        ThreatCluster group = GroupWithPlanetGunsOn(-1f, GameDifficulty.Insane, Enemy);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertGoesRoundTo(ToLeft, FleetShips[0], group.Position, "the left ship, beside its own planet's guns,");
        Assert.AreEqual(Vector2.Zero, RoundOf(FleetShips[0]).Plan.BarredSide, "no side is barred");
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

        AssertGoesRoundTo(ToLeft, FleetShips[0], group.Position, "the left ship");
        Assert.AreEqual(Vector2.Zero, RoundOf(FleetShips[0]).Plan.BarredSide, "no side is barred");
    }

    [TestMethod]
    public void AtInsaneARearShipKeepsOffTheSideInReachOfPlanetGuns()
    {
        ThreatCluster group = GroupWithPlanetGunsOn(-1f, GameDifficulty.Insane, Left, Left, Right, Screen, Screen, Screen);
        TestShip rear = FleetShips[1];
        rear.MaxSTLSpeed = FleetShips[0].MaxSTLSpeed * 2f;
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertGoesRoundTo(ToRear, rear, group.Position, "the fastest side ship");
        AssertGreaterThan(RoundOf(rear).Plan.BarredSide.Dot(ToLeft), 0.99f, "it does not pass the left side on its way round");
    }

    [TestMethod]
    public void AtInsaneBothSidesInReachOfPlanetGunsAreKept()
    {
        Planet planet = PlanetWithGuns(new Vector2(600_000f, 300_000f));
        ThreatCluster group = GroupNearPlanetGuns(GameDifficulty.Insane, planet, new Vector2(0f, 5000f), SideShips);
        Assert.IsTrue(InReach(planet, NearestSidePoint(group, -1f)) && InReach(planet, NearestSidePoint(group, 1f)),
                      "setup: the guns reach both sides");
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertGoesRoundTo(ToLeft, FleetShips[0], group.Position, "the left ship");
        AssertGoesRoundTo(ToRight, FleetShips[1], group.Position, "the right ship");
        Assert.AreEqual(Vector2.Zero, RoundOf(FleetShips[0]).Plan.BarredSide, "no side is barred");
    }

    [TestMethod]
    public void OneShipSentAtAGroupGoesRoundNowhere()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 0.5f, SideShips);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertStaysHeadOn(FleetShips[0], group.Position, "a left ship sent alone");
    }

    [TestMethod]
    public void TheCommandShipStaysHeadOnFromAnyList()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, SideShips);
        AIFleet.SetCommandShip(FleetShips[0]);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertStaysHeadOn(FleetShips[0], group.Position, "the command ship, a left ship,");
        AssertGoesRoundTo(ToRight, FleetShips[1], group.Position, "the right ship");
    }

    [TestMethod]
    public void AtBrutalACenterShipShortOfTheFleetsAverageRangeStaysHeadOn()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Center, Center, Screen, Screen, Screen, Screen);
        typeof(Ship).GetProperty(nameof(Ship.WeaponsMaxRange))!.SetValue(FleetShips[1], 15_000f);
        AIFleet.AverageFleetAttributes = new ShipAI.TargetParameterTotals { MaxRange = 10_000f };
        AssertLessThan(FleetShips[0].WeaponsMaxRange, 10_000f, "setup: the first center ship's range is short of the average");
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertStaysHeadOn(FleetShips[0], group.Position, "a center ship without long range, such as a former command ship,");
        AssertGoesRoundTo(ToLeft, FleetShips[1], group.Position, "the long range ship");
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

        AssertGoesRoundTo(ToLeft, FleetShips[1], near.Position, "setup: the first group's left ship");
        AssertStaysHeadOn(FleetShips[3], next.Position, "with three ships left free for a group as strong as 2.1, the next group's left ship");
    }

    // four ships, one busy, against a group as strong as three: the free three are short of half again its strength
    void AssertABusyShipIsNotFree(System.Action<TestShip> busy, string what)
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Left, Right, Screen, Screen);
        busy(FleetShips[3]);
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertStaysHeadOn(FleetShips[0], group.Position, $"with {what}, the left ship");
        AssertStaysHeadOn(FleetShips[1], group.Position, "and the right ship");
    }

    [TestMethod]
    public void AFleetWithoutHalfAgainTheGroupsStrengthInFreeShipsSendsNoneRound()
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
    public void AnUnarmedShipGoesRoundNowhere()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, SideShips);
        FleetShips[0].Weapons.Clear();
        AssertTrue(Attack(), "setup: the fleet attacks");

        AssertStaysHeadOn(FleetShips[0], group.Position, "an unarmed left ship");
        AssertGoesRoundTo(ToRight, FleetShips[1], group.Position, "the armed right ship");
    }

    [TestMethod]
    public void AShipSentAgainAtTheSameGroupKeepsItsOneRound()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        Round round = RoundOf(FleetShips[1]);

        FreeTheShipsLeftOver();
        AssertTrue(Attack(), "setup: the fleet sends its free ships again while they are on their way");
        Assert.AreSame(round, RoundOf(FleetShips[1]), "the left ship keeps the round it was given");
        Assert.AreEqual(Round.Stage.Waiting, round.At, "still waiting for its target to come in reach");

        round.At = Round.Stage.Done;
        FreeTheShipsLeftOver();
        AssertTrue(Attack(), "setup: the fleet sends its free ships again once the round is over");
        Assert.AreSame(round, RoundOf(FleetShips[1]), "the left ship keeps that round");
        Assert.AreEqual(Round.Stage.Done, round.At, "and does not go round the same group twice");
        AssertHeadOn(FleetShips[1], group.Position, "it comes in head-on");
    }

    // ships left over are sent to the area on a locked move, which keeps their strength out of the next send's free strength, so it would not split
    void FreeTheShipsLeftOver()
    {
        foreach (int i in new[] { 3, 4 })
            FleetShips[i].AI.ClearOrders();
    }

    [TestMethod]
    public void AShipSentAtAGroupThatMovedOffGetsANewRound()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        Round first = RoundOf(FleetShips[1]);
        first.At = Round.Stage.Done;

        group.Position += new Vector2(20_000f, 0f);
        FreeTheShipsLeftOver();
        AssertTrue(Attack(), "setup: the fleet attacks the group where it went");

        Assert.IsTrue(first.Plan.Dropped, "the old plan's round is dropped");
        Round next = RoundOf(FleetShips[1]);
        Assert.IsTrue(next != null && next != first && next.At == Round.Stage.Waiting, "the left ship is to go round the group anew");
    }

    [TestMethod]
    public void APlanFollowsItsGroupAsItDrifts()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        Round round = RoundOf(FleetShips[1]);

        for (int i = 0; i < 2; ++i)
        {
            group.Position += new Vector2(6000f, 0f);
            Assert.IsFalse(AttackWithNoShipFree(), "setup: no ship is free");
        }

        AssertEqual(1, FlankPlanCount(), "the plan follows its group, twelve thousand off in two moves");
        Assert.IsFalse(round.Plan.Dropped, "and its rounds stand");
    }

    [TestMethod]
    public void APlanIsDroppedOnceItsGroupHasMovedOff()
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");
        Round round = RoundOf(FleetShips[1]);

        group.Position += new Vector2(20_000f, 0f);
        Assert.IsFalse(AttackWithNoShipFree(), "setup: no ship is free");

        AssertEqual(0, FlankPlanCount(), "the plan round where the group was is dropped");
        Assert.IsTrue(round.Plan.Dropped, "and so are its rounds");
    }

    [TestMethod]
    public void ANewTaskDropsTheOldPlans()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");
        Round round = RoundOf(FleetShips[1]);

        MilitaryTask next = NewTask();
        next.AO = GroupCenter;
        AssertTrue(AttackFor(next, FleetShips[0]), "setup: the fleet attacks for its next task with one ship");

        AssertEqual(0, FlankPlanCount(), "the old task's plans are dropped");
        Assert.IsTrue(round.Plan.Dropped, "and so are their rounds");
    }

    [TestMethod]
    public void AResetFleetDropsItsPlans()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");
        Round round = RoundOf(FleetShips[1]);

        AIFleet.Reset(fleeIfOutmatched: false);

        AssertEqual(0, FlankPlanCount(), "a reset fleet keeps no plans");
        Assert.IsTrue(round.Plan.Dropped, "nor rounds");
    }

    [TestMethod]
    public void ThePlansAreDroppedWhenNoGroupIsLeftInTheArea()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertEqual(1, FlankPlanCount(), "setup: the fleet plans a split");
        Round round = RoundOf(FleetShips[1]);

        Task.AO = GroupCenter + new Vector2(0f, 200_000f);
        Assert.IsFalse(Attack(), "setup: no group is known in the area");

        AssertEqual(0, FlankPlanCount(), "the plans are dropped");
        Assert.IsTrue(round.Plan.Dropped, "and so are their rounds");
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

    // the fleet attacks, and its ship with the given index is put sublight the given way off the group's center, facing it, while the others hold back
    TestShip SentRoundFrom(int index, Vector2 fromCenter)
    {
        AssertTrue(Attack(), "setup: the fleet attacks");
        for (int i = 0; i < FleetShips.Length; ++i)
            if (i != index)
                FleetShips[i].AI.ClearOrders();
        TestShip ship = FleetShips[index];
        ship.Position = GroupCenter + fromCenter;
        ship.Rotation = ship.Position.DirectionToTarget(GroupCenter).ToRadians();
        Assert.IsNotNull(RoundOf(ship), "setup: the ship is to go round the group");
        return ship;
    }

    // what the ship did while it went round the group
    sealed class Flight
    {
        public bool Flew;
        public float TargetAtStart;
        public float Radius;
        public float AngleAtStart;
        public float Wander; // the most it strayed from the round's distance
        public bool Warped;
        public bool LeftTheFight;
        public float West, East, South; // its furthest from the group's center on the way
        public float TopSpeed;
        public double Seconds;
        public float EndRadius; // its distance from the group's center where the round ends
    }

    // the test frigates are slow: half way round takes them over two minutes
    Flight FlyRound(TestShip ship, System.Action whileFlying = null, double timeout = 300)
    {
        Round round = RoundOf(ship);
        var flight = new Flight();
        RunSimWhile((timeout, fatal: true), () => round.At != Round.Stage.Done, () =>
        {
            if (round.At != Round.Stage.Flying)
                return;
            if (!flight.Flew)
            {
                flight.Flew = true;
                flight.TargetAtStart = ship.Position.Distance(ship.AI.Target.Position);
                flight.Radius = round.Radius;
                flight.AngleAtStart = round.AngleLeft;
            }
            else
            {
                flight.Seconds += TestSimStepD;
            }

            whileFlying?.Invoke();
            Vector2 offset = ship.Position - GroupCenter;
            flight.Wander = System.Math.Max(flight.Wander, System.Math.Abs(offset.Length() - round.Radius));
            flight.Warped |= ship.IsInWarp && flight.Seconds > 1.0;
            flight.LeftTheFight |= !ship.InCombat;
            flight.West = System.Math.Min(flight.West, offset.X);
            flight.East = System.Math.Max(flight.East, offset.X);
            flight.South = System.Math.Max(flight.South, offset.Y);
            if (flight.Seconds > 1.0) // once out of the warp it may have come in at
                flight.TopSpeed = System.Math.Max(flight.TopSpeed, ship.CurrentVelocity);
        });
        flight.EndRadius = ship.Position.Distance(GroupCenter);
        return flight;
    }

    static float ReachOf(Ship ship) => System.Math.Max(7500f, ship.DesiredCombatRange);

    [TestMethod]
    public void ASideShipGoesRoundToItsSideOnceItsTargetIsInReachFightingOnTheWay()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        TestShip ship = SentRoundFrom(1, new Vector2(-1000f, 12_000f));

        Flight flight = FlyRound(ship);

        Assert.IsTrue(flight.Flew, "the left ship goes round the group");
        AssertLessThan(flight.TargetAtStart, ReachOf(ship) + 100f, "once its target is in reach");
        Assert.IsFalse(flight.LeftTheFight, "fighting all the way");
        Assert.IsFalse(flight.Warped, "at sublight");
        AssertGreaterThan(flight.TopSpeed, ship.MaxSTLSpeed * 0.9f, "at its top speed");
        AssertLessThan(flight.Wander, flight.Radius * 0.15f, "keeping the distance it had when it started");
        AssertLessThan(flight.East, 1f, "the shorter way, never east of the group");
        AssertLessThan(Toward(GroupCenter, ship.Position).X, -0.95f, "and ends at the group's left");
        AssertGreaterThan(Toward(GroupCenter, ship.Position).Y, 0.05f, "as it reaches its place, not past it");
        AssertLessThan(System.Math.Abs(flight.EndRadius - flight.Radius), flight.Radius * 0.02f, "at the distance it started at");
    }

    [TestMethod]
    public void ARearShipGoesRoundTheNearerSideToTheRear()
    {
        FleetAgainstAGroup(GameDifficulty.Brutal, 2.5f, Left, Left, Right, Screen, Screen, Screen);
        FleetShips[1].MaxSTLSpeed = FleetShips[0].MaxSTLSpeed * 2f;
        TestShip ship = SentRoundFrom(1, new Vector2(-1000f, 9000f));
        AssertGreaterThan(RoundOf(ship).Place.Dot(ToRear), 0.99f, "setup: the fastest side ship goes to the rear");

        Flight flight = FlyRound(ship);

        Assert.IsTrue(flight.Flew, "the rear ship goes round the group");
        AssertLessThan(flight.West, -flight.Radius * 0.8f, "round the west side, nearer it");
        AssertLessThan(Toward(GroupCenter, ship.Position).Y, -0.95f, "to the group's rear");
    }

    [TestMethod]
    public void ARoundKeepsOffTheBarredSide()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        TestShip ship = SentRoundFrom(1, new Vector2(-1000f, 9000f));
        ship.AI.FleetRound = new Round(new Fleet.FlankPlan(AIFleet, GroupCenter, ToRear, barredSide: ToLeft), ToRear);

        Flight flight = FlyRound(ship);

        Assert.IsTrue(flight.Flew, "the ship goes round the group");
        AssertGreaterThan(flight.East, flight.Radius * 0.8f, "the long way, round the east side, the west being barred");
        AssertGreaterThan(flight.West, -flight.Radius * 0.3f, "never near the west side");
        AssertLessThan(Toward(GroupCenter, ship.Position).Y, -0.95f, "to the group's rear");
    }

    [TestMethod]
    public void AShipAlreadyAtItsSideGoesRoundNowhere()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        TestShip ship = SentRoundFrom(1, new Vector2(-7000f, 500f));

        Flight flight = FlyRound(ship);

        Assert.IsFalse(flight.Flew, "the left ship, at the group's left when its target comes in reach, fights where it is");
    }

    [TestMethod]
    public void AShipThatCannotGetRoundGivesUpAfterHalfAgainTheTimeItShouldTake()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        TestShip ship = SentRoundFrom(1, new Vector2(-1000f, 9000f));
        Vector2 held = default;
        float speed = 0f;

        Flight flight = FlyRound(ship, whileFlying: () =>
        {
            if (held == default)
            {
                held = ship.Position;
                speed = ship.MaxSTLSpeed;
            }
            ship.Position = held;
            ship.Velocity = Vector2.Zero;
        });

        Assert.IsTrue(flight.Flew, "setup: the left ship sets off round the group");
        double expected = 1.5 * flight.Radius * flight.AngleAtStart / speed;
        AssertEqual(0.2f, (float)expected, (float)flight.Seconds, "held back, it gives up after half again the time the round should take");
    }

    // the ship set off round the group
    TestShip FlankerOnItsWay()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        TestShip ship = SentRoundFrom(1, new Vector2(-1000f, 9000f));
        RunSimWhile((60, fatal: true), () => RoundOf(ship).At == Round.Stage.Waiting);
        Assert.AreEqual(Round.Stage.Flying, RoundOf(ship).At, "setup: the left ship sets off round the group");
        return ship;
    }

    [TestMethod]
    public void ARoundStopsWhenItsPlanIsDropped()
    {
        TestShip ship = FlankerOnItsWay();
        Round round = RoundOf(ship);

        KnownGroupAt(GroupCenter).Position += new Vector2(20_000f, 0f);
        Assert.IsFalse(AttackWithNoShipFree(), "setup: the fleet finds its group gone from where it was planned");
        RunObjectsSim(TestSimStep);

        Assert.IsTrue(round.Plan.Dropped, "setup: the plan is dropped");
        Assert.IsNull(RoundOf(ship), "the ship gives up its round");
        Assert.IsTrue(ship.InCombat, "and fights on");
    }

    [TestMethod]
    public void ARoundStopsWhenTheShipSeesNoEnemyNearTheGroup()
    {
        TestShip ship = FlankerOnItsWay();

        foreach (TestShip rival in GroupShips)
            if (rival.Loyalty == Player)
                rival.Position += new Vector2(80_000f, 0f);
        RunObjectsSim(TestSimStep);

        Assert.AreEqual(Round.Stage.Done, RoundOf(ship).At, "with the group gone from where it last saw it, the ship stops going round");
        Assert.IsTrue(ship.InCombat, "while still fighting");
    }

    [TestMethod]
    public void FightersLeftNearTheGroupDoNotKeepARoundGoing()
    {
        TestShip ship = FlankerOnItsWay();
        TestShip[] rivals = GroupShips.Where(s => s.Loyalty == Player).ToArray();
        TestShip fighter = rivals[1];
        fighter.Mothership = rivals[0];
        foreach (TestShip rival in rivals)
            if (rival != fighter)
                rival.Position += new Vector2(80_000f, 0f);
        ship.AI.Update(TestSimStep);

        Assert.AreEqual(Round.Stage.Done, RoundOf(ship).At, "with only a fighter left where the group was, the ship stops going round");
        Assert.IsTrue(ship.InCombat, "while still fighting");
    }

    [TestMethod]
    public void ARoundStopsWhenTheGroupWarpsAway()
    {
        TestShip ship = FlankerOnItsWay();
        TestShip straggler = Quiet(SpawnShip(FrigateName, Player, GroupCenter + new Vector2(20_000f, 0f)));
        straggler.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        RunObjectsSim(1.5f);
        Assert.AreEqual(Round.Stage.Flying, RoundOf(ship).At, "setup: the ship is still on its way round");

        foreach (TestShip rival in GroupShips) // the ship's own update next, before theirs takes them out of warp again
            if (rival.Loyalty == Player)
                rival.engineState = Ship.MoveState.Warp;
        ship.AI.Update(TestSimStep);

        Assert.AreEqual(Round.Stage.Done, RoundOf(ship).At, "with the group gone to warp, the ship stops going round");
        Assert.IsTrue(ship.InCombat && ship.AI.Target == straggler, "and fights the ship still there");
    }

    [TestMethod]
    public void AShipThatLeavesItsFleetForgetsItsRound()
    {
        TestShip ship = FlankerOnItsWay();

        AIFleet.RemoveShip(ship, clearOrders: false);
        RunObjectsSim(TestSimStep);

        Assert.IsNull(RoundOf(ship), "a ship out of the fleet no longer goes round for it");
        Assert.IsTrue(ship.InCombat, "and fights on");
    }

    [TestMethod]
    public void ARoundWaitsForATargetOfThePlannedGroup()
    {
        FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        var picketOffset = new Vector2(-14_000f, 0f);
        TestShip ship = SentRoundFrom(1, picketOffset + new Vector2(-2000f, 5000f));
        TestShip lone = Quiet(SpawnShip(FrigateName, Player, GroupCenter + picketOffset));
        lone.AI.OrderHoldPosition(MoveOrder.HoldPosition);

        RunSimWhile((20, fatal: true), () => ship.AI.Target != lone || !ship.InCombat);
        RunObjectsSim(3f);

        Assert.IsTrue(ship.AI.Target == lone && ship.Position.InRadius(lone.Position, ReachOf(ship)), "setup: a picket on the way is in reach");
        Assert.IsTrue(ship.AI.PotentialTargets.Any(t => t != lone && t.Position.InRadius(GroupCenter, 2000f)), "setup: the ship sees the planned group too");
        Assert.AreEqual(Round.Stage.Waiting, RoundOf(ship).At, "the ship goes round only the group it was sent at, and keeps its round for it");
    }

    [TestMethod]
    public void AShipSentAtAnotherGroupDropsItsRoundForTheFirst()
    {
        UState.P.Difficulty = GameDifficulty.Hard;
        Vector2 far = GroupCenter + new Vector2(0f, -25_000f);
        RivalsAt(GroupCenter);
        RivalsAt(far);
        FleetOf(Screen, Left, Right, Screen, Screen);
        SeeTheGroups();
        Task.AO = GroupCenter;
        ThreatCluster near = GroupAsStrongAs(GroupCenter, 2.5f);
        ThreatCluster farGroup = GroupAsStrongAs(far, 2.5f);
        AssertTrue(Attack(), "setup: the fleet attacks");
        AssertGoesRoundTo(ToLeft, FleetShips[1], near.Position, "setup: the left ship");

        near.Strength = 0.5f * FleetShips[0].GetStrength() / Attacker.GetFleetStrEmpireMultiplier(near.Loyalty);
        AssertTrue(Attack(FleetShips[0], FleetShips[1]), "setup: the screen ship alone now covers the near group, and the left ship is sent on alone");

        AssertHeadOn(FleetShips[1], farGroup.Position, "setup: the left ship");
        Assert.IsNull(RoundOf(FleetShips[1]), "sent at another group, it drops its round for the first");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AShipLeftOverDropsItsRound(bool withRearShip)
    {
        ThreatCluster group = FleetAgainstAGroup(GameDifficulty.Hard, 2.5f, Screen, Left, Right, Screen, Screen);
        AssertTrue(Attack(), "setup: the fleet attacks");
        Assert.IsNotNull(RoundOf(FleetShips[1]), "setup: the left ship is to go round the group");
        TestShip rear = withRearShip ? RearShipOfTheFleet() : null;

        group.Strength = 0.5f * FleetShips[0].GetStrength() / Attacker.GetFleetStrEmpireMultiplier(group.Loyalty);
        AssertTrue(Attack(FleetShips[0], FleetShips[1]), "setup: the screen ship alone now covers the group");

        if (withRearShip)
            AssertEqual(rear, FleetShips[1].AI.EscortTarget, "setup: the left ship, left over, escorts the rear ship");
        else
            Assert.IsTrue(FleetShips[1].AI.HasPriorityOrder, "setup: the left ship, left over, moves to the area");
        Assert.IsNull(RoundOf(FleetShips[1]), "and drops its round");
    }

    [TestMethod]
    public void KnockedOutEnginesEndARound()
    {
        TestShip ship = FlankerOnItsWay();

        ship.EnginesKnockedOut = true;
        ship.AI.Update(TestSimStep);

        Assert.AreEqual(Round.Stage.Done, RoundOf(ship).At, "a ship that cannot fly round fights where it is");
        Assert.IsTrue(ship.InCombat, "and fights on");
    }

    [TestMethod]
    public void ARoundTheFightEndsIsOver()
    {
        TestShip ship = FlankerOnItsWay();

        ship.AI.ExitCombatState();

        Assert.AreEqual(Round.Stage.Done, RoundOf(ship).At, "a ship whose fight ends does not take up its round again");
    }
}
