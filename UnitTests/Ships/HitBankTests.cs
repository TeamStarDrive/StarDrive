using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// An off-center hit, such as a shell's push, rolls a ship as if it turned that way, by the impulse's torque over the
/// inertia of a disc of the ship's mass and radius, eased in over a quarter second, capped at 3 times its turn rate and
/// 90 degrees unless the roll it is at or easing to is greater. Dying ships, stations and ships at warp, launching or
/// landing are not rolled, by hits or blasts, and landing or launching cancels a roll still easing in. A roll past the
/// bank limit settles back at the ship's banking speed, a blast keeps a bigger roll the ship already has, and module
/// heights turn with the roll.
/// </summary>
[TestClass]
public class HitBankTests : StarDriveTest
{
    public HitBankTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair", "Vulcan Scout", "Platform Base mk1-a", "Shipyard");
        CreateUniverseAndPlayerEmpire();
    }

    static Vector2 Site(int i) => new(200_000f + i * 20_000f, 200_000f);
    Ship Spawn(string shipName, int site)
    {
        Ship ship = SpawnShip(shipName, Player, Site(site));
        ship.YRotation = 0f;
        return ship;
    }

    static void DrainShields(Ship ship)
    {
        foreach (ShipModule m in ship.GetShields())
            m.DamageShield(m.ShieldPower, null, out float _);
    }

    static readonly FieldInfo HitRollSeconds = typeof(Ship).GetField("HitRollSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo RotationThisFrame = typeof(Ship).GetField("RotationThisFrame", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly MethodInfo UpdateShipRotation = typeof(Ship).GetMethod("UpdateShipRotation", BindingFlags.Instance | BindingFlags.NonPublic);

    void RotateOneFrame(Ship ship) => UpdateShipRotation.Invoke(ship, new object[] { TestSimStep });

    // lets a hit's roll ease in frame by frame; returns the frames it took
    int EaseIn(Ship ship)
    {
        int frames = 0;
        for (; frames < 600 && (float)HitRollSeconds.GetValue(ship) > 0f; ++frames)
            RotateOneFrame(ship);
        return frames;
    }

    [TestMethod]
    public void AnOffCenterHitRollsTheShipByItsTorqueOverADiscsInertiaWithinAQuarterSecond()
    {
        Ship right = Spawn("Dreadnought mk1-a", 0);
        Ship left = Spawn("Dreadnought mk1-a", 1);
        Ship inLine = Spawn("Dreadnought mk1-a", 2);
        var impulse = new Vector2(0f, 200_000f);
        float arm = right.Radius * 0.5f;

        right.BankFromHit(impulse, right.Position + new Vector2(arm, 0f));
        left.BankFromHit(impulse, left.Position + new Vector2(-arm, 0f));
        inLine.BankFromHit(impulse, inLine.Position + new Vector2(0f, -arm));

        float expected = arm * 200_000f / (0.5f * right.Mass * right.Radius * right.Radius);
        AssertEqual(0f, right.YRotation, "the roll is not there at once");
        for (int i = 0; i < 7; ++i)
            RotateOneFrame(right);
        AssertEqual(expected * 0.01f, -expected * 7f / 15f, right.YRotation, "it eases in evenly");
        AssertEqual(1f, 8f, EaseIn(right), "over a quarter second");
        EaseIn(left);
        AssertEqual(0, EaseIn(inLine), "a push in line with the center has no roll to ease in");

        AssertLessThan(expected, 3f * right.RotationRadsPerSecond, "setup: the roll stays under the cap");
        AssertEqual(0.00001f, -expected, right.YRotation, "a push on the right side rolls the ship as if it turned clockwise");
        AssertEqual(0.00001f, expected, left.YRotation, "and one on the left side the other way");
        AssertEqual(0f, inLine.YRotation, "and one in line not at all");
    }

    [TestMethod]
    public void AShellThatStrikesOffCenterRollsTheShipByItsMomentumsTorque()
    {
        Ship target = Spawn("Dreadnought mk1-a", 0);
        DrainShields(target);
        target.Velocity = Vector2.Zero;
        Ship attacker = SpawnShip("Corsair", Enemy, new Vector2(-200_000f));
        Weapon massDriver = ResourceManager.CreateWeapon(UState, "MassDriver", attacker, null);
        AssertEqual(13f, massDriver.OrdinanceRequiredToFire, "setup: a mass driver shot costs 13 ordnance");
        Projectile shell = Projectile.Create(massDriver, attacker, target.Position + new Vector2(0, -target.Radius - 500f),
                                             new Vector2(0, 1), target, playSound: false);
        shell.Velocity = new Vector2(0f, 1600f);
        shell.DamageAmount = 1f;
        float arm = target.Radius * 0.5f;

        Assert.IsTrue(shell.Touch(target.Modules[0], target.Position + new Vector2(arm, 0f)), "setup: the shell hits the ship");
        EaseIn(target);

        float expected = arm * 13f * 5f * 1600f / (0.5f * target.Mass * target.Radius * target.Radius);
        AssertLessThan(expected, 3f * target.RotationRadsPerSecond, "setup: the roll stays under the cap");
        AssertEqual(0.00001f, -expected, target.YRotation, "a shell striking the right side rolls the ship by its momentum's torque");
    }

    [TestMethod]
    public void ASecondHitWhileTheRollEasesInAddsToTheRollItIsEasingTo()
    {
        Ship ship = Spawn("Dreadnought mk1-a", 0);
        var impulse = new Vector2(0f, 200_000f);
        Vector2 hitPos = ship.Position + new Vector2(ship.Radius * 0.5f, 0f);
        float one = ship.Radius * 0.5f * 200_000f / (0.5f * ship.Mass * ship.Radius * ship.Radius);
        AssertLessThan(one * 2f, 3f * ship.RotationRadsPerSecond, "setup: two rolls stay under the cap");

        ship.BankFromHit(impulse, hitPos);
        for (int i = 0; i < 5; ++i)
            RotateOneFrame(ship);
        ship.BankFromHit(impulse, hitPos);
        EaseIn(ship);

        AssertEqual(0.00001f, -2f * one, ship.YRotation, "the second hit adds its roll to the one the first was easing to");
    }

    [TestMethod]
    public void AHitRollsAShipAtMostThreeTimesItsTurnRateAnd90DegreesUnlessItIsRolledFurther()
    {
        Ship frigate = Spawn("Corsair", 0);
        Ship rolled = Spawn("Corsair", 1);
        Ship scout = Spawn("Vulcan Scout", 2);
        Ship pastNinety = Spawn("Corsair", 3);
        float cap = 3f * frigate.RotationRadsPerSecond;
        AssertLessThan(cap, 1.5f, "setup: a frigate's cap is under 90 degrees");
        AssertGreaterThan(3f * scout.RotationRadsPerSecond, 1.6f, "setup: a scout's is over");
        float between = -(cap + 1.5f) * 0.5f;
        rolled.YRotation = between;
        pastNinety.YRotation = -2f;
        var impulse = new Vector2(0f, 1_000_000f);
        void HitOffCenter(Ship s) => s.BankFromHit(impulse, s.Position + new Vector2(s.Radius * 0.5f, 0f));
        float uncapped = frigate.Radius * 0.5f * 1_000_000f / (0.5f * frigate.Mass * frigate.Radius * frigate.Radius);
        AssertGreaterThan(uncapped, 2f, "setup: uncapped, the hit would roll a frigate past 90 degrees");

        foreach (Ship s in new[] { frigate, rolled, scout, pastNinety })
        {
            HitOffCenter(s);
            EaseIn(s);
        }

        AssertEqual(0.0001f, -cap, frigate.YRotation, "a roll stops at three times the ship's turn rate");
        AssertEqual(0.0001f, between, rolled.YRotation, "a ship already rolled past that is neither rolled further nor flattened");
        AssertEqual(0.0001f, -(float)Math.PI / 2f, scout.YRotation, "and no roll goes past 90 degrees");
        AssertEqual(0.0001f, -2f, pastNinety.YRotation, "unless the ship is rolled further already, as after a barrel roll, which it keeps");
    }

    [TestMethod]
    public void ATinyHitOnARolledShipStillEasesInAndLetsTheRollSettle()
    {
        Ship ship = Spawn("Dreadnought mk1-a", 0);
        ship.YRotation = 0.4f;
        float arm = ship.Radius * 0.5f;
        var impulse = new Vector2(0f, 1e-7f * 0.5f * ship.Mass * ship.Radius * ship.Radius / arm); // a roll of 1e-7

        ship.BankFromHit(impulse, ship.Position + new Vector2(arm, 0f));
        for (int i = 0; i < 60; ++i)
            RotateOneFrame(ship);

        float settled = ship.RotationRadsPerSecond * TestSimStep.FixedTime * 40f;
        AssertLessThan(ship.YRotation, 0.4f - settled, "the roll eases in within a quarter second, then settles as usual");
    }

    [TestMethod]
    public void LandingOrLaunchingCancelsARollStillEasingIn()
    {
        Planet planet = AddDummyPlanet(new Vector2(400_000f));
        Ship landing = Spawn("Dreadnought mk1-a", 0);
        Ship launching = Spawn("Dreadnought mk1-a", 1);
        foreach (Ship s in new[] { landing, launching })
        {
            s.BankFromHit(new Vector2(0f, 200_000f), s.Position + new Vector2(s.Radius * 0.5f, 0f));
            RotateOneFrame(s);
            AssertGreaterThan((float)HitRollSeconds.GetValue(s), 0f, "setup: the roll is still easing in");
        }

        landing.InitLanding(LandPlan.Scrap, planet);
        launching.InitLaunch(LaunchPlan.Planet, 0f);
        Assert.IsTrue(landing.IsLanding && launching.IsLaunching, "setup: one lands, the other launches");
        RotateOneFrame(landing);
        RotateOneFrame(launching);

        AssertEqual(0, EaseIn(landing), "landing sets the roll itself, so a hit's roll still easing in is dropped");
        AssertEqual(0, EaseIn(launching), "and so does launching");
    }

    [TestMethod]
    public void DyingShipsStationsAndShipsAtWarpLaunchingOrLandingAreNotRolled()
    {
        var impulse = new Vector2(0f, 200_000f);
        Planet planet = AddDummyPlanet(new Vector2(400_000f));
        void HitOffCenter(Ship s) => s.BankFromHit(impulse, s.Position + new Vector2(s.Radius * 0.5f, 0f));

        Ship control = Spawn("Dreadnought mk1-a", 0);
        HitOffCenter(control);
        AssertGreaterThan(EaseIn(control), 0, "setup: the hit rolls a ship in play");
        AssertLessThan(control.YRotation, 0f, "setup: the hit rolls a ship in play");

        Ship dying = Spawn("Dreadnought mk1-a", 1);
        dying.Dying = true;
        dying.SetDieTimer(10f);
        HitOffCenter(dying);
        AssertEqual(0, EaseIn(dying), "a dying ship tumbles on its own and is not rolled");

        Ship station = Spawn("Platform Base mk1-a", 2);
        Assert.IsTrue(station.Stats.IsStationary, "setup: a platform is stationary");
        station.RotationRadsPerSecond = control.RotationRadsPerSecond; // so that only the station rule holds it
        HitOffCenter(station);
        AssertEqual(0, EaseIn(station), "nor a station");

        Ship warping = Spawn("Dreadnought mk1-a", 3);
        warping.engineState = Ship.MoveState.Warp;
        HitOffCenter(warping);
        AssertEqual(0, EaseIn(warping), "nor a ship at warp");

        Ship launching = Spawn("Dreadnought mk1-a", 4);
        launching.InitLaunch(LaunchPlan.Planet, 0f);
        Assert.IsTrue(launching.IsLaunching, "setup: the ship is launching");
        HitOffCenter(launching);
        AssertEqual(0, EaseIn(launching), "nor a launching ship");

        Ship landing = Spawn("Dreadnought mk1-a", 5);
        landing.InitLanding(LandPlan.Scrap, planet);
        Assert.IsTrue(landing.IsLanding, "setup: the ship is landing");
        HitOffCenter(landing);
        AssertEqual(0, EaseIn(landing), "nor a landing ship");
    }

    [TestMethod]
    public void ABlastDoesNotRollStationsOrShipsAtWarpEither()
    {
        Ship level = Spawn("Dreadnought mk1-a", 0);
        Ship station = Spawn("Shipyard", 1);
        Ship warping = Spawn("Dreadnought mk1-a", 2);
        Ship[] targets = { level, station, warping };
        var bombs = new Ship[targets.Length];
        for (int i = 0; i < targets.Length; ++i)
            bombs[i] = SpawnShip("Corsair", Enemy, targets[i].Position + new Vector2(targets[i].Radius + 100f, 0f));
        UState.Objects.Update(TestSimStep);
        warping.engineState = Ship.MoveState.Warp;
        foreach (Ship target in targets)
        {
            DrainShields(target);
            target.YRotation = 0f;
            AssertEqual(0f, target.ExplosionEvadeBaseChance(pointBlank: true, stacked: false), "setup: it cannot evade");
        }
        Assert.IsTrue(station.Stats.IsStationary, "setup: a shipyard is a station");
        float[] health = targets.Select(t => t.Health).ToArray();

        for (int i = 0; i < targets.Length; ++i)
            UState.Spatial.ShipExplode(bombs[i], 1000f, bombs[i].Position, 500f);

        for (int i = 0; i < targets.Length; ++i)
            AssertLessThan(targets[i].Health, health[i], $"setup: the blast reaches {targets[i].Name}");
        AssertLessThan(level.YRotation, 0f, "setup: the blast rolls a ship in play");
        AssertEqual(0f, station.YRotation, "but not a station");
        AssertEqual(0f, warping.YRotation, "nor a ship at warp");
    }

    // one frame of the ship turning so that it banks toward a positive roll
    void TurnOneFrame(Ship ship)
    {
        RotationThisFrame.SetValue(ship, -0.001f);
        RotateOneFrame(ship);
    }

    [TestMethod]
    public void ARollPastTheBankLimitSettlesBackWhileTheShipTurnsIntoIt()
    {
        Ship ship = Spawn("Dreadnought mk1-a", 0);
        ship.InFrustum = true;
        ship.KnownByEmpires.SetSeen(Player);
        Assert.IsTrue(ship.IsVisibleToPlayer, "setup: on screen, where a turning ship banks");
        float bankPerFrame = ship.RotationRadsPerSecond * TestSimStep.FixedTime;
        AssertGreaterThan(bankPerFrame, 0f, "setup: the ship banks");
        ship.YRotation = ship.MaxBank * 3f;

        TurnOneFrame(ship);
        AssertEqual(0.00001f, ship.MaxBank * 3f - bankPerFrame, ship.YRotation,
                    "a roll past the bank limit settles back at the banking speed, though the ship banks the same way");

        for (int i = 0; i < 2000 && ship.YRotation > ship.MaxBank; ++i)
            TurnOneFrame(ship);
        for (int i = 0; i < 10; ++i)
            TurnOneFrame(ship);
        AssertEqual(0.00001f, ship.MaxBank, ship.YRotation, "then the ship banks into its turn up to the limit, and holds it");
    }

    [TestMethod]
    public void ABlastKeepsABiggerRollTheShipAlreadyHas()
    {
        Ship rolled = Spawn("Dreadnought mk1-a", 0);
        Ship flat = Spawn("Dreadnought mk1-a", 1);
        Ship bombNearRolled = SpawnShip("Corsair", Enemy, rolled.Position + new Vector2(rolled.Radius + 100f, 0f));
        Ship bombNearFlat = SpawnShip("Corsair", Enemy, flat.Position + new Vector2(flat.Radius + 100f, 0f));
        UState.Objects.Update(TestSimStep);
        DrainShields(rolled);
        DrainShields(flat);
        rolled.YRotation = -2f;
        flat.YRotation = 0f;

        UState.Spatial.ShipExplode(bombNearRolled, 1000f, bombNearRolled.Position, 500f);
        UState.Spatial.ShipExplode(bombNearFlat, 1000f, bombNearFlat.Position, 500f);

        AssertLessThan(flat.YRotation, 0f, "setup: the blast rolls a level ship");
        AssertEqual(-2f, rolled.YRotation, "but leaves a bigger roll as it was");
    }

    [TestMethod]
    public void ABlastsOwnRollStopsAtTheBankLimit()
    {
        Ship capital = Spawn("Dreadnought mk1-a", 0); // a capital never evades a blast
        Ship bomb = SpawnShip("Corsair", Enemy, capital.Position + new Vector2(capital.Radius + 100f, 0f));
        UState.Objects.Update(TestSimStep);
        DrainShields(capital);
        capital.YRotation = 0f;
        AssertEqual(0f, capital.ExplosionEvadeBaseChance(pointBlank: true, stacked: false), "setup: it cannot evade");
        AssertGreaterThan(200_000f / (float)Math.Pow(capital.Mass, 1.3), capital.MaxBank * 2f, "setup: the blast would roll it past the limit");

        UState.Spatial.ShipExplode(bomb, 1000f, bomb.Position, 200_000f);

        AssertEqual(0.00001f, -capital.MaxBank, capital.YRotation, "a blast rolls a ship at most to the bank limit");
    }

    [TestMethod]
    public void ModuleHeightsTurnWithTheRollAtAnyRoll()
    {
        Ship ship = Spawn("Dreadnought mk1-a", 0);
        ShipModule wing = ship.Modules.First(m => Math.Abs(m.LocalCenter.X) > 50f);
        wing.HullSurfaceZ = 40f; // with no mesh loaded every module sits just over the hull
        float x = wing.LocalCenter.X, z = wing.HullSurfaceZ;
        float tolerance = (Math.Abs(x) + Math.Abs(z)) * 0.01f;

        foreach (float roll in new[] { 0f, 1.4f, 2.8f, -1f })
        {
            ship.YRotation = roll;
            ship.UpdateModulePositions(TestSimStep, forceUpdate: true);
            AssertEqual(tolerance, z * (float)Math.Cos(roll) - x * (float)Math.Sin(roll), wing.Center3D.Z,
                        $"at a roll of {roll} the module sits where the rolled hull carries it");
        }
    }
}
