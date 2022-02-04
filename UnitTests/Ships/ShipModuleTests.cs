using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipModuleTests : StarDriveTest
    {
        public ShipModuleTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        public static void AssertAreEqual(DesignSlot expected, DesignSlot actual)
        {
            Assert.AreEqual(expected.Pos, actual.Pos);
            Assert.AreEqual(expected.ModuleUID, actual.ModuleUID);
            Assert.AreEqual(expected.Size, actual.Size);
            Assert.AreEqual(expected.TurretAngle, actual.TurretAngle, 0.001f);
            Assert.AreEqual(expected.ModuleRot, actual.ModuleRot);
            Assert.AreEqual(expected.HangarShipUID, actual.HangarShipUID);
        }

        public static void AssertAreEqual(ModuleSaveData expected, ModuleSaveData actual)
        {
            AssertAreEqual(expected as DesignSlot, actual as DesignSlot);

            Assert.AreEqual(expected.Health, actual.Health);
            Assert.AreEqual(expected.ShieldPower, actual.ShieldPower);
            Assert.AreEqual(expected.HangarShipId, actual.HangarShipId);
        }

        public static void AssertAreEqual(ShipModule expected, ShipModule actual)
        {
            Assert.AreEqual(expected.Position, actual.Position);
            Assert.AreEqual(expected.UID, actual.UID);
            Assert.AreEqual(expected.HangarShipUID, actual.HangarShipUID);
            Assert.AreEqual(expected.TurretAngle, actual.TurretAngle);
            Assert.AreEqual(expected.ModuleRot, actual.ModuleRot);
            Assert.AreEqual(expected.Restrictions, actual.Restrictions); // TODO

            Assert.AreEqual(expected.Health, actual.Health, 0.001f);
            Assert.AreEqual(expected.ShieldPower, actual.ShieldPower, 0.001f);
            Assert.AreEqual(expected.HangarShipId, actual.HangarShipId);
        }

        [TestMethod]
        public void CreateModule_WithWeapon()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, new Vector2(1000, 1000));
            var m = ShipModule.Create(null, new DesignSlot(new Point(1, 2), "LaserBeam2x3", new Point(3,2), 45, ModuleOrientation.Left, null), ship, false);
            Assert.AreEqual("LaserBeam2x3", m.UID);
            Assert.AreEqual(new Point(3,2), m.GetSize()); // DesignSlot size is always already oriented
            Assert.AreEqual(new Point(1, 2), m.Pos, "Module Grid Position was not set");

            Assert.AreEqual(new Point(2,2), ship.BaseHull.GridCenter, "Following calculations require GridCenter 2,2");
            Assert.AreEqual(new Vector2(8, 16), m.LocalCenter);
            Assert.AreEqual(new Vector2(1008, 1016), m.Position, "Initial module position should be offset from ship center");

            Assert.AreEqual(45, m.TurretAngle);
            Assert.AreEqual(ModuleOrientation.Left, m.ModuleRot);
            Assert.AreEqual(m.ActualMaxHealth, m.Health);
            Assert.That.GreaterThan(m.TargetValue, 1, "Weapon module LaserBeam2x3 should have a reasonably high target value");
            Assert.AreEqual(3 * 11.5f, m.Radius, "Bounding Radius should use the biggest Size Axis");

            Assert.IsNotNull(m.InstalledWeapon);
            Assert.AreEqual("FocusLaserBeam", m.WeaponType);
            Assert.AreNotSame(m.InstalledWeapon, ResourceManager.GetWeaponTemplate(m.WeaponType), "Installed weapon was not cloned! This is a bug!");

            Assert.AreEqual(m, m.InstalledWeapon.Module, "Installed weapon Module ref was not set");
            Assert.AreEqual(ship, m.InstalledWeapon.Owner, "Installed weapon Owner ref was not set");
        }

        
        [TestMethod]
        public void Module_Uninstall()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, new Vector2(1000, 1000));
            var m = ShipModule.Create(null, new DesignSlot(new Point(1, 2), "LaserBeam2x3", new Point(3,2), 45, ModuleOrientation.Left, null), ship, false);
            m.IsExternal = true;
            m.Powered = true;

            m.UninstallModule();
            Assert.IsFalse(m.IsExternal, "isExternal must be reset after module uninstall");
            Assert.IsFalse(m.Powered, "Powered must be reset after module uninstall");
        }

        [TestMethod]
        public void New_DesignSlot()
        {
            var hs = new HullSlot(4, 8, Restrictions.IOE);
            Assert.AreEqual(new Point(4, 8), hs.Pos);
            Assert.AreEqual(Restrictions.IOE, hs.R);

            var slot = new DesignSlot(hs.Pos, "FighterBay", new Point(3,4),
                                      180, ModuleOrientation.Rear, "Vulcan Scout");

            Assert.AreEqual(new Point(4, 8), slot.Pos);
            Assert.AreEqual("FighterBay", slot.ModuleUID);
            Assert.AreEqual(180, slot.TurretAngle);
            Assert.AreEqual(ModuleOrientation.Rear, slot.ModuleRot);
            Assert.AreEqual("Vulcan Scout", slot.HangarShipUID);
        }

        [TestMethod]
        public void SaveGame_ShipModule_To_ModuleSaveData()
        {
            var ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);

            var dData = new DesignSlot(new Point(4,8), "FighterBay", new Point(3,4), 180, ModuleOrientation.Rear, "Vulcan Scout");
            var oData = new ModuleSaveData(dData, health:555, shieldPower:0, 0);
            var original = ShipModule.Create(null, oData, ship);

            var data = new ModuleSaveData(original);
            AssertAreEqual(oData, data);

            var recreated = ShipModule.Create(null, data, ship);
            AssertAreEqual(original, recreated);
        }

        [TestMethod]
        public void ShipDesign_DesignSlot_To_DesignSlotString()
        {
            var slot1 = new DesignSlot(new Point(4,8), "FighterBay", new Point(1,1), 0, ModuleOrientation.Normal, null);
            Assert.AreEqual("4,8;0", ShipDesign.DesignSlotString(slot1, 0), "Expected gridX,gridY;moduleIdx");

            var slot2 = new DesignSlot(new Point(4,8), "FighterBay", new Point(2,4), 0, ModuleOrientation.Normal, null);
            Assert.AreEqual("4,8;0;2,4", ShipDesign.DesignSlotString(slot2, 0), "Expected gridX,gridY;moduleIdx;sizeX,sizeY");

            var slot3 = new DesignSlot(new Point(4,8), "FighterBay", new Point(1,1), 0, ModuleOrientation.Normal, "Vulcan Scout");
            Assert.AreEqual("4,8;0;;;;Vulcan Scout", ShipDesign.DesignSlotString(slot3, 0),
                            "Expected gridX,gridY;moduleIdx;sizeX,sizeY;turretAngle;moduleRot;hangarShip");

            var slot4 = new DesignSlot(new Point(4,8), "FighterBay", new Point(4,3), 123, ModuleOrientation.Left, "Vulcan Scout");
            Assert.AreEqual("4,8;1;4,3;123;1;Vulcan Scout", ShipDesign.DesignSlotString(slot4, 1),
                            "Expected gridX,gridY;moduleIdx;sizeX,sizeY;turretAngle;moduleRot;hangarShip");
        }

        static DesignSlot ParseDesignSlot(string text, string[] moduleUIDs)
        {
            return ShipDesign.ParseDesignSlot(new StringView(text.ToCharArray()), moduleUIDs);
        }

        [TestMethod]
        public void ShipDesign_ParseDesignSlot()
        {
            string[] moduleUIDs = { "FighterBay" };
            var slot1 = new DesignSlot(new Point(4,8), "FighterBay", new Point(1,1), 0, ModuleOrientation.Normal, null);
            var slot11 = ParseDesignSlot(ShipDesign.DesignSlotString(slot1, 0), moduleUIDs);
            Assert.IsTrue(slot1.Equals(slot11), $"DesignSlots were not equal: Expected {slot1} != Actual {slot11}");

            var slot4 = new DesignSlot(new Point(4,8), "FighterBay", new Point(4,3), 123, ModuleOrientation.Left, "Vulcan Scout");
            var slot44 = ParseDesignSlot(ShipDesign.DesignSlotString(slot4, 0), moduleUIDs);
            Assert.IsTrue(slot4.Equals(slot44), $"DesignSlots were not equal: Expected {slot1} != Actual {slot11}");
        }
    }
}
