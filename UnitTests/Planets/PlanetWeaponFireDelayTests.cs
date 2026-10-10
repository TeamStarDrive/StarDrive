using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets
{
    // a planet weapon fires its whole salvo every FireDelay / planet level, and its offense rating scales with the level
    [TestClass]
    public class PlanetWeaponFireDelayTests : StarDriveTest
    {
        readonly Planet P;
        readonly Building Gun;
        readonly float FireDelay;

        const string EnemyShip = "Dodaving mk1-a"; // planet guns do not target scouts

        public PlanetWeaponFireDelayTests()
        {
            LoadStarterShips(EnemyShip);
            CreateUniverseAndPlayerEmpire();
            P = AddHomeWorldToEmpire(new Vector2(1000), Player);

            Building template = ResourceManager.BuildingsDict.Values.First(b => b.IsWeapon
                && ResourceManager.GetWeaponTemplate(b.Weapon, out IWeaponTemplate w) && w.FireDelay > 2);
            ResourceManager.GetWeaponTemplate(template.Weapon, out IWeaponTemplate weapon);
            FireDelay = weapon.FireDelay;

            Gun = ResourceManager.CreateBuilding(P, template);
            P.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(Gun, P);
        }

        void SetLevel(int level)
        {
            float[] billions = { 0, 0.3f, 1f, 3f, 7f, 12f };
            P.Population = billions[level] * 1000f;
            P.UpdateDevelopmentLevel();
            Assert.AreEqual(level, P.Level, "The test population did not give the planet the level it needs");
        }

        Building PlaceGun(Building template)
        {
            Building gun = ResourceManager.CreateBuilding(P, template);
            P.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(gun, P);
            return gun;
        }

        // an enemy ship next to the planet, which the planet's system knows about; the guns hold fire meanwhile
        Ship EnemyNextToPlanet()
        {
            Ship enemy = SpawnShip(EnemyShip, Enemy, P.Position + new Vector2(P.Radius + 2000f, 0f));
            foreach (Building b in P.Buildings)
                b.WeaponTimer = 1000f;
            RunSimWhile((simTimeout: 1.5, fatal: false));
            Assert.IsTrue(P.System.HostileForcesPresent(Player), "setup: the planet's system does not see the enemy ship");
            return enemy;
        }

        Building SalvoGun(out IWeaponTemplate salvo)
        {
            Building template = ResourceManager.BuildingsDict.Values.First(b => b.IsWeapon
                && ResourceManager.GetWeaponTemplate(b.Weapon, out IWeaponTemplate w) && w.SalvoCount > 1 && !w.IsBeam);
            ResourceManager.GetWeaponTemplate(template.Weapon, out salvo);
            return PlaceGun(template);
        }

        // updates the gun as the planet does; Cycles = times it fired, Shots = projectiles it made
        (int Cycles, int Shots) FireFor(Building gun, float seconds, int stepsPerSecond, float weaponTimer = 0f)
        {
            var step = new FixedSimTime(1f / stepsPerSecond);
            gun.WeaponTimer = weaponTimer;
            int projectilesBefore = UState.Objects.GetProjectiles(null).Length;
            int cycles = 0;
            for (int i = 0; i < (int)Math.Round(seconds * stepsPerSecond); ++i)
            {
                float timer = gun.WeaponTimer;
                gun.UpdateSpaceCombatActions(step, P);
                if (gun.WeaponTimer > timer)
                    ++cycles;
            }
            return (cycles, UState.Objects.GetProjectiles(null).Length - projectilesBefore);
        }

        [TestMethod]
        public void ASalvoGunFiresEveryShotOfItsSalvo()
        {
            SetLevel(1);
            Building gun = SalvoGun(out IWeaponTemplate salvo);
            float delay = gun.ActualFireDelay(P.Level);
            Assert.IsTrue(salvo.SalvoDuration < delay / 2, $"setup: {gun.Name}'s salvo outlasts half its fire delay");
            EnemyNextToPlanet();

            (int cycles, int shots) = FireFor(gun, 4.5f * delay, 60);
            Assert.AreEqual(5, cycles, $"setup: {gun.Name} did not fire every {delay} s");
            Assert.AreEqual(cycles * salvo.SalvoCount * salvo.ProjectileCount, shots,
                $"{gun.Name} did not fire all {salvo.SalvoCount} shots of each salvo");
        }

        [TestMethod]
        public void TheRestOfASalvoIsNotFiredAtATargetThatLeftRange()
        {
            SetLevel(1);
            Building gun = SalvoGun(out IWeaponTemplate salvo);
            Ship enemy = EnemyNextToPlanet();

            (int _, int firstShot) = FireFor(gun, 1f / 60f, 60);
            Assert.AreEqual(salvo.ProjectileCount, firstShot, "setup: the first shot of the salvo was not fired");
            enemy.Position = P.Position + new Vector2(gun.SpaceRange * 2f, 0f);

            (int _, int rest) = FireFor(gun, 1f, 60, weaponTimer: gun.WeaponTimer);
            Assert.AreEqual(0, rest, $"{gun.Name} kept firing its salvo at a ship out of its range");
        }

        [TestMethod]
        public void TheRestOfASalvoIsNotFiredAtADyingTarget()
        {
            SetLevel(1);
            Building gun = SalvoGun(out IWeaponTemplate salvo);
            Ship enemy = EnemyNextToPlanet();

            (int _, int firstShot) = FireFor(gun, 1f / 60f, 60);
            Assert.AreEqual(salvo.ProjectileCount, firstShot, "setup: the first shot of the salvo was not fired");
            enemy.Dying = true; // tumbling for a few seconds before it explodes, still active
            Assert.IsTrue(enemy.Active, "setup: a dying ship is still active");

            (int _, int rest) = FireFor(gun, 1f, 60, weaponTimer: gun.WeaponTimer);
            Assert.AreEqual(0, rest, $"{gun.Name} kept firing its salvo at a dying ship");
        }

        [TestMethod]
        public void AtASlowSimTheGunKeepsItsFireRate()
        {
            const int stepsPerSecond = 10;
            int level = Enumerable.Range(2, 4).First(l =>
            {
                float stepsPerShot = FireDelay / l * stepsPerSecond;
                float part = stepsPerShot - (float)Math.Floor(stepsPerShot);
                return part > 0.25f && part < 0.75f;
            });
            SetLevel(level);
            float delay = Gun.ActualFireDelay(P.Level);
            EnemyNextToPlanet();

            float seconds = 200 * delay; // rounding each delay up to whole steps would lose several shots
            (int cycles, int _) = FireFor(Gun, seconds, stepsPerSecond);
            int expected = (int)((seconds - 1f / stepsPerSecond) / delay) + 1;
            Assert.IsTrue(Math.Abs(cycles - expected) <= 1,
                $"At {stepsPerSecond} sim steps per second {Gun.Name} fired {cycles} times in {seconds:0} s, every {delay:0.###} s would be {expected}");
        }

        [TestMethod]
        public void AGunThatWaitedForATargetDoesNotFireABurst()
        {
            SetLevel(1);
            EnemyNextToPlanet();
            float delay = Gun.ActualFireDelay(P.Level);

            // the timer keeps running down while no target is in range
            (int cycles, int _) = FireFor(Gun, delay * 0.9f, 60, weaponTimer: -10f * delay);
            Assert.AreEqual(1, cycles, $"{Gun.Name} fired {cycles} times within one fire delay after waiting for a target");
        }

        [TestMethod]
        public void AtLevelOneTheGunFiresAtItsOwnDelay()
        {
            SetLevel(1);
            AssertEqual(0.001f, FireDelay, Gun.ActualFireDelay(P.Level),
                $"A level 1 colony fires {Gun.Name} on a flat 1 s instead of its own {FireDelay} s fire delay");
        }

        [TestMethod]
        public void OffenseFollowsTheRealFireRateAtEveryLevel()
        {
            SetLevel(1);
            float offensePerShot = Gun.Offense * Gun.ActualFireDelay(P.Level);
            for (int level = 2; level <= 5; ++level)
            {
                SetLevel(level);
                AssertEqual(0.001f * offensePerShot, offensePerShot, Gun.Offense * Gun.ActualFireDelay(P.Level),
                    $"At level {level} the offense of {Gun.Name} no longer matches how often it really fires");
            }
        }
    }
}
