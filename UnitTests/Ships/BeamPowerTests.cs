using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A firing beam goes out at once when its ship runs out of power, its emitter is destroyed or loses power, or its ship
/// dies, instead of burning on until its duration ends.
/// </summary>
[TestClass]
public class BeamPowerTests : StarDriveTest
{
    public BeamPowerTests()
    {
        LoadStarterShips("Proton Troopship  Mk1");
        CreateUniverseAndPlayerEmpire();
    }

    // the troopship stores its power in fuel cells apart from its reactor, and carries laser cannons and turrets
    TestShip NewShip() => SpawnShip("Proton Troopship  Mk1", Player, new Vector2(1000f, 1000f));

    // the prototype's reactors make more than it draws, so they trickle power into an emptied store
    TestShip NewReactorFedShip() => SpawnShip("Terran-Prototype", Player, new Vector2(3000f, 1000f));

    void Run(Ship ship, float seconds)
    {
        for (float time = 0f; time < seconds; time += TestSimStep.FixedTime)
            ship.Update(TestSimStep);
    }

    // a LaserBeam fired from one of the ship's weapon modules at a point ahead of it
    Beam FireBeam(Ship ship)
    {
        ShipModule emitter = ship.Modules.First(m => m.InstalledWeapon != null);
        Weapon laser = ResourceManager.CreateWeapon(UState, "LaserBeam", ship, emitter);
        var beam = new Beam(UState.CreateId(), laser, emitter.Position, emitter.Position + ship.Direction * 500f);
        beam.Update(TestSimStep);
        Assert.IsTrue(beam.Active, "setup: the beam fires while its ship has power");
        return beam;
    }

    static void KillReactors(Ship ship)
    {
        foreach (ShipModule m in ship.Modules.Where(m => m.PowerFlowMax > 0f))
            m.SetHealth(0f, "Test");
    }

    [TestMethod]
    public void ABeamBurnsWhileItsShipHasPower()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        for (int i = 0; i < 10; ++i)
            beam.Update(TestSimStep);
        Assert.IsTrue(beam.Active, "a beam on a powered ship burns on");
    }

    [TestMethod]
    public void ABeamGoesOutWhenItsShipsReactorsDie()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        KillReactors(ship);
        Run(ship, 1.1f);
        AssertEqual(0.001f, 0f, ship.PowerFlowMax, "setup: no reactor works");
        AssertGreaterThan(ship.PowerCurrent, beam.PowerCost * TestSimStep.FixedTime, "setup: the fuel cells could still feed the beam");
        beam.Module.Powered = true; // its emitter lost power with the reactors too; that is another test

        beam.Update(TestSimStep);
        Assert.IsFalse(beam.Active, "no reactors, no power: the beam goes out");
    }

    [TestMethod]
    public void ABeamGoesOutWhenItsShipsStoreIsEmptied()
    {
        TestShip ship = NewReactorFedShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        beam.PowerCost = 60f; // a beam cheap enough to burn on the reactors' trickle
        ship.PowerCurrent = 0f; // as other beams or shots would leave it
        Run(ship, TestSimStep.FixedTime);
        AssertGreaterThan(ship.PowerCurrent, beam.PowerCost * TestSimStep.FixedTime, "setup: the trickle could pay this tick of the beam");
        Assert.IsTrue(ship.OutOfPower, "setup: an emptied store is no power");

        beam.Update(TestSimStep);
        Assert.IsFalse(beam.Active, "the beam goes out instead of burning on the trickle until its duration ends");
    }

    [TestMethod]
    public void ABeamBurnsOnWhenTheStoreIsOnlyTooLowForACannonShot()
    {
        TestShip ship = NewReactorFedShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        beam.PowerCost = 60f;
        ship.PowerCurrent = ship.CheapestEnergyShot * 0.9f; // read at the start of the tick: not enough for a laser shot
        Run(ship, TestSimStep.FixedTime);
        AssertGreaterThan(ship.PowerCurrent, beam.PowerCost * TestSimStep.FixedTime, "setup: the store can pay this tick of the beam");
        Assert.IsFalse(ship.OutOfPower, "too little for a cannon shot dims the lights but is not out of power");

        beam.Update(TestSimStep);
        Assert.IsTrue(beam.Active, "a beam the store can pay burns on while the cannons recharge");
    }

    [TestMethod]
    public void ABeamWeaponDoesNotStartOnAShipOutOfPower()
    {
        TestShip ship = NewReactorFedShip();
        Run(ship, 0.1f);
        ShipModule emitter = ship.Modules.First(m => m.InstalledWeapon != null);
        Weapon NewLaser() => ResourceManager.CreateWeapon(UState, "LaserBeam", ship, emitter);
        Vector2 ahead = emitter.Position + ship.Direction * 500f;
        Assert.IsTrue(NewLaser().ManualFireTowardsPos(ahead), "setup: a powered ship starts the beam");

        ship.PowerCurrent = 0f;
        Run(ship, TestSimStep.FixedTime);
        Assert.IsTrue(ship.OutOfPower, "setup: its store was emptied");
        Assert.IsFalse(NewLaser().ManualFireTowardsPos(ahead), "a beam does not start only to go out on its first update");
    }

    [TestMethod]
    public void ABeamWeaponDoesNotStartOnAStoreAnotherBeamEmptiedThisTick()
    {
        TestShip ship = NewReactorFedShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        ship.PowerCurrent = beam.PowerCost * TestSimStep.FixedTime * 0.5f;
        beam.Update(TestSimStep); // in the game beams update after their ships and before the ships fire
        AssertEqual(0.001f, 0f, ship.PowerCurrent, "setup: the beam emptied the store");
        Assert.IsFalse(ship.OutOfPower, "setup: the ship read its store before the beam emptied it");

        ShipModule emitter = ship.Modules.First(m => m.InstalledWeapon != null);
        Weapon laser = ResourceManager.CreateWeapon(UState, "LaserBeam", ship, emitter);
        Assert.IsFalse(laser.ManualFireTowardsPos(emitter.Position + ship.Direction * 500f), "a beam started on an empty store");
    }

    [TestMethod]
    public void ABeamGoesOutWhenItsEmitterIsDestroyed()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        beam.Module.SetHealth(0f, "Test");
        beam.Update(TestSimStep);
        Assert.IsFalse(beam.Active, "a destroyed emitter fires nothing");
    }

    [TestMethod]
    public void ABeamGoesOutWhenItsEmitterLosesPower()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        beam.Module.Powered = false;
        beam.Update(TestSimStep);
        Assert.IsFalse(beam.Active, "an emitter cut off from the reactors fires nothing");
    }

    [TestMethod]
    public void ABeamGoesOutWhenItsShipDies()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        ship.Die(null, cleanupOnly: true);
        Assert.IsFalse(ship.Active, "setup: the ship is gone");
        Assert.IsTrue(beam.Active, "setup: its beam outlives it until the beam's next update");
        beam.Update(TestSimStep);
        Assert.IsFalse(beam.Active, "a dead ship's beam goes out");
    }

    [TestMethod]
    public void ABeamGoesOutWhenItsShipStartsDying()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        Beam beam = FireBeam(ship);
        ship.Dying = true; // tumbling for a few seconds before it explodes
        Assert.IsTrue(ship.Active, "setup: a dying ship is still active");
        beam.Update(TestSimStep);
        Assert.IsFalse(beam.Active, "a dying ship's beam goes out");
    }
}
