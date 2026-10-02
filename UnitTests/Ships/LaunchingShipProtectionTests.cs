using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A launching ship is out of reach as a landing ship is: no shot, beam, death blast, splash or star radiation
/// hurts it, and planet defenses do not pick it as a target. Each test checks the same hit on a ship that is not
/// launching, so a setup that hurts nothing cannot pass.
/// </summary>
[TestClass]
public class LaunchingShipProtectionTests : StarDriveTest
{
    public LaunchingShipProtectionTests()
    {
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
    }

    TestShip Launching(Empire owner, Vector2 position)
    {
        TestShip ship = SpawnShip("Vulcan Scout", owner, position);
        ship.InitLaunch(LaunchPlan.Planet, 0f);
        Assert.IsTrue(ship.IsLaunching, "setup: the ship must be launching");
        return ship;
    }

    unsafe void ShootAndBeam(Ship attacker, Ship target)
    {
        Projectile shell = Projectile.Create(attacker.Weapons[0], attacker, target.Position, Vectors.Up, null, playSound: false);
        Weapon laser = ResourceManager.CreateWeapon(UState, "LaserBeam", attacker, null);
        var beam = new Beam(UState.CreateId(), laser, attacker.Position, target.Position - new Vector2(0, 400));

        var objects = new SpatialObjectBase[] { shell, beam, target };
        CollisionPair* pairs = stackalloc CollisionPair[2];
        pairs[0] = new CollisionPair(0, 2);
        pairs[1] = new CollisionPair(1, 2);
        NarrowPhase.Collide(TestSimStep, pairs, 2, objects);
    }

    [TestMethod]
    public void ShotsAndBeamsPassThroughALaunchingShip()
    {
        Ship launching = Launching(Player, new Vector2(300_000, 0));
        Ship inPlay = SpawnShip("Vulcan Scout", Player, new Vector2(305_000, 0));
        float health = launching.Health;

        ShootAndBeam(SpawnShip("Vulcan Scout", Enemy, launching.Position + new Vector2(0, 400)), launching);
        ShootAndBeam(SpawnShip("Vulcan Scout", Enemy, inPlay.Position + new Vector2(0, 400)), inPlay);

        AssertLessThan(inPlay.Health, inPlay.HealthMax, "setup: the same shot and beam hurt a ship that is not launching");
        AssertEqual(health, launching.Health, "shots and beams pass through a launching ship");
    }

    float HealthAfterDeathBlastsNextTo(Ship target, out float before)
    {
        Ship dying = SpawnShip("Vulcan Scout", Enemy, target.Position + new Vector2(0, 300));
        RunObjectsSim(TestSimStep);
        before = target.Health;
        for (int i = 0; i < 100; ++i)
            UState.Spatial.ShipExplode(dying, 10_000f, target.Position, 500f);
        return target.Health;
    }

    float HealthAfterSpaceNukeNextTo(Ship target, out float before)
    {
        Ship attacker = SpawnShip("Vulcan Scout", Enemy, target.Position + new Vector2(0, 300));
        RunObjectsSim(TestSimStep);
        Projectile nuke = Projectile.Create(attacker.Weapons[0], attacker, target.Position, Vectors.Up, null, playSound: false);
        nuke.DamageRadius = 1000f;
        nuke.DamageAmount = 10_000f;
        before = target.Health;
        UState.Spatial.ProjectileExplode(nuke, target.Modules[0]);
        return target.Health;
    }

    [TestMethod]
    public void DeathBlastsDoNotReachALaunchingShip()
    {
        Ship launching = Launching(Player, new Vector2(300_000, 0));
        Ship inPlay = SpawnShip("Vulcan Scout", Player, new Vector2(320_000, 0));

        AssertLessThan(HealthAfterDeathBlastsNextTo(inPlay, out float inPlayBefore), inPlayBefore,
                       "setup: the same blasts hurt a ship that is not launching");
        AssertEqual(HealthAfterDeathBlastsNextTo(launching, out float before), before, "a death blast next to a launching ship does not reach it");
    }

    [TestMethod]
    public void ASpaceNukeSplashDoesNotReachALaunchingShip()
    {
        Ship launching = Launching(Player, new Vector2(300_000, 0));
        Ship inPlay = SpawnShip("Vulcan Scout", Player, new Vector2(320_000, 0));

        AssertLessThan(HealthAfterSpaceNukeNextTo(inPlay, out float inPlayBefore), inPlayBefore,
                       "setup: the same splash hurts a ship that is not launching");
        AssertEqual(HealthAfterSpaceNukeNextTo(launching, out float before), before, "a space nuke's splash does not reach a launching ship");
    }

    [TestMethod]
    public void StarRadiationDoesNotHurtALaunchingShip()
    {
        var system = new SolarSystem(UState, new Vector2(600_000)) { Sun = SunType.FindSun("star_neutron") };
        UState.AddSolarSystem(system);
        Ship launching = Launching(Player, system.Position + new Vector2(3000, 0));
        Ship inPlay = SpawnShip("Vulcan Scout", Player, system.Position + new Vector2(0, 3000));
        float health = launching.Health;

        MethodInfo radiate = typeof(SolarSystem).GetMethod("ApplySolarRadiationDamage", BindingFlags.NonPublic | BindingFlags.Instance);
        for (int i = 0; i < 20; ++i)
        {
            radiate.Invoke(system, new object[] { launching });
            radiate.Invoke(system, new object[] { inPlay });
        }
        AssertLessThan(inPlay.Health, inPlay.HealthMax, "setup: the star's radiation hurts a ship that is not launching");
        AssertEqual(health, launching.Health, "star radiation does not hurt a launching ship");
    }

    [TestMethod]
    public void TroopsOrderedAboardALaunchingShipStayOnTheirShip()
    {
        Ship launching = Launching(Player, new Vector2(300_000, 0));
        Ship boarder = SpawnShip("Vulcan Scout", Enemy, launching.Position + new Vector2(0, 100));
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(boarder);
        boarder.AI.OrderTroopToBoardShip(launching);

        RunObjectsSim(1f);
        Assert.IsTrue(launching.IsLaunching, "setup: the ship must still be launching");
        AssertEqual(1, boarder.TroopCount, "a boarding order must not put troops on a launching ship");
        AssertEqual(Player, launching.Loyalty, "a launching ship cannot be captured");
    }

    [TestMethod]
    public void APlanetSendsNoBoardersAtALaunchingShip()
    {
        LoadStarterShips(Enemy.data.DefaultTroopShip);
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Planet enemyWorld = AddHomeWorldToEmpire(new Vector2(200_000), Enemy, new Vector2(205_000), explored: true);
        var troops = new Troop[3];
        for (int i = 0; i < troops.Length; ++i)
        {
            troops[i] = ResourceManager.CreateTroop("Wyvern", Enemy);
            Assert.IsTrue(troops[i].TryLandTroop(enemyWorld), "setup: the planet must hold troops");
            troops[i].UpdateMoveActions(troops[i].MaxStoredActions);
        }
        Ship launching = Launching(Player, enemyWorld.Position + new Vector2(enemyWorld.Radius + 500, 0));
        Ship inPlay = SpawnShip("Vulcan Scout", Player, enemyWorld.Position + new Vector2(0, enemyWorld.Radius + 5000));
        RunSimWhile((simTimeout: 1.5, fatal: false));
        Assert.IsTrue(launching.IsLaunching, "setup: the closer ship must still be launching");

        new AssaultBombers(enemyWorld, Enemy, Player).Evaluate();
        Ship[] boarders = troops.Select(t => t.HostShip).Where(s => s != null).ToArray();
        Assert.IsTrue(boarders.Length > 0, "setup: the planet must send boarders");
        foreach (Ship boarder in boarders)
            Assert.AreNotSame(launching, boarder.AI.EscortTarget, "no boarders go after the closer ship still launching");
        Assert.IsTrue(boarders.Any(b => b.AI.EscortTarget == inPlay), "the boarders go after the ship in play");
    }

    [TestMethod]
    public void PlanetDefensesTargetAShipInPlayOverALaunchingOneThatIsCloser()
    {
        Planet homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Ship launching = Launching(Enemy, homeworld.Position + new Vector2(homeworld.Radius + 500, 0));
        Ship inPlay = SpawnShip("Vulcan Scout", Enemy, homeworld.Position + new Vector2(0, homeworld.Radius + 5000));
        RunSimWhile((simTimeout: 1.5, fatal: false));
        Assert.IsTrue(launching.IsLaunching, "setup: the closer ship must still be launching");
        Assert.IsTrue(homeworld.System.HostileForcesPresent(Player), "setup: the planet must see hostiles in its system");

        Ship target = homeworld.ScanForSpaceCombatTargets(null, 10_000f, canLaunchShips: true);
        Assert.AreSame(inPlay, target, "planet defenses target the ship in play, not the closer one still launching");
    }
}
