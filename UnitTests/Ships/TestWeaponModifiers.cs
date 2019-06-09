using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestWeaponModifiers
    {
        public TestWeaponModifiers()
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
            ResourceManager.LoadStarterShipsForTesting();
        }

        [TestMethod]
        public void ApplyModsToProjectile()
        {
            Ship ship = Ship.CreateShipAtPoint("Vulcan Scout", EmpireManager.Void, Vector2.Zero);
            Weapon vulcan = ship.Weapons.Find(w => w.UID == "VulcanCannon");

            Projectile p = Projectile.Create(vulcan, new Vector2(), Vectors.Up, null, false);
        }
    }
}
