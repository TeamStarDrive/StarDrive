using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

[TestClass]
public class ColonyShieldChargeTests : StarDriveTest
{
    const string ShieldShip = "TEST_ShipShield"; // two shields, amplified past their base power
    const int Level = 2;
    const int TurnSeconds = 3;
    const float Charge = Level * 100f * TurnSeconds;
    readonly Planet Homeworld;

    public ColonyShieldChargeTests()
    {
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(ShieldShip);
        UState.Objects.EnableParallelUpdate = false;
        UState.P.TurnTimer = TurnSeconds;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        typeof(Planet).GetProperty(nameof(Planet.Level)).SetValue(Homeworld, Level);
    }

    static ShipModule[] Shields(Ship ship) => ship.GetShields().ToArray();

    // ships holding position beside the colony, their shields drained
    Ship[] DrainedShipsBesideTheColony(int count)
    {
        var ships = new Ship[count];
        for (int i = 0; i < count; ++i)
        {
            ships[i] = SpawnShip(ShieldShip, Player, Homeworld.Position + new Vector2(Homeworld.Radius + 1000, 500 * i));
            ships[i].AI.OrderHoldPosition(MoveOrder.HoldPosition);
        }
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        foreach (Ship ship in ships)
        {
            Assert.IsTrue(Homeworld.System.ShipList.Contains(ship), "setup: the ship is in the colony's system");
            Assert.IsFalse(ship.IsInRecentCombat, "setup: the ship is out of combat");
            AssertEqual(2, Shields(ship).Length, "setup: the ship has two shields");
            foreach (ShipModule shield in Shields(ship))
            {
                AssertGreaterThan(shield.ActualShieldPowerMax, Charge, "setup: a turn's charge does not fill a shield");
                shield.DamageShield(shield.ShieldPower, null, out _);
            }
            ship.UpdateShields();
            AssertEqual(0f, ship.ShieldPower, "setup: the shields are drained");
        }
        return ships;
    }

    Ship DrainedShipBesideTheColony() => DrainedShipsBesideTheColony(1)[0];

    [TestMethod]
    public void AColonyChargesEachShieldByItsLevelForEverySecondOfTheTurn()
    {
        Ship ship = DrainedShipBesideTheColony();

        Homeworld.GeodeticManager.AffectNearbyShips();

        foreach (ShipModule shield in Shields(ship))
            AssertEqual(0.01f, Charge, shield.ShieldPower, "each shield gains the planet's level times 100 for every second of the turn");
        AssertEqual(0.01f, Charge * 2, ship.ShieldPower, "and the ship's shield power shows it at once");
    }

    [TestMethod]
    public void TheChargeFillsAnAmplifiedShieldAndNoMore()
    {
        Ship ship = DrainedShipBesideTheColony();
        foreach (ShipModule shield in Shields(ship))
        {
            AssertGreaterThan(shield.ActualShieldPowerMax, shield.ShieldPowerMax, "setup: amplifiers raised the shield past its base power");
            AssertLessThan(shield.ActualShieldPowerMax, Charge * 3, "setup: three turns' charge is more than the shield holds");
        }

        for (int turn = 0; turn < 3; ++turn)
            Homeworld.GeodeticManager.AffectNearbyShips();

        foreach (ShipModule shield in Shields(ship))
            AssertEqual(0.01f, shield.ActualShieldPowerMax, shield.ShieldPower, "three turns fill the amplified shield, and no more");
    }

    [TestMethod]
    public void AnUnpoweredShieldGetsNoCharge()
    {
        Ship ship = DrainedShipBesideTheColony();
        ShipModule[] shields = Shields(ship);
        shields[0].Powered = false;

        Homeworld.GeodeticManager.AffectNearbyShips();

        AssertEqual(0f, shields[0].ShieldPower, "an unpowered shield is not charged");
        AssertEqual(0.01f, Charge, shields[1].ShieldPower, "the powered one is");
    }

    [TestMethod]
    public void NoShieldIsChargedWhileThereIsFightingAtTheColony()
    {
        Ship ship = DrainedShipBesideTheColony();
        typeof(Planet).GetProperty(nameof(Planet.SpaceCombatNearPlanet)).SetValue(Homeworld, true);

        Homeworld.GeodeticManager.AffectNearbyShips();

        AssertEqual(0f, ship.ShieldPower, "a colony in a fight charges no shield");
    }

    [TestMethod]
    public void AShipInCombatOrHitJustNowGetsNoCharge()
    {
        Ship[] ships = DrainedShipsBesideTheColony(2);
        Ship fighting = ships[0], hit = ships[1];
        fighting.InCombat = true;
        hit.SetLastDamagedBy(null);
        Assert.IsFalse(Homeworld.SpaceCombatNearPlanet, "setup: the colony itself is not fighting");

        Homeworld.GeodeticManager.AffectNearbyShips();

        AssertEqual(0f, fighting.ShieldPower, "a ship in combat gets no charge");
        AssertEqual(0f, hit.ShieldPower, "nor does one hit in the last few seconds");
    }
}
