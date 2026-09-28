using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class ShipEmpRecoveryTests : StarDriveTest
{
    const float StartingEmp = 40000f;

    public ShipEmpRecoveryTests()
    {
        LoadStarterShips("Dreadnought mk1-a");
        CreateUniverseAndPlayerEmpire();
    }

    Ship SpawnDisabled(Empire owner = null)
    {
        Ship ship = SpawnShip("Dreadnought mk1-a", owner ?? Player, Vector2.Zero);
        Assert.IsFalse(ship.OnHighAlert, "setup: high alert slows EMP recovery");
        ship.CauseEmpDamage(StartingEmp);
        AssertEqual(0.01f, StartingEmp, ship.EMPDamage, "setup: the EMP must not be capped");
        return ship;
    }

    float EmpLeftAfterOneSecond(Ship ship, int stepsPerSecond)
    {
        var step = new FixedSimTime(1f / stepsPerSecond);
        for (int i = 0; i < stepsPerSecond; ++i)
            ship.UpdateShipStatus(step);
        return ship.EMPDamage;
    }

    [TestMethod]
    public void EmpWearsOffPerSecondOfGameTimeWhateverTheSimulationRate()
    {
        float at60 = EmpLeftAfterOneSecond(SpawnDisabled(), 60);
        AssertGreaterThan(at60, 0f, "setup: some EMP must be left after a second");

        AssertEqual(1f, at60, EmpLeftAfterOneSecond(SpawnDisabled(), 30),
            "a lower simulation rate, as the game picks in big battles, must not slow EMP recovery");
        AssertEqual(1f, at60, EmpLeftAfterOneSecond(SpawnDisabled(), 120),
            "a higher simulation rate, as 0.5x game speed runs, must not speed EMP recovery up");
    }

    [TestMethod]
    public void EmpWearsOffAsBeforeAtTheDefaultSimulationRate()
    {
        Ship ship = SpawnDisabled();
        float perStepAt60 = 20 + ship.BonusEMPProtection / 20;

        AssertEqual(1f, StartingEmp - 60 * perStepAt60, EmpLeftAfterOneSecond(ship, 60),
            "at 60 steps a second EMP must wear off exactly as it did per step");
    }

    [TestMethod]
    public void HighAlertSlowsEmpRecoveryAsBefore()
    {
        Ship ship = SpawnDisabled();
        ship.SetHighAlertStatus();
        float perStepAt60 = 1 + ship.BonusEMPProtection / 1000;

        AssertEqual(1f, StartingEmp - 60 * perStepAt60, EmpLeftAfterOneSecond(ship, 60),
            "on high alert EMP must wear off exactly as it did per step");
    }

    [TestMethod]
    public void RemnantsRecoverAtFullSpeedEvenOnHighAlert()
    {
        Enemy.SetAsRemnants(Enemy.AI);
        Ship ship = SpawnDisabled(Enemy);
        Assert.IsTrue(ship.Loyalty.WeAreRemnants, "setup: the ship must be a Remnant");
        ship.SetHighAlertStatus();
        float perStepAt60 = 20 + ship.BonusEMPProtection / 20;

        AssertEqual(1f, StartingEmp - 60 * perStepAt60, EmpLeftAfterOneSecond(ship, 60),
            "a Remnant on high alert must still shake off EMP at the full rate");
    }
}
