using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShipRanges : StarDriveTest
    {
        public TestShipRanges()
        {
            LoadStarterShips("Heavy Carrier mk5-b", "Dreadnought mk1-a");
            CreateUniverseAndPlayerEmpire();
        }

        void UpdateStatus(Ship ship, CombatState state)
        {
            ship.AI.CombatState = state;
            ship.ShipStatusChanged = true;
            ship.Update(new FixedSimTime(1f));
        }

        TestShip SpawnShipWithBaseRanges(string name, float minValue, float maxValue)
        {
            TestShip ship = SpawnShip(name, Player, Vector2.Zero);
            bool useMin = false;
            var activeWeapons = ship.ActiveWeapons;
            foreach (WeaponTestWrapper w in activeWeapons)
            {
                w.TestBaseRange = useMin ? minValue : maxValue;
                useMin = !useMin;
            }
            return ship;
        }

        [TestMethod]
        public void ShipRanges()
        {
            Ship ship = SpawnShipWithBaseRanges("Dreadnought mk1-a", 4000, 8000);
            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(8000, ship.WeaponsMaxRange);
            Assert.That.Equal(4000, ship.WeaponsMinRange);
            Assert.That.Equal(6000, ship.WeaponsAvgRange);
            Assert.That.Equal(7200, ship.DesiredCombatRange);
            Assert.That.Equal(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
            AssertRanges(ship);
        }

        [TestMethod]
        public void CarrierRanges()
        {
            Ship ship = SpawnShipWithBaseRanges("Heavy Carrier mk5-b", 4000, 8000);
            // including DefaultHangarRange = 7500
            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(8000, ship.WeaponsMaxRange);
            Assert.That.Equal(7500, ship.WeaponsMinRange);
            Assert.That.Equal(6500, ship.WeaponsAvgRange);
            Assert.That.Equal(7200, ship.DesiredCombatRange);
            Assert.That.Equal(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
            AssertRanges(ship);
        }
        
        [TestMethod]
        public void ShipRangesWithModifiers()
        {
            Ship ship = SpawnShipWithBaseRanges("Dreadnought mk1-a", 4000, 8000);
            Player.WeaponBonuses(WeaponTag.Kinetic).Range = 1;
            Player.WeaponBonuses(WeaponTag.Guided).Range = 1;

            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(16000, ship.WeaponsMaxRange);
            Assert.That.Equal(8000, ship.WeaponsMinRange);
            Assert.That.Equal(12000, ship.WeaponsAvgRange);
            Assert.That.Equal(14400, ship.DesiredCombatRange);
            Assert.That.Equal(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
        }

        void AssertRanges(Ship ship)
        {
            UpdateStatus(ship, CombatState.Evade);
            Assert.That.Equal(Ship.UnarmedRange, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.HoldPosition);
            Assert.That.Equal(ship.WeaponsMaxRange, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.ShortRange);
            Assert.That.Equal(ship.WeaponsMinRange*0.9f, ship.DesiredCombatRange);
            
            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(ship.WeaponsMaxRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.BroadsideLeft);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.BroadsideRight);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.OrbitLeft);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.OrbitRight);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.AssaultShip);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.OrbitalDefense);
            Assert.That.Equal(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
        }
    }
}
