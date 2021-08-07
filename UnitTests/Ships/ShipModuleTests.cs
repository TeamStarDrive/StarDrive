using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
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
            Assert.AreEqual(expected.HangarShip, actual.HangarShip);
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
            Assert.AreEqual(expected.HangarShipGuid, actual.HangarShipGuid);
        }

        [TestMethod]
        public void Load_ShipModule()
        {
        }

        [TestMethod]
        public void New_DesignSlot()
        {
            var hs = new HullSlot(4, 8, Restrictions.IOE);
            Assert.AreEqual(new Point(4, 8), hs.Pos);
            Assert.AreEqual(Restrictions.IOE, hs.R);

            var slot = new DesignSlot(hs.Pos, "FighterBay", new Point(3,4),
                                      180, ModuleOrientation.Rear, "Vulcan Scout");

            Assert.AreEqual(new Vector2(64f, 128f), slot.Pos);
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
            var oData = new ModuleSaveData(dData, health:555, shieldPower:0, Guid.Empty);
            var original = ShipModule.Create(oData, ship);

            var data = new ModuleSaveData(original);
            AssertAreEqual(oData, data);

            var recreated = ShipModule.Create(data, ship);
            AssertAreEqual(original, recreated);
        }

        [Ignore]
        [TestMethod]
        public void ShipDesign_SlotStruct_To_ModuleSlotData()
        {
            ResourceManager.Hull("Terran/Shuttle", out ShipHull hull);

            // TODO: needs to be rewritten

            //var original = new ModuleSlotData(new Vector2(64f, 128f), Restrictions.IOE, "FighterBay",
            //                                  180f, ModuleOrientation.Rear.ToString(),
            //                                  slotOptions:"Vulcan Scout"/*this is the expected hangarShipUID*/);

            //var slot = new SlotStruct(original, hull);
            //slot.Module = ShipModule.CreateDesignModule(original.ModuleOrNull, 
            //                                            slot.Orientation, slot.Facing, hull);

            //var recreated = new ModuleSlotData(slot);
            //AssertAreEqual(original, recreated);
        }
    }
}
