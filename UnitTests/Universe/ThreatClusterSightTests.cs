using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Universe;

// what an empire keeps of a rival group it sees in part, or not at all, while it looks at the group's spot
[TestClass]
public class ThreatClusterSightTests : StarDriveTest
{
    const string Platform = "Platform Base mk1-a";
    const string Scout = "TEST_Vulcan Scout";
    const string UnarmedScout = "Unarmed Scout";

    // the Player's eyes, a platform that cannot fly off
    TestShip Eyes;

    public ThreatClusterSightTests()
    {
        LoadStarterShips(Platform, Scout, UnarmedScout);
        CreateUniverseAndPlayerEmpire();
        Eyes = SpawnShipNoCombatHoldPos(Platform, Player, new(100_000, 100_000));
        AssertEqual(24_000f, Eyes.AI.GetSensorRadius(), "test setup: the eyes' sensor range");
    }

    TestShip EnemyShip(string design, Vector2 at) => SpawnShipNoCombatHoldPos(design, Enemy, at);

    static void MoveTo(TestShip ship, Vector2 at)
    {
        ship.Position = at;
        ship.AI.OrderHoldPosition(MoveOrder.StandGround|MoveOrder.HoldPosition);
    }

    // one update: the empire's own threat update reads what its sensors saw in the update before
    void Step(Vector2 eyesAt)
    {
        MoveTo(Eyes, eyesAt);
        UState.Objects.Update(new(time: 2f));
    }

    void LookFrom(Vector2 at)
    {
        Step(at);
        Step(at);
    }

    float Known() => KnownAt(new(100_000, 100_000), 20_000);
    float KnownAt(Vector2 at, float radius) => Player.Threats.GetHostileStrengthAt(at, radius);

    // both 3,000 from the spot at 100,000 / 100,000
    void LookAtAll() => LookFrom(new(100_000, 100_000));
    // the west one at 22,000, the east one at 28,000
    void LookAtTheWestOne() => LookFrom(new(75_000, 100_000));
    // the group's center 25,500 away, inside what counts as scanned, both ships beyond the sensors
    void LookAtTheCenterOnly() => LookFrom(new(100_000, 125_500));

    [TestMethod]
    public void APlatformOutOfViewStaysInItsGroupWhileAnotherOfItsShipsIsSeen()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        LookAtAll();
        AssertEqual(west.GetStrength() + east.GetStrength(), Known(), "both platforms seen");

        LookAtTheWestOne();
        AssertEqual(west.GetStrength() + east.GetStrength(), Known(), "the platform out of view cannot have moved off");
        AssertEqual(1, Player.Threats.RivalClusters.Length, "it stays in the group of the platform seen 6,000 from it");
    }

    [TestMethod]
    public void AShipOutOfViewIsDroppedWhenItsGroupIsSeenInPart()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Scout, new(103_000, 100_000));
        LookAtAll();
        AssertEqual(west.GetStrength() + east.GetStrength(), Known(), "the platform and the scout seen");

        LookAtTheWestOne();
        AssertEqual(west.GetStrength(), Known(), "a ship out of view may have flown off");
    }

    [TestMethod]
    public void ADestroyedPlatformOutOfViewIsDropped()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        LookAtAll();

        east.InstantKill();
        LookAtTheWestOne();
        AssertEqual(west.GetStrength(), Known());
    }

    [TestMethod]
    public void PlatformsStayWhenTheirGroupsCenterIsScannedWithoutSeeingThem()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        LookAtAll();

        LookAtTheCenterOnly();
        AssertEqual(west.GetStrength() + east.GetStrength(), Known());
    }

    [TestMethod]
    public void AGroupOfOnlyPlatformsOutOfViewCountsDownAsUnseen()
    {
        EnemyShip(Platform, new(97_000, 100_000));
        EnemyShip(Platform, new(103_000, 100_000));
        LookAtAll();
        AssertEqual(60f, Player.Threats.RivalClusters[0].TimeToLive, "test setup: seen in deep space");

        LookAtTheCenterOnly();
        Step(new(100_000, 125_500));
        AssertEqual(58f, Player.Threats.RivalClusters[0].TimeToLive, "two threat updates without seeing them");
    }

    [TestMethod]
    public void AGroupScannedWithoutSeeingItsShipsKeepsOnlyItsPlatformsAndTheirPlace()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        EnemyShip(Scout, new(109_000, 100_000));
        LookAtAll();

        LookFrom(new(102_500, 125_500)); // the group's center 25,500 away, all three beyond the sensors
        AssertEqual(west.GetStrength() + east.GetStrength(), Known(), "the scout may have flown off");
        AssertEqual(0f, KnownAt(new(109_000, 100_000), 1_500), "the group no longer reaches where the scout was");
    }

    [TestMethod]
    public void AGroupNobodyLooksAtIsRememberedWhole()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip scout = EnemyShip(Scout, new(103_000, 100_000));
        LookAtAll();

        LookFrom(new(300_000, 300_000));
        AssertEqual(west.GetStrength() + scout.GetStrength(), Known());
    }

    [TestMethod]
    public void AShipOutOfViewIsDroppedWhenItsGroupsCenterIsScanned()
    {
        EnemyShip(Scout, new(97_000, 100_000));
        EnemyShip(Scout, new(103_000, 100_000));
        LookAtAll();

        LookAtTheCenterOnly();
        AssertEqual(0f, Known());
        AssertEqual(0, Player.Threats.RivalClusters.Length);
    }

    [TestMethod]
    public void PlatformsOutOfViewStayWhereTheyAreWhenTheShipsOfTheirGroupFlyOff()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        TestShip scout = EnemyShip(Scout, new(109_000, 100_000));
        LookFrom(new(103_000, 100_000));
        AssertEqual(1, Player.Threats.RivalClusters.Length, "test setup: the three are seen as one group");

        LookFrom(new(129_000, 100_000)); // only the scout in view, the east platform 6,000 from it, the west one 12,000
        AssertEqual(east.GetStrength() + scout.GetStrength(), KnownAt(new(103_000, 100_000), 1_500), "the east platform stays in the scout's group, where it is");
        AssertEqual(west.GetStrength(), KnownAt(new(97_000, 100_000), 1_500), "the west one is left where it was seen");

        MoveTo(scout, new(117_000, 100_000));
        LookFrom(new(137_000, 100_000)); // the scout 14,000 from the east platform now
        AssertEqual(east.GetStrength(), KnownAt(new(103_000, 100_000), 1_500), "the east platform is left where it was seen");
        AssertEqual(west.GetStrength(), KnownAt(new(97_000, 100_000), 1_500));
        AssertEqual(scout.GetStrength(), KnownAt(scout.Position, 5_000), "the scout's group no longer holds them");

        LookFrom(new(103_000, 100_000)); // all three seen again
        AssertEqual(west.GetStrength() + east.GetStrength(), KnownAt(new(100_000, 100_000), 4_000), "the platforms seen again are one group");
        AssertEqual(west.GetStrength() + east.GetStrength() + scout.GetStrength(), Known(), "and nothing counts twice");
    }

    [TestMethod]
    public void PlatformsLeftBehindStayWhereTheyAreInGroupsOfThoseNearEachOther()
    {
        TestShip farWest = EnemyShip(Platform, new(87_000, 100_000));
        TestShip west = EnemyShip(Platform, new(90_000, 100_000));
        TestShip east = EnemyShip(Platform, new(110_000, 100_000));
        TestShip scout1 = EnemyShip(Scout, new(96_000, 100_000));
        TestShip scout2 = EnemyShip(Scout, new(104_000, 100_000));
        LookAtAll();
        LookFrom(new(100_000, 123_000)); // only the scouts in view, each platform within 9,000 of one
        AssertEqual(1, Player.Threats.RivalClusters.Length, "test setup: the five are one group");

        MoveTo(scout1, new(99_000, 108_000));
        MoveTo(scout2, new(101_000, 108_000));
        Step(new(100_000, 123_000)); // the platforms now over 12,000 from the scouts, the west two 20,000 from the east one
        AssertEqual(farWest.GetStrength() + west.GetStrength(), KnownAt(new(88_500, 100_000), 3_000), "the west two are left together where they were seen");
        AssertEqual(east.GetStrength(), KnownAt(east.Position, 1_500), "the east one on its own");
        AssertEqual(0f, KnownAt(new(100_000, 100_000), 1_500), "no group stretches across the space between them");
        AssertEqual(3, Player.Threats.RivalClusters.Length, "the scouts' group and two of platforms");
    }

    [TestMethod]
    public void EachPlatformOfAGroupSeenWholeCountsOnce()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        LookAtAll();
        LookAtAll();
        AssertEqual(west.GetStrength() + east.GetStrength(), Known());
    }

    [TestMethod]
    public void AGroupLeftWithOnlyAShipOfNoStrengthDoesNotTakeArmedShipsWithIt()
    {
        TestShip unarmed = EnemyShip(UnarmedScout, new(97_000, 100_000));
        TestShip armed = EnemyShip(Scout, new(103_000, 100_000));
        AssertEqual(0f, unarmed.GetStrength(), "test setup: the unarmed scout has no strength");
        LookAtAll();

        Step(new(75_000, 100_000)); // the next threat update sees only the unarmed scout
        Step(new(100_000, 100_000));
        Step(new(100_000, 100_000)); // the armed scout seen with it again
        AssertEqual(armed.GetStrength(), Known());
    }

    [TestMethod]
    public void AGroupOfShipsWithNoStrengthIsDropped()
    {
        EnemyShip(UnarmedScout, new(97_000, 100_000));
        LookAtAll();
        AssertEqual(0, Player.Threats.RivalClusters.Length);
    }

    [TestMethod]
    public void OurGroupStartedByAShipOfNoStrengthKeepsOurArmedShips()
    {
        SpawnShipNoCombatHoldPos(UnarmedScout, Player, new(300_000, 300_000));
        TestShip armed = SpawnShipNoCombatHoldPos(Scout, Player, new(302_000, 300_000));
        LookAtAll();
        AssertEqual(armed.GetStrength(), Player.Threats.GetStrengthAt(Player, new(300_000, 300_000), 5_000));
    }

    // an Enemy group seen this pass through its first ship, holding these ships from before
    ThreatCluster GroupSeenWith(Ship first, params Ship[] heldBefore)
    {
        var group = new ThreatCluster(Enemy, first) { Ships = heldBefore };
        group.Update.ResetForObservation(isOwnerCluster: false);
        group.Update.AddShip(first);
        return group;
    }

    float StrengthAfterUpdate(ThreatCluster group, params Ship[] seenAnywhere)
    {
        group.Update.KeepUnseenStatics(new HashSet<Ship>(seenAnywhere));
        group.Update.Update(new(1f), Player);
        return group.Strength;
    }

    [TestMethod]
    public void APlatformOutOfViewInAGroupMergedIntoAnotherGoesWithIt()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        TestShip scout = EnemyShip(Scout, new(110_000, 100_000));
        ThreatCluster merged = GroupSeenWith(west, west, east);
        ThreatCluster root = GroupSeenWith(scout, scout);
        root.Update.Merge(merged.Update);

        AssertEqual(0.01f, west.GetStrength() + east.GetStrength() + scout.GetStrength(), StrengthAfterUpdate(root, west, scout),
                    "both platforms and the scout");
    }

    [TestMethod]
    public void APlatformOutOfViewGoesWithAGroupMergedTwice()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        TestShip scout = EnemyShip(Scout, new(110_000, 100_000));
        TestShip other = EnemyShip(Scout, new(115_000, 100_000));
        ThreatCluster first = GroupSeenWith(west, west, east);
        ThreatCluster second = GroupSeenWith(scout, scout);
        ThreatCluster root = GroupSeenWith(other, other);
        second.Update.Merge(first.Update);
        root.Update.Merge(second.Update);

        AssertEqual(0.01f, west.GetStrength() + east.GetStrength() + scout.GetStrength() + other.GetStrength(),
                    StrengthAfterUpdate(root, west, scout, other), "both platforms and both scouts");
    }

    [TestMethod]
    public void APlatformOutOfViewHeldTwiceCountsOnce()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        TestShip scout = EnemyShip(Scout, new(110_000, 100_000));
        ThreatCluster merged = GroupSeenWith(west, west, east);
        ThreatCluster root = GroupSeenWith(scout, scout, east);
        root.Update.Merge(merged.Update);

        AssertEqual(0.01f, west.GetStrength() + east.GetStrength() + scout.GetStrength(), StrengthAfterUpdate(root, west, scout),
                    "each platform once, and the scout");
    }

    [TestMethod]
    public void APlatformOutOfViewNoLongerOfTheGroupsEmpireIsDropped()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip ours = SpawnShipNoCombatHoldPos(Platform, Player, new(103_000, 100_000));
        ThreatCluster group = GroupSeenWith(west, west, ours);

        AssertEqual(west.GetStrength(), StrengthAfterUpdate(group, west));
    }

    [TestMethod]
    public void AGroupWhoseMemoryRanOutIsKeptWhenPartOfItIsSeenAgain()
    {
        TestShip west = EnemyShip(Platform, new(97_000, 100_000));
        TestShip east = EnemyShip(Platform, new(103_000, 100_000));
        ThreatCluster seen = GroupSeenWith(west, west, east);
        seen.TimeToLive = -1f;
        Assert.IsFalse(seen.Update.ShouldBeRemoved, "part of it is seen on the pass its memory ran out");
        AssertEqual(west.GetStrength() + east.GetStrength(), StrengthAfterUpdate(seen, west), "with its platform out of view");

        var unseen = new ThreatCluster(Enemy, east) { TimeToLive = -1f };
        unseen.Update.ResetForObservation(isOwnerCluster: false);
        Assert.IsTrue(unseen.Update.ShouldBeRemoved, "a group with nothing of it seen still runs out");
    }
}
