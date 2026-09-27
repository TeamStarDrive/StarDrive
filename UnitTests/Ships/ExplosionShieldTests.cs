using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    /// <summary>
    /// Regression coverage for the "LALA" Remnant ship shield bypass: a single explosive round
    /// was (1) draining a whole shield because the lateral splash hit it once per covered grid
    /// cell, and (2) splashing turrets / reactors directly through the shield bubble.
    ///
    /// The rule under test: a blast that lands on an ACTIVE shield stays on the shield layer -
    /// it may spread to other active shields covering the area but never transfers onto a hull
    /// module, and each module is damaged at most once per blast.
    /// </summary>
    [TestClass]
    public class ExplosionShieldTests : StarDriveTest
    {
        public ExplosionShieldTests()
        {
            LoadStarterShips("TEST_ShipShield");
            CreateUniverseAndPlayerEmpire();
        }

        // TEST_ShipShield 4x4 layout:
        //      x0        x1        x2         x3
        // y0   -         Amplifier Amplifier  -
        // y1   -         Cockpit   Cockpit    -
        // y2   Shield    Reactor   Reactor    Shield
        // y3   Reactor   Engine    WarpEngine Reactor

        static bool IsShield(ShipModule m) => m.ShieldPowerMax > 0;

        [TestMethod]
        public void ExplosionOnActiveShield_LeavesHullModulesUndamaged()
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(1000, 0));

            ShipModule shield = ship.GetModuleAt(0, 2);
            Assert.IsTrue(IsShield(shield), "Module at (0,2) should be a shield");
            Assert.IsTrue(shield.ShieldsAreActive, "Shield must be charged for this test");

            const float damage = 200f;
            Assert.IsTrue(shield.ShieldPower >= damage, "Shield must have enough power to absorb the blast");

            var before = new Dictionary<ShipModule, float>();
            foreach (ShipModule m in ship.Modules)
                before[m] = m.Health;

            // Detonate directly on the active shield.
            ship.DamageExplosiveDirectional(attacker, damage, shield, hitRadius: 60f, ignoreShields: false);

            foreach (ShipModule m in ship.Modules)
            {
                if (IsShield(m))
                    continue; // shields legitimately lose POWER (not health) here
                AssertEqual(before[m], m.Health,
                    $"{m.UID} must take NO explosion damage behind an active shield");
            }
        }

        [TestMethod]
        public void ExplosionOnActiveShield_DoesNotDrainOtherShieldPerCell()
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(1000, 0));

            ShipModule nearShield = ship.GetModuleAt(0, 2);
            ShipModule farShield  = ship.GetModuleAt(3, 2);
            Assert.IsTrue(IsShield(nearShield) && IsShield(farShield), "Both (0,2) and (3,2) should be shields");
            Assert.IsTrue(nearShield.ShieldsAreActive && farShield.ShieldsAreActive, "Both shields must be charged");

            const float damage = 200f;
            float farBefore = farShield.ShieldPower;

            ship.DamageExplosiveDirectional(attacker, damage, nearShield, hitRadius: 60f, ignoreShields: false);

            float farLost = farBefore - farShield.ShieldPower;
            // The far shield is inside the blast radius so it takes exactly ONE splash hit. Before
            // the fix it was hit once per covered grid cell and drained by many multiples of this.
            Assert.IsTrue(farLost > 0f, "Far shield should take one splash hit (it is within the blast radius)");
            Assert.IsTrue(farLost <= damage * 0.5f + 1f,
                $"Far shield should lose at most one splash hit (<= {damage * 0.5f}), but lost {farLost}");
        }

        [TestMethod]
        public void ExplosionThatDrainsEntryShield_StillContainsSplashToShieldLayer()
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(1000, 0));

            ShipModule shield = ship.GetModuleAt(0, 2);
            Assert.IsTrue(IsShield(shield) && shield.ShieldsAreActive, "Entry must be a charged shield");

            const float damage = 100f;
            // Trim the entry shield to exactly the blast size so the penetration column drains it to
            // zero THIS blast. It was still live at impact, so the splash must stay on the shield
            // layer - regression for the ordering bug where the shield-state was read AFTER
            // penetration had already zeroed it, making the splash treat it as a bare-hull hit.
            shield.DamageShield(shield.ShieldPower - damage, null, out _);
            AssertEqual(1f, damage, shield.ShieldPower, "precondition: entry shield trimmed to the blast size");

            var before = new Dictionary<ShipModule, float>();
            foreach (ShipModule m in ship.Modules)
                before[m] = m.Health;

            ship.DamageExplosiveDirectional(attacker, damage, shield, hitRadius: 60f, ignoreShields: false);

            Assert.IsTrue(shield.ShieldPower <= 1f, "precondition: penetration should have drained the entry shield");
            foreach (ShipModule m in ship.Modules)
            {
                if (IsShield(m))
                    continue;
                AssertEqual(before[m], m.Health,
                    $"{m.UID} must stay undamaged - the blast struck a LIVE shield, so splash stays on the shield layer");
            }
        }

        [TestMethod]
        public void ExplosionOverwhelmingShield_AttenuatesOverflowByBubbleDepth()
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(1000, 0));

            ShipModule shield = ship.GetModuleAt(0, 2);
            ShipModule farShield = ship.GetModuleAt(3, 2);
            ShipModule behind = ship.GetModuleAt(1, 2);
            Assert.IsTrue(IsShield(shield) && shield.ShieldsAreActive, "(0,2) must be a charged shield");
            Assert.IsFalse(IsShield(behind), "(1,2) must be a hull module behind the shield");

            const float hitRadius = 60f;
            const float shieldLeft = 20f;
            // Drop the OTHER shield first: its bubble also covers the module behind, and an active
            // shield over a cell re-absorbs the overflow (shield-layer containment). With it down,
            // the module is genuinely exposed once the entry shield is overwhelmed.
            farShield.DamageShield(farShield.ShieldPower, null, out _);
            Assert.IsFalse(farShield.ShieldsAreActive, "far shield must be down so it can't re-absorb the overflow");
            shield.DamageShield(shield.ShieldPower - shieldLeft, null, out _);

            // Replicate the production attenuation to size a non-flaky scenario: the overflow that
            // breaches the shield decays from a point one bubble-radius out along the flight path,
            // over a (bubble + blast) range.
            Vector2 dir = shield.Position.DirectionToTarget(ship.Position);
            Vector2 impact = shield.Position - dir * shield.ShieldHitRadius;
            float falloff = ShipModule.DamageFalloff(impact, behind.Position, shield.ShieldHitRadius + hitRadius, behind.Radius);
            Assert.IsTrue(falloff is > 0.01f and < 0.5f, $"precondition: bubble should heavily attenuate (falloff={falloff:0.00})");

            // Size the excess so the ATTENUATED overflow is ~half the module's health: the module
            // survives - which proves the attenuation, because the RAW excess is several times its
            // health and would have destroyed it outright without the bubble-depth falloff.
            float behindBefore = behind.Health;
            float excess = behindBefore * 0.5f / falloff; // > behindBefore since falloff < 0.5
            Assert.IsTrue(excess > behindBefore, "precondition: raw excess would destroy the module without attenuation");

            ship.DamageExplosiveDirectional(attacker, shieldLeft + excess, shield, hitRadius: hitRadius, ignoreShields: false);

            Assert.IsTrue(behind.Active && behind.Health > 0f,
                "module behind must SURVIVE - the bubble attenuates the overflow far below the raw excess that would have destroyed it");
            Assert.IsTrue(behind.Health < behindBefore,
                "but the attenuated overflow should still reach and damage the module");
        }

        [TestMethod]
        public void ShieldPiercingExplosion_StillReachesHullModules()
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(1000, 0));

            ShipModule reactor = ship.GetModuleAt(1, 2);
            Assert.IsFalse(IsShield(reactor), "Module at (1,2) should not be a shield");

            float reactorBefore = reactor.Health;

            // A shield-ignoring explosive (e.g. a piercing Remnant weapon) entering on the reactor:
            // the shield-aware splash logic must NOT block these - they still reach the hull.
            ship.DamageExplosiveDirectional(attacker, 300f, reactor, hitRadius: 60f, ignoreShields: true);

            Assert.IsTrue(reactor.Health < reactorBefore,
                "Shield-piercing explosion must still damage the hull module it struck");
        }

        // Solar radiation picks one shield per covered module and damages it once. The pick is
        // meant to be the strongest shield covering that module; while maxPower was never raised
        // inside the loop it was whichever one came last in slot order, so a nearly drained
        // bubble could soak the radiation while a full one beside it stood untouched.
        [TestMethod]
        public void ShieldCoverPicksTheStrongestShield()
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);

            ShipModule[] shields = ship.GetActiveShields().ToArr();
            AssertEqual(2, shields.Length, "setup: TEST_ShipShield should have two active shields");

            ShipModule covered = null;
            foreach (ShipModule m in ship.Modules)
            {
                if (shields[0].HitTestShield(m.Position, m.Radius)
                    && shields[1].HitTestShield(m.Position, m.Radius))
                {
                    covered = m;
                    break;
                }
            }
            Assert.IsNotNull(covered, "setup: needs a module both shields cover");

            // drain the LAST shield in slot order, so 'last one wins' would pick the weaker
            shields[1].DamageShield(shields[1].ShieldPower * 0.5f, null, out _);
            Assert.IsTrue(shields[0].ShieldPower > shields[1].ShieldPower,
                "setup: the first shield in slot order must be the stronger one");

            Assert.IsTrue(ship.IsCoveredByShield(covered, out ShipModule picked));
            AssertEqual(shields[0], picked,
                "the strongest covering shield must absorb, not whichever came last");
        }
    }
}
