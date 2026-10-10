using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    /// <summary>
    /// Covers what a dying ship does to the ships around it. SpatialManager.ShipExplode is the
    /// only blast path that crosses hulls, and before these tests a single death could wipe a
    /// whole formation of capitals: the blast entered on whichever module happened to be
    /// geometrically nearest, which was an internal one whenever the hulls overlapped, and the
    /// falloff still delivered two thirds of the damage at contact range.
    /// </summary>
    [TestClass]
    public class AdjacentShipExplosionTests : StarDriveTest
    {
        public AdjacentShipExplosionTests()
        {
            LoadStarterShips("Dreadnought mk1-a", "Corsair", "Seeder Transport", "Terran-Prototype", "Vulcan Scout",
                             "Hunter mk1-a", "Shipyard");
            CreateUniverseAndPlayerEmpire();
        }

        static readonly MethodInfo GetExplosionDamageMethod =
            typeof(Ship).GetMethod("GetExplosionDamage", BindingFlags.Instance | BindingFlags.NonPublic);

        static float BlastDamage(Ship s) => (float)GetExplosionDamageMethod.Invoke(s, null);

        /// <summary>
        /// Destroys every module carrying ExplosiveResist. Those modules subtract from the ship's
        /// own death blast, so killing them is the state that produces the largest possible one.
        /// </summary>
        static void StripResistArmor(Ship s)
        {
            foreach (ShipModule m in s.Modules)
                if (m.ExplosiveResist > 0f && m.Active)
                    m.Die(null, cleanupOnly: true);
        }

        static void DrainShields(Ship s)
        {
            foreach (ShipModule m in s.GetShields())
                m.DamageShield(m.ShieldPower, null, out float _);
        }

        int TrialSite;

        Vector2 NextSite() => new(++TrialSite * 200000f, 0);

        TestShip[] SpawnStack(string shipName, int count, float spacing)
        {
            var stack = new TestShip[count];
            Vector2 site = NextSite();
            for (int i = 0; i < count; ++i)
                stack[i] = SpawnShip(shipName, Player, site + new Vector2(i * spacing, 0));
            UState.Objects.Update(TestSimStep);
            return stack;
        }

        [TestMethod]
        public void BlastKeepsItsFloorWhenTheShipHasNothingToGive()
        {
            TestShip ship = SpawnShip("Dreadnought mk1-a", Player, NextSite());
            AssertEqual(1f, ship.Radius * 10f, BlastDamage(ship),
                "An intact ship's armour cancels its reactors, so the blast sits on the floor");
        }

        [TestMethod]
        public void ExplosionFalloffHalvesAtTheReferenceDistance()
        {
            AssertEqual(0.001f, 100f / 110f, ShipModule.ExplosionFalloff(0f),
                "at the blast center the distance floor still applies");
            AssertEqual(0.001f, 0.5f, ShipModule.ExplosionFalloff(ShipModule.ExplosionHalfDamageDistance));
            AssertEqual(0.001f, 0.25f, ShipModule.ExplosionFalloff(300f));

            float previous = float.MaxValue;
            for (float d = 0; d <= 4000f; d += 50f)
            {
                float f = ShipModule.ExplosionFalloff(d);
                AssertGreaterThan(f, 0f, $"falloff must stay positive at {d}");
                Assert.IsTrue(f <= 1f, $"falloff must never exceed 1 at {d}, got {f}");
                Assert.IsTrue(f <= previous, $"falloff must not rise at {d}");
                previous = f;
            }
        }

        [TestMethod]
        public void ExplosionFalloffCannotSpikeAtZeroDistance()
        {
            // Two hulls can occupy the same point, and a raw 1/d would divide by ~0 there.
            AssertEqual(0.0001f, ShipModule.ExplosionFalloff(ShipModule.ExplosionMinDistance),
                        ShipModule.ExplosionFalloff(0f));
            AssertEqual(0.0001f, ShipModule.ExplosionFalloff(ShipModule.ExplosionMinDistance),
                        ShipModule.ExplosionFalloff(-5f));
        }

        [TestMethod]
        public void BlastAlwaysEntersThroughAnExternalModule()
        {
            TestShip target = SpawnShip("Dreadnought mk1-a", Player, NextSite());
            UState.Objects.Update(TestSimStep);

            // Sweep the blast from well outside the hull to its exact center. The last two sit
            // inside the grid, where the nearest module is a turret and an ordnance store.
            foreach (float offsetCells in new[] { 40f, 20f, 10f, 5f, 0f })
            {
                Vector2 center = target.Position + new Vector2(offsetCells * 16f, 0);
                ShipModule entry = target.FindBlastEntryModule(center);

                Assert.IsNotNull(entry, $"no entry module found at offset {offsetCells} cells");
                Assert.IsTrue(entry.IsExternal,
                    $"blast at offset {offsetCells} cells entered through internal module " +
                    $"'{entry.UID}' at grid {entry.Pos.X},{entry.Pos.Y}");
            }
        }

        [TestMethod]
        public void StackedFreightersSurviveOneOfThemExploding()
        {
            // Seeder Transport carries no ExplosiveResist at all, so nothing subtracts from its
            // blast and it emits the full value at full health. 135 vanilla designs are built
            // this way, and they are the ships that actually pile on top of each other.
            const int count = 8;
            for (int trial = 0; trial < 5; ++trial)
            {
                TestShip[] stack = SpawnStack("Seeder Transport", count, spacing: 8f);
                for (int i = 1; i < count; ++i)
                    DrainShields(stack[i]);

                stack[0].Die(null, cleanupOnly: false);

                int dead = stack.Skip(1).Count(s => !s.Active);
                AssertEqual(0, dead, $"trial {trial}: a freighter's death blast destroyed {dead} " +
                                     $"of {count - 1} stacked neighbours");
            }
        }

        [TestMethod]
        public void SmallHullsGetATighterBlastCapThanCapitals()
        {
            TestShip capital = SpawnShip("Dreadnought mk1-a", Player, NextSite());
            TestShip freighter = SpawnShip("Seeder Transport", Player, NextSite());

            AssertEqual(1f, capital.Radius * 100f, capital.ExplosionDamageCap());
            AssertEqual(1f, freighter.Radius * 15f, freighter.ExplosionDamageCap());

            // The cap must never sit under the floor, or Clamped would invert.
            foreach (Ship s in new Ship[] { capital, freighter })
                AssertGreaterThan(s.ExplosionDamageCap(), s.Radius * 10f,
                    $"{s.Name}: blast cap must stay above the Radius*10 floor");
        }

        static float RadiusOf(string shipName)
        {
            ResourceManager.GetShipTemplate(shipName, out Ship template);
            return template.Radius;
        }

        [TestMethod]
        public void OverlappingCapitalsSurviveOneOfThemExploding()
        {
            // overlapping, but each one more than a tenth of a radius from the next, so none is stacked
            const int count = 8;
            float spacing = RadiusOf("Dreadnought mk1-a") * 0.15f;
            for (int trial = 0; trial < 5; ++trial)
            {
                TestShip[] stack = SpawnStack("Dreadnought mk1-a", count, spacing);
                StripResistArmor(stack[0]);
                for (int i = 1; i < count; ++i)
                    DrainShields(stack[i]);

                stack[0].Die(null, cleanupOnly: false);

                int dead = stack.Skip(1).Count(s => !s.Active);
                AssertEqual(0, dead, $"trial {trial}: a capital's death blast destroyed {dead} " +
                                     $"of {count - 1} overlapping neighbours");
            }
        }

        [TestMethod]
        public void AStationFrigateOrBiggerStacksOnACruiserOrBigger()
        {
            Vector2 site = NextSite();
            TestShip capital = SpawnShip("Dreadnought mk1-a", Player, site);
            TestShip cruiser = SpawnShip("Terran-Prototype", Player, site);
            TestShip frigate = SpawnShip("Corsair", Player, site);
            TestShip corvette = SpawnShip("Hunter mk1-a", Player, site);
            TestShip fighter = SpawnShip("Vulcan Scout", Player, site);
            TestShip freighter = SpawnShip("Seeder Transport", Player, site);
            TestShip station = SpawnShip("Shipyard", Player, site);
            var hullRoles = new[] { capital, cruiser, frigate, corvette, fighter, freighter, station }
                .Select(s => s.ShipData.HullRole).ToArray();
            CollectionAssert.AreEqual(new[] { RoleName.capital, RoleName.cruiser, RoleName.frigate, RoleName.corvette,
                                              RoleName.fighter, RoleName.freighter, RoleName.station }, hullRoles,
                                      "setup: the hull role of each test ship");

            AssertTrue(capital.IsStackedOnExplosion(cruiser, site), "a cruiser exploding on a capital");
            AssertTrue(cruiser.IsStackedOnExplosion(capital, site), "a capital exploding on a cruiser");
            AssertTrue(capital.IsStackedOnExplosion(frigate, site), "a frigate exploding on a capital");
            AssertTrue(capital.IsStackedOnExplosion(station, site), "a station exploding on a capital");
            AssertFalse(capital.IsStackedOnExplosion(corvette, site), "a corvette's blast is always raycast");
            AssertFalse(capital.IsStackedOnExplosion(fighter, site), "a fighter's blast is always raycast");
            AssertFalse(capital.IsStackedOnExplosion(freighter, site), "a freighter's blast is always raycast");
            AssertFalse(frigate.IsStackedOnExplosion(capital, site), "a frigate is always raycast");
            AssertFalse(fighter.IsStackedOnExplosion(capital, site), "a fighter is always raycast");
            AssertFalse(freighter.IsStackedOnExplosion(capital, site), "a freighter is always raycast");
            AssertFalse(station.IsStackedOnExplosion(capital, site), "a station is always raycast");
        }

        [TestMethod]
        public void AShipIsStackedOnlyWithinATenthOfItsOwnRadius()
        {
            Vector2 site = NextSite();
            TestShip exploding = SpawnShip("Dreadnought mk1-a", Player, site);
            float cruiserRadius = RadiusOf("Terran-Prototype");
            TestShip inside = SpawnShip("Terran-Prototype", Player, site + new Vector2(cruiserRadius * 0.09f, 0));
            TestShip outside = SpawnShip("Terran-Prototype", Player, site + new Vector2(cruiserRadius * 0.11f, 0));
            AssertTrue(inside.IsStackedOnExplosion(exploding, site), "9% of the cruiser's radius away");
            AssertFalse(outside.IsStackedOnExplosion(exploding, site), "11% of the cruiser's radius away");

            float smallerOffset = cruiserRadius * 0.2f;
            AssertGreaterThan(exploding.Radius * 0.1f, smallerOffset, "setup: within a tenth of the capital's radius");
            TestShip smaller = SpawnShip("Terran-Prototype", Player, site + new Vector2(smallerOffset, 0));
            AssertFalse(smaller.IsStackedOnExplosion(exploding, site), "the checked ship's radius sets the range");
        }

        [TestMethod]
        public void AStackedShipDodgesHalfAsOften()
        {
            TestShip cruiser = SpawnShip("Terran-Prototype", Player, NextSite());
            foreach (bool pointBlank in new[] { false, true })
            {
                float normal = cruiser.ExplosionEvadeBaseChance(pointBlank, stacked: false);
                AssertGreaterThan(normal, 0f, "setup: a cruiser can dodge");
                AssertEqual(0.001f, normal * 0.5f, cruiser.ExplosionEvadeBaseChance(pointBlank, stacked: true),
                            $"pointBlank={pointBlank}");
            }
        }

        [TestMethod]
        public void ABlastOnTopOfAStackedShipGoesOffInsideIt()
        {
            Vector2 site = NextSite();
            TestShip dying = SpawnShip("Dreadnought mk1-a", Player, site);
            TestShip stacked = SpawnShip("Dreadnought mk1-a", Player, site + new Vector2(8f, 0));
            UState.Objects.Update(TestSimStep);
            DrainShields(stacked);

            ShipModule core = stacked.FindClosestModule(site);
            ShipModule plate = stacked.FindBlastEntryModule(site);
            AssertTrue(stacked.IsStackedOnExplosion(dying, site), "setup: the two hulls are stacked");
            AssertFalse(core.IsExternal, "setup: the module nearest the blast is internal");
            AssertTrue(plate.IsExternal, "setup: the raycast would enter through an armour plate");

            dying.Die(null, cleanupOnly: false);
            AssertGreaterThan(core.ActualMaxHealth, core.Health, $"the blast goes off at {core.UID}, nearest to it");
            AssertEqual(0.001f, plate.ActualMaxHealth, plate.Health, $"the blast does not enter through {plate.UID}");
        }

        [TestMethod]
        public void ExplosionStillHurtsWhatItCannotKill()
        {
            // The blast must not be tuned into nothing: the entry and falloff changes are there to
            // stop the formation wipe, not to make a dying capital harmless to what it lands on.
            int killed = 0;
            for (int trial = 0; trial < 20; ++trial)
            {
                Vector2 site = NextSite();
                TestShip victim = SpawnShip("Dreadnought mk1-a", Player, site);
                TestShip frigate = SpawnShip("Corsair", Player, site + new Vector2(64f, 0));
                UState.Objects.Update(TestSimStep);
                StripResistArmor(victim);
                DrainShields(frigate);

                victim.Die(null, cleanupOnly: false);
                if (!frigate.Active)
                    ++killed;
            }

            // A capital's armour shrugs off its own class's blast, which is why the overlapping-capital
            // test above expects no losses. A frigate has nothing to absorb it with.
            AssertGreaterThan(killed, 0,
                "a capital dying beside a frigate must still destroy it - the blast has been " +
                "tuned down to nothing");
        }

        [TestMethod]
        public void BlastStillBitesNearTheEdgeOfItsRadius()
        {
            // The old (1-d/R)^2 curve collapsed to almost nothing near the blast edge - at 1100u
            // it delivered 483 of a 60k blast where the inverse curve delivers 5033. This pins
            // ShipExplode to the inverse curve: the blast hits softer up close but reaches out.
            // The frigate is outside point-blank range here, so it keeps its full evade and
            // dodges half the time - the trial count has to swamp that or the test flakes.
            int killed = 0;
            for (int trial = 0; trial < 30; ++trial)
            {
                Vector2 site = NextSite();
                TestShip victim = SpawnShip("Dreadnought mk1-a", Player, site);
                TestShip frigate = SpawnShip("Corsair", Player, site + new Vector2(1100f, 0));
                UState.Objects.Update(TestSimStep);
                StripResistArmor(victim);
                DrainShields(frigate);

                victim.Die(null, cleanupOnly: false);
                if (!frigate.Active)
                    ++killed;
            }

            AssertGreaterThan(killed, 0,
                "a frigate near the edge of a capital's blast radius should still be destroyed " +
                "at least once in 30 tries");
        }

        [TestMethod]
        public void AShipShotToDeathProducesOnlyTheFloorBlast()
        {
            // A ship dies when its INTERNAL slots drop below ShipDestroyThreshold, and resist
            // armour is external, so combat never strips it. The 29x blast a stripped ship
            // produces is real but unreachable this way - worth pinning so it stays that way.
            TestShip ship = SpawnShip("Dreadnought mk1-a", Player, NextSite());
            TestShip shooter = SpawnShip("Corsair", Enemy, NextSite());
            UState.Objects.Update(TestSimStep);
            DrainShields(ship);

            float volley = ship.Radius * 8f;
            float blastAtDeath = BlastDamage(ship);
            for (int shots = 0; ship.Active && shots < 4000; ++shots)
            {
                blastAtDeath = BlastDamage(ship);
                float angle = shots * 0.37f;
                var dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 impact = ship.Position + dir * ship.Radius;
                ShipModule hit = ship.RayHitTestSingle(impact + dir * 100f, ship.Position, true);
                hit?.GetParent().DamageExplosive(shooter, volley, hit.Position, 32f, true);
            }

            Assert.IsFalse(ship.Active, "the ship should have been shot to death");
            AssertEqual(1f, ship.Radius * 10f, blastAtDeath,
                "a ship killed by weapons fire still has most of its armour, so it blasts at the floor");
            AssertGreaterThan(ship.Modules.Count(m => m.ExplosiveResist > 0f && m.Active), 0,
                "resist armour must still be standing when the ship dies");
        }
    }
}
