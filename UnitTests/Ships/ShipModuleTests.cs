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
            CreateGameInstance();
            LoadStarterShips(starterShips:new[]{ "Vulcan Scout" }, 
                             savedDesigns:new[]{ "Prototype Frigate" });
            CreateUniverseAndPlayerEmpire(out _);
        }

        public static void AssertAreEqual(ModuleSlotData expected, ModuleSlotData actual)
        {
            Assert.AreEqual(expected.Position, actual.Position);
            Assert.AreEqual(expected.ModuleUID, actual.ModuleUID);
            Assert.AreEqual(expected.HangarshipGuid, actual.HangarshipGuid);
            Assert.AreEqual(expected.Health, actual.Health, 0.001f);
            Assert.AreEqual(expected.Facing, actual.Facing, 0.001f);
            Assert.AreEqual(expected.ShieldPower, actual.ShieldPower, 0.001f);
            Assert.AreEqual(expected.Orientation, actual.Orientation);
            Assert.AreEqual(expected.Restrictions, actual.Restrictions);
            Assert.AreEqual(expected.SlotOptions, actual.SlotOptions);
        }

        public static void AssertAreEqual(ShipModule expected, ShipModule actual)
        {
            Assert.AreEqual(expected.Position, actual.Position);
            Assert.AreEqual(expected.UID, actual.UID);
            Assert.AreEqual(expected.hangarShipUID, actual.hangarShipUID);
            Assert.AreEqual(expected.Health, actual.Health, 0.001f);
            Assert.AreEqual(expected.FacingDegrees, actual.FacingDegrees, 0.001f);
            Assert.AreEqual(expected.ShieldPower, actual.ShieldPower, 0.001f);
            Assert.AreEqual(expected.Orientation, actual.Orientation);
            Assert.AreEqual(expected.Restrictions, actual.Restrictions);
            Assert.AreEqual(expected.HangarShipGuid, actual.HangarShipGuid);
        }

        [TestMethod]
        public void Load_ShipModule()
        {
        }

        [TestMethod]
        public void New_ModuleSlotData()
        {
            var slot = new ModuleSlotData(new Vector2(64f, 128f), Restrictions.IOE);
            Assert.AreEqual(new Vector2(64f, 128f), slot.Position);
            Assert.AreEqual(Restrictions.IOE, slot.Restrictions);

            slot = new ModuleSlotData(new Vector2(64f, 128f), Restrictions.IOE,
                                      "FighterBay", 180f, ModuleOrientation.Rear.ToString(),
                                      slotOptions:"Vulcan Scout");

            Assert.AreEqual(new Vector2(64f, 128f), slot.Position);
            Assert.AreEqual(Restrictions.IOE, slot.Restrictions);
            Assert.AreEqual("FighterBay", slot.ModuleUID);
            Assert.AreEqual(180f, slot.Facing);
            Assert.AreEqual(ModuleOrientation.Rear.ToString(), slot.Orientation);
            Assert.AreEqual("Vulcan Scout", slot.SlotOptions);
        }

        [TestMethod]
        public void SaveGame_ShipModule_To_ModuleSlotData()
        {
            var ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);

            var oData = new ModuleSlotData(new Vector2(64f, 128f), Restrictions.O, "FighterBay",
                                           180f, ModuleOrientation.Rear.ToString(),
                                           slotOptions:"Vulcan Scout"/*this is the expected hangarShipUID*/);
            oData.Health = 555f;
            var original = ShipModule.Create(oData, ship, false, fromSave: true);

            var data = new ModuleSlotData(original);
            AssertAreEqual(oData, data);

            var recreated = ShipModule.Create(data, ship, false, fromSave: true);
            AssertAreEqual(original, recreated);
        }

        [TestMethod]
        public void ShipDesign_SlotStruct_To_ModuleSlotData()
        {
            ResourceManager.Hull("Terran/Shuttle", out ShipData hull);

            var original = new ModuleSlotData(new Vector2(64f, 128f), Restrictions.IOE, "FighterBay",
                                              180f, ModuleOrientation.Rear.ToString(),
                                              slotOptions:"Vulcan Scout"/*this is the expected hangarShipUID*/);

            var slot = new SlotStruct(original, new Vector2(0f, 0f));
            slot.Module = ShipModule.CreateDesignModule(original.ModuleOrNull, 
                                                        slot.Orientation, slot.Facing, hull);

            var recreated = new ModuleSlotData(slot);

            AssertAreEqual(original, recreated);
        }
    }
}
