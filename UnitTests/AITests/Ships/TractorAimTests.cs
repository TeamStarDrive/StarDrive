using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class TractorAimTests : StarDriveTest
{
    static readonly Vector2 Center = new(300_000, 300_000);
    readonly TestShip Us;
    readonly Weapon Tractor;
    readonly TestShip Fighter, Cruiser;

    public TractorAimTests()
    {
        LoadStarterShips("Warbird mk1-c", "Terran-Prototype", "Fang Strafer");
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
        UState.Objects.EnableParallelUpdate = false;
        Us = SpawnShip("Warbird mk1-c", Player, Center, Vectors.Up);
        Us.AI.SetCombatTriggerDelay(1000f);
        Us.AI.OrderHoldPosition(MoveOrder.StandGround | MoveOrder.HoldPosition);
        Tractor = Us.Weapons.Find(w => w.TractorDamage > 0f && w.DamageAmount < 1f);
        Assert.IsNotNull(Tractor, "setup: the ship must carry a pure tractor beam");

        Fighter = SpawnShipNoCombatHoldPos("Fang Strafer", Enemy, InTractorArc(1500f));
        Cruiser = SpawnShipNoCombatHoldPos("Terran-Prototype", Enemy, InTractorArc(2500f));
        Fighter.AI.SetCombatTriggerDelay(1000f);
        Cruiser.AI.SetCombatTriggerDelay(1000f);
        RunObjectsSim(TestSimStep);
        Fighter.Velocity = Vectors.Right * 600f;
        Cruiser.Velocity = Vector2.Zero;
        AssertTrue(Tractor.TargetValid(Cruiser), "setup: the tractor must be allowed to pull the cruiser");
    }

    Vector2 InTractorArc(float distance)
        => Tractor.Module.Position + (Us.Rotation + Tractor.Module.TurretAngleRads).RadiansToDirection() * distance;

    // the ship the tractor fires at, given our ship's main target and the ships around it
    Ship Pulled(Ship mainTarget, params Ship[] around)
    {
        Tractor.UpdateAndFireAtTarget(mainTarget, Empty<Projectile>.Array, around, TestSimStep.FixedTime);
        return (Tractor.FireTarget as ShipModule)?.GetParent();
    }

    [TestMethod]
    public void ATractorPullsTheFastestShipInReachFightersIncluded()
    {
        AssertEqual(Fighter, Pulled(Cruiser, Fighter, Cruiser), "a tractor must pull the fastest ship in reach over its ship's main target");
    }

    [TestMethod]
    public void ATractorPassesOverAShipAlreadyHeldStill()
    {
        Fighter.CauseTractorDamage(Fighter.Mass + 1f, hittingShields: false);
        AssertEqual(Cruiser, Pulled(Fighter, Cruiser, Fighter), "a tractor must not waste a pull on a ship already held still");
    }

    [TestMethod]
    public void ATractorHoldsFireWhenEveryShipInReachIsHeld()
    {
        Fighter.CauseTractorDamage(Fighter.Mass + 1f, hittingShields: false);
        AssertEqual(null, Pulled(Fighter, Fighter), "a tractor must hold fire with nothing left to pull");
    }

    [TestMethod]
    public void ATractorPicksAgainAtItsNextShot()
    {
        AssertEqual(Cruiser, Pulled(Cruiser, Cruiser), "setup: the first pull must go to the only ship in reach");
        Tractor.CooldownTimer = 0f;
        Tractor.UpdateAndFireAtTarget(Cruiser, Empty<Projectile>.Array, new Ship[] { Fighter, Cruiser }, 1f);
        AssertEqual(Fighter, (Tractor.FireTarget as ShipModule)?.GetParent(), "a tractor must pick the fastest ship again at its next shot");
    }

    [TestMethod]
    public void AWeaponThatDealsDamageStillPrefersItsShipsMainTarget()
    {
        Weapon gun = Us.Weapons.Find(w => w.DamageAmount >= 1f && !w.TruePD && w.TargetValid(Fighter)
                                       && Us.IsTargetInFireArcRange(w, Fighter) && Us.IsTargetInFireArcRange(w, Cruiser));
        Assert.IsNotNull(gun, "setup: the ship must carry a damaging weapon that reaches both ships");
        gun.UpdateAndFireAtTarget(Cruiser, Empty<Projectile>.Array, new Ship[] { Fighter, Cruiser }, TestSimStep.FixedTime);
        AssertEqual(Cruiser, (gun.FireTarget as ShipModule)?.GetParent(), "a damaging weapon must keep to its ship's main target");
    }

    [TestMethod]
    public void ATractorPullsTheShipThePlayerOrderedEvenIfSlower()
    {
        Us.AI.OrderAttackSpecificTarget(Cruiser);
        AssertTrue(Us.AI.HasPriorityTarget, "setup: the player's order must give our ship a priority target");
        AssertEqual(Cruiser, Pulled(Cruiser, Fighter, Cruiser), "a tractor must pull the ship the player ordered");
    }
}
