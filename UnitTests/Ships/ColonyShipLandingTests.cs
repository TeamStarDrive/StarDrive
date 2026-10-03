using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A colony ship lands on the planet it settled. The colony exists from the start of the landing,
/// so the ship is out of play while it descends and is removed once it touches down.
/// </summary>
[TestClass]
public class ColonyShipLandingTests : StarDriveTest
{
    readonly Planet Target;
    readonly Planet Other;
    TestShip ColonyShip;

    public ColonyShipLandingTests()
    {
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Target = AddDummyPlanet(new Vector2(200_000), 1f, 1f, 4f, new Vector2(205_000), explored: true);
        Other = AddDummyPlanet(new Vector2(300_000), 1f, 1f, 4f, new Vector2(305_000), explored: true);
    }

    MarkForColonization Colonize(Fleet fleet = null)
    {
        ColonyShip = SpawnShip("Colony Ship", Player, Target.Position);
        fleet?.AddShips(new Ship[] { ColonyShip });
        var colonization = new MarkForColonization(ColonyShip, Target, Player);
        RunSimWhile((simTimeout: 60, fatal: true), () => Target.Owner == null);
        AssertEqual(Player, Target.Owner, "setup: the colony ship must settle the planet");
        return colonization;
    }

    [TestMethod]
    public void AColonyShipLandsOnThePlanetItSettled()
    {
        Colonize();
        Assert.IsTrue(ColonyShip.Active, "the colony ship must stay in space while it lands, not vanish");
        Assert.IsTrue(ColonyShip.IsLanding, "the colony ship must land once the planet is settled");

        double landing = RunSimWhile((simTimeout: 60, fatal: true), () => ColonyShip.Active);
        AssertGreaterThan(landing, 1.0, "the landing takes a few seconds");
        Assert.IsFalse(ColonyShip.Dying, "a landed ship is removed, not destroyed");
    }

    [TestMethod]
    public void AColonyShipDoesNotTurnInPlaceBeforeItLands()
    {
        ColonyShip = SpawnShip("Colony Ship", Player, Target.Position + new Vector2(5000, 0));
        ColonyShip.AI.OrderColonization(Target);

        Vector2 stoppedFacing = Vector2.Zero;
        float turnWhileStopped = 0f;
        RunSimWhile((simTimeout: 120, fatal: true), () => !ColonyShip.IsLanding, () =>
        {
            bool stoppedOverPlanet = ColonyShip.CurrentVelocity < 1f
                                  && ColonyShip.Position.InRadius(Target.Position, Target.Radius + 200f);
            if (stoppedOverPlanet && stoppedFacing == Vector2.Zero)
                stoppedFacing = ColonyShip.Direction;
            if (stoppedFacing != Vector2.Zero)
                turnWhileStopped = Math.Max(turnWhileStopped, ColonyShip.Direction.Distance(stoppedFacing));
        });

        Assert.AreNotEqual(Vector2.Zero, stoppedFacing, "setup: the colony ship must stop over the planet");
        AssertLessThan(turnWhileStopped, 0.05f, "the ship must land facing the way it flew in, not turn in place first");
    }

    [TestMethod]
    public void ALandingColonyShipIsOutOfPlay()
    {
        Fleet fleet = Player.CreateFleet(1, null);
        MarkForColonization colonization = Colonize(fleet);

        Assert.IsFalse(Enemy.IsEmpireAttackable(Player, ColonyShip), "enemies must not target a landing ship");
        Assert.IsFalse(ColonyShip.CanBeScrapped, "a landing ship cannot be scrapped or refitted");
        Assert.IsNull(new MarkForColonization(Other, Player, isManual: true).FinishedShip,
                      "a landing ship is not free to settle another planet");

        AssertEqual(GoalStep.GoalComplete, colonization.Evaluate(), "setup: the colonization is done");
        AssertEqual(AIState.AwaitingOrders, ColonyShip.AI.State,
                    "the finished colonization must not send the landing ship to orbit");

        fleet.Update(TestSimStep);
        Assert.IsNull(ColonyShip.Fleet, "a landing ship leaves its fleet");
    }

    [TestMethod]
    public void ALandingColonyShipCannotBeSelected()
    {
        Colonize();
        Ship scout = SpawnShip("Vulcan Scout", Player, ColonyShip.Position + new Vector2(300, 0));
        RunObjectsSim(TestSimStep);

        Universe.SetViewPerspective(Matrices.CreateLookAtDown(ColonyShip.Position.X, ColonyShip.Position.Y, -5000),
                                    maxDistance: 1_000_000);
        Ship[] selectable = Universe.GetVisibleShipsInScreenRect(new RectF(0, 0, Universe.ScreenArea.X, Universe.ScreenArea.Y));
        Assert.IsTrue(selectable.Contains(scout), "setup: the box must select the scout next to the landing ship");
        Assert.IsFalse(selectable.Contains(ColonyShip), "a landing ship cannot be selected or clicked on");
    }

    [TestMethod]
    public void TroopsOrderedAboardALandingColonyShipStayOnTheirShip()
    {
        Colonize();
        Ship boarder = SpawnShip("Vulcan Scout", Enemy, ColonyShip.Position + new Vector2(0, 100));
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(boarder);
        boarder.AI.OrderTroopToBoardShip(ColonyShip);

        RunObjectsSim(1f);
        AssertEqual(1, boarder.TroopCount, "a boarding order must not put troops on a landing ship");
        AssertEqual(Player, ColonyShip.Loyalty, "a landing ship cannot be captured");
    }

    [TestMethod]
    public void ALandingCarriesOnAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Colonize();

        SavedGame save = Universe.Save("UnitTest.ColonyShipLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(ColonyShip.Id);
        Assert.IsTrue(landing is { Active: true, IsLanding: true },
                      "a spent colony ship must still be landing after a load, not back in play");

        for (double time = 0; landing.Active; time += TestSimStepD)
        {
            AssertLessThan(time, 60.0, "the loaded landing must finish");
            loaded.UState.Objects.Update(TestSimStep);
        }
    }

    [TestMethod]
    public unsafe void ShotsBeamsAndBlastsDoNotHurtALandingColonyShip()
    {
        Colonize();
        float health = ColonyShip.Health;
        Ship attacker = SpawnShip("Vulcan Scout", Enemy, ColonyShip.Position + new Vector2(0, 400));

        Projectile shell = Projectile.Create(attacker.Weapons[0], attacker, ColonyShip.Position, Vectors.Up, null, playSound: false);
        Weapon laser = ResourceManager.CreateWeapon(UState, "LaserBeam", attacker, null);
        var beam = new Beam(UState.CreateId(), laser, attacker.Position, ColonyShip.Position - new Vector2(0, 400));

        var objects = new SpatialObjectBase[] { shell, beam, ColonyShip };
        CollisionPair* pairs = stackalloc CollisionPair[2];
        pairs[0] = new CollisionPair(0, 2);
        pairs[1] = new CollisionPair(1, 2);
        NarrowPhase.Collide(TestSimStep, pairs, 2, objects);
        AssertEqual(health, ColonyShip.Health, "shots and beams must pass through a landing ship");

        for (int i = 0; i < 20; ++i)
            UState.Spatial.ShipExplode(attacker, 10_000f, ColonyShip.Position, 500f);
        AssertEqual(health, ColonyShip.Health, "a death blast next to a landing ship must not reach it");
        Assert.IsFalse(ColonyShip.Dying, "a landing ship cannot be destroyed");
    }
}
