using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A ship that dies near a planet keeps the velocity it had and is pulled evenly onto the planet: its velocity turns into
/// the fall over the time a straight fall would take, 2 to 5 seconds, without thrust. It dies only when it reaches the
/// planet, even when it first flies away from it or was tethered to it.
/// </summary>
[TestClass]
public class PlanetCrashTests : StarDriveTest
{
    readonly Planet CrashPlanet;

    public PlanetCrashTests()
    {
        LoadStarterShips("Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
        CrashPlanet = AddDummyPlanet(new Vector2(400_000f));
        Universe.Particles = new ParticleManager(Content); // the ship burns through the atmosphere
    }

    [TestCleanup]
    public override void Cleanup()
    {
        Universe.Particles?.Dispose();
        Universe.Particles = null;
        base.Cleanup();
    }

    Ship SpawnCrashing(Vector2 direction, bool tethered = false)
    {
        Ship ship = SpawnShip("Vulcan Scout", Player, CrashPlanet.Position + new Vector2(3000f, 0f));
        if (tethered)
            ship.TetherToPlanet(CrashPlanet);
        ship.Velocity = direction * ship.MaxSTLSpeed;
        ship.PlanetCrash = new PlanetCrash(CrashPlanet, ship);
        ship.Dying = true;
        return ship;
    }

    // updates the crashing ship until it dies, at most a minute; the largest change of its velocity in one step
    float CrashUntilDead(Ship ship, out Vector2 velocityAfterOneSecond)
    {
        velocityAfterOneSecond = Vector2.Zero;
        float largestChange = 0f;
        for (int step = 1; step <= 3600 && ship.Active; step++)
        {
            Vector2 before = ship.Velocity;
            ship.Update(TestSimStep);
            if (!ship.Active)
                break;
            largestChange = System.Math.Max(largestChange, (ship.Velocity - before).Length());
            if (step == 60)
                velocityAfterOneSecond = ship.Velocity;
        }
        return largestChange;
    }

    void AssertDiedOnThePlanet(Ship ship, string what)
    {
        Assert.IsFalse(ship.Active, $"{what} dies within a minute");
        AssertLessThan(ship.Position.Distance(CrashPlanet.Position), CrashPlanet.Radius + 200f, $"{what} dies on the planet");
    }

    [TestMethod]
    public void ADyingShipKeepsItsVelocityAndIsPulledEvenlyOntoThePlanet()
    {
        Ship ship = SpawnCrashing(new Vector2(0f, 1f));
        float startSpeed = ship.Velocity.Y;
        AssertGreaterThan(startSpeed, 200f, "setup: the ship flies past the planet");

        float largestChange = CrashUntilDead(ship, out Vector2 afterOneSecond);

        AssertGreaterThan(afterOneSecond.Y, startSpeed * 0.6f, "a second in, the ship still flies on past the planet");
        AssertLessThan(largestChange, 3f, "the velocity changes a little at a time");
        AssertDiedOnThePlanet(ship, "the ship");
    }

    [TestMethod]
    public void AShipFlyingAwayFromThePlanetTurnsBackAndFallsOntoIt()
    {
        Ship ship = SpawnCrashing(new Vector2(1f, 0f));

        float largestChange = CrashUntilDead(ship, out Vector2 afterOneSecond);

        AssertGreaterThan(afterOneSecond.X, 0f, "a second in, the ship still flies away");
        AssertLessThan(largestChange, 3f, "the velocity changes a little at a time");
        AssertDiedOnThePlanet(ship, "a ship that first flies away");
    }

    [TestMethod]
    public void AShipHitAtWarpFallsFromItsTopSublightSpeed()
    {
        Ship ship = SpawnShip("Vulcan Scout", Player, CrashPlanet.Position + new Vector2(3000f, 0f));
        ship.Velocity = new Vector2(0f, ship.MaxSTLSpeed * 20f);
        ship.PlanetCrash = new PlanetCrash(CrashPlanet, ship);
        ship.Dying = true;

        AssertEqual(0.01f, ship.MaxSTLSpeed, ship.Velocity.Length(), "the fall starts at the ship's top sublight speed");
        AssertEqual(0.01f, 0f, ship.Velocity.X, "in the direction it flew");
        CrashUntilDead(ship, out _);
        AssertDiedOnThePlanet(ship, "a ship hit at warp");
    }

    [TestMethod]
    public void ATetheredShipThatIsNotAStationFallsOntoThePlanet()
    {
        Ship ship = SpawnCrashing(new Vector2(0f, 1f), tethered: true);
        Assert.IsFalse(ship.IsPlatformOrStation, "setup: the ship is not a station");
        Assert.IsNull(ship.GetTether(), "the crash frees the ship from its tether");
        Assert.IsFalse(CrashPlanet.OrbitalStations.Contains(ship), "and from the planet's orbitals");

        CrashUntilDead(ship, out _);

        AssertDiedOnThePlanet(ship, "a tethered ship");
    }
}
