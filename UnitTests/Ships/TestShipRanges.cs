using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShipRanges : StarDriveTest, IDisposable
    {
        readonly GameDummy Game;

        public TestShipRanges()
        {
            // Excalibur class has all the bells and whistles
            ResourceManager.LoadStarterShipsForTesting(new[]{ "Excalibur-Class Supercarrier" });
            // UniverseScreen requires a game instance
            Game = new GameDummy();
            Game.Create();
        }

        public void Dispose()
        {
            Empire.Universe?.ExitScreen();
            Empire.Universe = null;
            Game.Dispose();
        }

        void CreateTestEnv(out Empire empire, out Ship ship)
        {
            var data = new UniverseData();
            empire = EmpireManager.CreateNewEmpire("ShipRangesEmpire");
            data.EmpireList.Add(empire);
            Empire.Universe = new UniverseScreen(data, empire);

            ship = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", empire, Vector2.Zero);
        }


        [TestMethod]
        public void TestShipRangeModifiers()
        {
            CreateTestEnv(out Empire empire, out Ship ship);
            
            Assert.That.Equal(4000, ship.WeaponsMaxRange);
            Assert.That.Equal(1000, ship.WeaponsMinRange);
            Assert.That.Equal(2200, ship.WeaponsAvgRange);
            
            WeaponTagModifier kinetic = empire.WeaponBonuses(WeaponTag.Kinetic);
            WeaponTagModifier guided = empire.WeaponBonuses(WeaponTag.Guided);
            kinetic.Range = 1;
            guided.Range = 1;

            ship.shipStatusChanged = true;
            ship.Update(0.016f);

            Assert.That.Equal(8000, ship.WeaponsMaxRange);
            Assert.That.Equal(2000, ship.WeaponsMinRange);
            Assert.That.Equal(4400, ship.WeaponsAvgRange);

        }
    }
}
