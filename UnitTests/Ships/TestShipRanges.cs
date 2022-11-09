using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

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
            AssertEqual(8000, ship.WeaponsMaxRange);
            AssertEqual(4000, ship.WeaponsMinRange);
            AssertEqual(6000, ship.WeaponsAvgRange);
            AssertEqual(7200, ship.DesiredCombatRange);
            AssertEqual(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
            AssertRanges(ship);
        }

        [TestMethod]
        public void CarrierRanges()
        {
            Ship ship = SpawnShipWithBaseRanges("Heavy Carrier mk5-b", 4000, 8000);
            // including DefaultHangarRange = 7500
            UpdateStatus(ship, CombatState.Artillery);
            AssertEqual(8000, ship.WeaponsMaxRange);
            AssertEqual(7500, ship.WeaponsMinRange);
            AssertEqual(6500, ship.WeaponsAvgRange);
            AssertEqual(7200, ship.DesiredCombatRange);
            AssertEqual(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
            AssertRanges(ship);
        }
        
        [TestMethod]
        public void ShipRangesWithModifiers()
        {
            Ship ship = SpawnShipWithBaseRanges("Dreadnought mk1-a", 4000, 8000);
            Player.WeaponBonuses(WeaponTag.Kinetic).Range = 1;
            Player.WeaponBonuses(WeaponTag.Guided).Range = 1;

            UpdateStatus(ship, CombatState.Artillery);
            AssertEqual(16000, ship.WeaponsMaxRange);
            AssertEqual(8000, ship.WeaponsMinRange);
            AssertEqual(12000, ship.WeaponsAvgRange);
            AssertEqual(14400, ship.DesiredCombatRange);
            AssertEqual(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
        }

        void AssertRanges(Ship ship)
        {
            UpdateStatus(ship, CombatState.Evade);
            AssertEqual(Ship.UnarmedRange, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.HoldPosition);
            AssertEqual(ship.WeaponsMaxRange, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.ShortRange);
            AssertEqual(ship.WeaponsMinRange*0.9f, ship.DesiredCombatRange);
            
            UpdateStatus(ship, CombatState.Artillery);
            AssertEqual(ship.WeaponsMaxRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.BroadsideLeft);
            AssertEqual(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.BroadsideRight);
            AssertEqual(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.OrbitLeft);
            AssertEqual(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.OrbitRight);
            AssertEqual(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.AssaultShip);
            AssertEqual(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
            UpdateStatus(ship, CombatState.OrbitalDefense);
            AssertEqual(ship.WeaponsAvgRange*0.9f, ship.DesiredCombatRange);
        }
    }
}
