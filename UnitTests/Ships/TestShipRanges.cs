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
            // Excalibur class has all the bells and whistles
            LoadStarterShips(new[]{ "Excalibur-Class Supercarrier" });
            CreateGameInstance();
        }

        void CreateTestEnv(out Empire empire, out Ship ship)
        {
            CreateUniverseAndPlayerEmpire(out empire);
            ship = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", empire, Vector2.Zero);
        }

        void UpdateStatus(Ship ship, CombatState state)
        {
            ship.AI.CombatState = state;
            ship.shipStatusChanged = true;
            ship.Update(1f);
        }

        [TestMethod]
        public void TestShipRangeModifiers()
        {
            CreateTestEnv(out Empire empire, out Ship ship);

            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(4000, ship.WeaponsMaxRange);
            Assert.That.Equal(1000, ship.WeaponsMinRange);
            Assert.That.Equal(2200, ship.WeaponsAvgRange);
            Assert.That.Equal(4000, ship.DesiredCombatRange);
            Assert.That.Equal(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
            
            WeaponTagModifier kinetic = empire.WeaponBonuses(WeaponTag.Kinetic);
            WeaponTagModifier guided = empire.WeaponBonuses(WeaponTag.Guided);
            kinetic.Range = 1;
            guided.Range = 1;

            UpdateStatus(ship, CombatState.Artillery);
            Assert.That.Equal(8000, ship.WeaponsMaxRange);
            Assert.That.Equal(2000, ship.WeaponsMinRange);
            Assert.That.Equal(4400, ship.WeaponsAvgRange);
            Assert.That.Equal(8000, ship.DesiredCombatRange);
            Assert.That.Equal(ship.OffensiveWeapons.Average(w => w.ProjectileSpeed), ship.InterceptSpeed);
            
            UpdateStatus(ship, CombatState.HoldPosition);
            Assert.That.Equal(ship.WeaponsMaxRange, ship.DesiredCombatRange);

            UpdateStatus(ship, CombatState.Evade);
            Assert.That.Equal(10000, ship.DesiredCombatRange);
        }
    }
}
