using System;
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
            ResourceManager.LoadWeapons();
        }

        [TestMethod]
        public void ApplyModsToProjectile()
        {
            Ship ship = Ship.CreateShipAtPoint("Vulcan Scout", EmpireManager.Void, Vector2.Zero);
            Weapon vulcan = ResourceManager.CreateWeapon("VulcanCannon");
            Projectile p = Projectile.Create(vulcan, new Vector2(), Vectors.Up, null, false);
        }
    }
}
