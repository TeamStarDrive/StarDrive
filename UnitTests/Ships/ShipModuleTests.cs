using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;

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
            AssertEqual(expected.Pos, actual.Pos);
            AssertEqual(expected.ModuleUID, actual.ModuleUID);
            AssertEqual(expected.Size, actual.Size);
            AssertEqual(0.001f, expected.TurretAngle, actual.TurretAngle);
            AssertEqual(expected.ModuleRot, actual.ModuleRot);
            AssertEqual(expected.HangarShipUID, actual.HangarShipUID);
        }

        public static void AssertAreEqual(ModuleSaveData expected, ModuleSaveData actual)
        {
            AssertAreEqual(expected as DesignSlot, actual as DesignSlot);

            AssertEqual(expected.Health, actual.Health);
            AssertEqual(expected.ShieldPower, actual.ShieldPower);
            AssertEqual(expected.HangarShip, actual.HangarShip);
        }

        public static void AssertAreEqual(ShipModule expected, ShipModule actual)
        {
            AssertEqual(expected.Position, actual.Position);
            AssertEqual(expected.UID, actual.UID);
            AssertEqual(expected.HangarShipUID, actual.HangarShipUID);
            AssertEqual(expected.TurretAngle, actual.TurretAngle);
            AssertEqual(expected.ModuleRot, actual.ModuleRot);
            AssertEqual(expected.Restrictions, actual.Restrictions); // TODO

            AssertEqual(0.001f, expected.Health, actual.Health);
            AssertEqual(0.001f, expected.ShieldPower, actual.ShieldPower);
            AssertEqual(expected.HangarShip, actual.HangarShip);
        }

        [TestMethod]
        public void CreateModule_WithWeapon()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, new Vector2(1000, 1000));
            var m = ShipModule.Create(null, new DesignSlot(new Point(1, 2), "LaserBeam2x3", new Point(3,2), 45, ModuleOrientation.Left, null), ship, false);
            AssertEqual("LaserBeam2x3", m.UID);
            AssertEqual(new Point(3,2), m.GetSize()); // DesignSlot size is always already oriented
            AssertEqual(new Point(1, 2), m.Pos, "Module Grid Position was not set");

            AssertEqual(new Point(2,2), ship.BaseHull.GridCenter, "Following calculations require GridCenter 2,2");
            AssertEqual(new Vector2(8, 16), m.LocalCenter);
            AssertEqual(new Vector2(1008, 1016), m.Position, "Initial module position should be offset from ship center");

            AssertEqual(45, m.TurretAngle);
            AssertEqual(ModuleOrientation.Left, m.ModuleRot);
            AssertEqual(m.ActualMaxHealth, m.Health);
            AssertGreaterThan(m.TargetValue, 1, "Weapon module LaserBeam2x3 should have a reasonably high target value");
            AssertEqual(3 * 11.5f, m.Radius, "Bounding Radius should use the biggest Size Axis");

            Assert.IsNotNull(m.InstalledWeapon);
            AssertEqual("FocusLaserBeam", m.WeaponType);
            Assert.AreNotSame(m.InstalledWeapon, ResourceManager.GetWeaponTemplate(m.WeaponType), "Installed weapon was not cloned! This is a bug!");

            AssertEqual(m, m.InstalledWeapon.Module, "Installed weapon Module ref was not set");
            AssertEqual(ship, m.InstalledWeapon.Owner, "Installed weapon Owner ref was not set");
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
            AssertEqual(new Point(4, 8), hs.Pos);
            AssertEqual(Restrictions.IOE, hs.R);

            var slot = new DesignSlot(hs.Pos, "FighterBay", new Point(3,4),
                                      180, ModuleOrientation.Rear, "Vulcan Scout");

            AssertEqual(new Point(4, 8), slot.Pos);
            AssertEqual("FighterBay", slot.ModuleUID);
            AssertEqual(180, slot.TurretAngle);
            AssertEqual(ModuleOrientation.Rear, slot.ModuleRot);
            AssertEqual("Vulcan Scout", slot.HangarShipUID);
        }

        [TestMethod]
        public void SaveGame_ShipModule_To_ModuleSaveData()
        {
            var ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);

            var dData = new DesignSlot(new Point(4,8), "FighterBay", new Point(3,4), 180, ModuleOrientation.Rear, "Vulcan Scout");
            var oData = new ModuleSaveData(dData, health:555, shieldPower:0, null);
            var original = ShipModule.Create(null, oData, ship);

            var data = new ModuleSaveData(original);
            AssertAreEqual(oData, data);

            var recreated = ShipModule.Create(null, data, ship);
            AssertAreEqual(original, recreated);
        }

        public static string DesignSlotString(DesignSlot slot, ushort moduleIdx)
        {
            return ShipDesign.WriteDesignSlotString(new ShipDesignWriter(), slot, moduleIdx).ToString();
        }
        
        static DesignSlot ParseDesignSlot(string text, string[] moduleUIDs)
        {
            return ShipDesign.ParseDesignSlot(new StringView(text.ToCharArray()), moduleUIDs);
        }

        [TestMethod]
        public void ShipDesign_DesignSlot_To_DesignSlotString()
        {
            var slot1 = new DesignSlot(new Point(4,8), "FighterBay", new Point(1,1), 0, ModuleOrientation.Normal, null);
            AssertEqual("4,8;0", DesignSlotString(slot1, 0), "Expected gridX,gridY;moduleIdx");

            var slot2 = new DesignSlot(new Point(4,8), "FighterBay", new Point(2,4), 0, ModuleOrientation.Normal, null);
            AssertEqual("4,8;0;2,4", DesignSlotString(slot2, 0), "Expected gridX,gridY;moduleIdx;sizeX,sizeY");

            var slot3 = new DesignSlot(new Point(4,8), "FighterBay", new Point(1,1), 0, ModuleOrientation.Normal, "Vulcan Scout");
            AssertEqual("4,8;0;;;;Vulcan Scout", DesignSlotString(slot3, 0),
                            "Expected gridX,gridY;moduleIdx;sizeX,sizeY;turretAngle;moduleRot;hangarShip");

            var slot4 = new DesignSlot(new Point(4,8), "FighterBay", new Point(4,3), 123, ModuleOrientation.Left, "Vulcan Scout");
            AssertEqual("4,8;1;4,3;123;1;Vulcan Scout", DesignSlotString(slot4, 1),
                            "Expected gridX,gridY;moduleIdx;sizeX,sizeY;turretAngle;moduleRot;hangarShip");
        }

        [TestMethod]
        public void ShipDesign_ParseDesignSlot()
        {
            string[] moduleUIDs = { "FighterBay" };
            var slot1 = new DesignSlot(new Point(4,8), "FighterBay", new Point(1,1), 0, ModuleOrientation.Normal, null);
            var slot11 = ParseDesignSlot(DesignSlotString(slot1, 0), moduleUIDs);
            Assert.IsTrue(slot1.Equals(slot11), $"DesignSlots were not equal: Expected {slot1} != Actual {slot11}");

            var slot4 = new DesignSlot(new Point(4,8), "FighterBay", new Point(4,3), 123, ModuleOrientation.Left, "Vulcan Scout");
            var slot44 = ParseDesignSlot(DesignSlotString(slot4, 0), moduleUIDs);
            Assert.IsTrue(slot4.Equals(slot44), $"DesignSlots were not equal: Expected {slot1} != Actual {slot11}");
        }
    }
}
