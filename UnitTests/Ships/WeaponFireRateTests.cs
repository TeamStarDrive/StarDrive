using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass]
    public class WeaponFireRateTests : StarDriveTest
    {
        Ship Ship;
        Weapon Thrower; // 0.05s fire delay, faster than one fire attempt per 2 steps at 30 sim steps per second

        public WeaponFireRateTests()
        {
            LoadStarterShips("Scythe Melter mk1");
            CreateUniverseAndPlayerEmpire();
            Ship = SpawnShip("Scythe Melter mk1", Player, new Vector2(300_000, 300_000));
            Thrower = Ship.Weapons.First(w => w.UID == "PlasmaThrower");
        }

        Vector2 InFront => Thrower.Module.Position + (Ship.Rotation + Thrower.Module.TurretAngleRads).RadiansToDirection() * 400f;

        // steps the weapon as the sim does and tries to fire like ShipAI; Due = shots whose cooldown ran out by the last attempt
        (int Shots, int Due, int Projectiles, float Damage) FireFor(int seconds, int simStepsPerSecond)
        {
            var step = new FixedSimTime(1f / simStepsPerSecond);
            Thrower.CooldownTimer = 0f;
            int shots = 0;
            float lastAttemptTime = 0f;
            int projectilesBefore = GetProjectileCount(Ship);
            for (int i = 0; i < seconds * simStepsPerSecond; ++i)
            {
                Thrower.Update(step);
                if (i % ShipAI.StepsBetweenFireAttempts == 0)
                {
                    Ship.ChangeOrdnance(1000);
                    float before = Ship.Ordinance;
                    Thrower.ManualFireTowardsPos(InFront);
                    shots += (int)System.Math.Round((before - Ship.Ordinance) / Thrower.OrdnancePerShot);
                    lastAttemptTime = (i + 1) * step.FixedTime;
                }
            }

            int due = (int)(lastAttemptTime / Thrower.NetFireDelay + 0.001f) + 1;
            Projectile[] fired = GetProjectiles(Ship).Skip(projectilesBefore).ToArray();
            return (shots, due, fired.Length, fired.Sum(p => p.DamageAmount));
        }

        float SingleShotDamage()
        {
            Ship.ChangeOrdnance(1000);
            Thrower.CooldownTimer = 0f;
            Assert.IsTrue(Thrower.ManualFireTowardsPos(InFront), "setup: the thrower must fire");
            return GetProjectiles(Ship).Last().DamageAmount;
        }

        [TestMethod]
        public void AFastWeaponFiresAtItsDesignedRate()
        {
            var fired = FireFor(seconds: 6, simStepsPerSecond: 60);

            AssertEqual(fired.Due, fired.Shots, "shots in 6 seconds at 60 sim steps per second");
            AssertEqual(fired.Shots, fired.Projectiles, "at 60 sim steps per second every shot is its own projectile");
        }

        [TestMethod]
        public void ASlowSimCombinesOwedShotsIntoOneProjectile()
        {
            float single = SingleShotDamage();
            var fired = FireFor(seconds: 6, simStepsPerSecond: 30);

            AssertEqual(fired.Due, fired.Shots, "shots in 6 seconds at 30 sim steps per second");
            Assert.IsTrue(fired.Projectiles < fired.Shots, $"owed shots were not combined: {fired.Projectiles} projectiles for {fired.Shots} shots");
            AssertEqual(1f, fired.Shots * single, fired.Damage, "combined projectiles must carry the damage of every shot");
        }

        [TestMethod]
        public void TheCooldownAfterAShotIsExactlyTheFireDelay()
        {
            for (int i = 0; i < 20; ++i)
            {
                SingleShotDamage();
                AssertEqual(0.0001f, Thrower.NetFireDelay, Thrower.CooldownTimer, "no random wait is added to the cooldown");
            }
        }

        [TestMethod]
        public void ACombinedShotIsLimitedByThePowerLeft()
        {
            Ship scout = SpawnShip("TEST_Vulcan Scout", Player, new Vector2(320_000, 300_000));
            var gun = (WeaponTestWrapper)scout.Weapons[0];
            gun.TestSalvoCount = 1;
            gun.TestProjectileCount = 1;
            gun.TestOrdinanceRequiredToFire = 0;
            gun.TestPowerRequiredToFire = 2;
            Vector2 inFront = gun.Module.Position + scout.Direction * 400f;

            scout.PowerCurrent = 10;
            gun.CooldownTimer = -gun.NetFireDelay * 1.5f; // two shots owed
            Assert.IsTrue(gun.ManualFireTowardsPos(inFront), "the gun must fire");
            AssertEqual(0.001f, 6f, scout.PowerCurrent, "two owed shots use two shots of power");

            scout.PowerCurrent = 3;
            gun.CooldownTimer = -gun.NetFireDelay * 1.5f;
            Assert.IsTrue(gun.ManualFireTowardsPos(inFront), "the gun must fire");
            AssertEqual(0.001f, 1f, scout.PowerCurrent, "only the shot the power paid for was fired");
        }

        [TestMethod]
        public void OnlyWeaponsThatDealPlainDamageCombineShots()
        {
            Ship scout = SpawnShip("TEST_Vulcan Scout", Player, new Vector2(320_000, 300_000));
            var gun = (WeaponTestWrapper)scout.Weapons[0];
            gun.TestSalvoCount = 1;
            gun.TestProjectileCount = 1;
            gun.TestOrdinanceRequiredToFire = 0;
            gun.TestPowerRequiredToFire = 2;
            gun.TestTag_PD = true;
            Vector2 inFront = gun.Module.Position + scout.Direction * 400f;

            scout.PowerCurrent = 10;
            gun.CooldownTimer = -gun.NetFireDelay * 1.5f; // two shots owed
            Assert.IsTrue(gun.ManualFireTowardsPos(inFront), "the gun must fire");
            AssertEqual(0.001f, 8f, scout.PowerCurrent, "a point defense gun fires its owed shots one at a time");
        }

        [TestMethod]
        public void IdleTimeBanksAtMostOneFireAttempt()
        {
            Thrower.CooldownTimer = 0f;
            var step = new FixedSimTime(1f / 60f);
            for (int i = 0; i < 600; ++i)
                Thrower.Update(step); // ten seconds without a target

            Ship.ChangeOrdnance(1000);
            float before = Ship.Ordinance;
            Assert.IsTrue(Thrower.ManualFireTowardsPos(InFront), "the thrower must fire");
            AssertEqual(0.001f, Thrower.OrdnancePerShot, before - Ship.Ordinance, "the first shot after idling must be a single shot");
        }

        [TestMethod]
        public void ACombinedShotIsLimitedByTheOrdnanceLeft()
        {
            float single = SingleShotDamage();

            Ship.ChangeOrdnance(1000);
            Thrower.CooldownTimer = -Thrower.NetFireDelay * 1.5f; // two shots owed
            float before = Ship.Ordinance;
            Assert.IsTrue(Thrower.ManualFireTowardsPos(InFront), "the thrower must fire");
            AssertEqual(0.001f, Thrower.OrdnancePerShot * 2, before - Ship.Ordinance, "two owed shots use two shots of ordnance");
            AssertEqual(0.001f, single * 2, GetProjectiles(Ship).Last().DamageAmount, "two owed shots fire one projectile with double damage");

            Ship.ChangeOrdnance(-Ship.Ordinance + Thrower.OrdnancePerShot * 1.5f);
            Thrower.CooldownTimer = -Thrower.NetFireDelay * 1.5f;
            Assert.IsTrue(Thrower.ManualFireTowardsPos(InFront), "the thrower must fire");
            AssertEqual(0.001f, Thrower.OrdnancePerShot * 0.5f, Ship.Ordinance, "only the shot the ordnance paid for was fired");
            AssertEqual(0.001f, single, GetProjectiles(Ship).Last().DamageAmount, "a shot the ordnance cannot pay for adds no damage");
        }
    }
}
