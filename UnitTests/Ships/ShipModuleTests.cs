using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;

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
        }

        public static void AssertAreEqual(ModuleSlotData sa, ModuleSlotData sb)
        {
            Assert.AreEqual(sa.Position, sb.Position);
            Assert.AreEqual(sa.ModuleUID, sb.ModuleUID);
            Assert.AreEqual(sa.HangarshipGuid, sb.HangarshipGuid);
            Assert.AreEqual(sa.Health, sb.Health, 0.001f);
            Assert.AreEqual(sa.Facing, sb.Facing, 0.001f);
            Assert.AreEqual(sa.ShieldPower, sb.ShieldPower, 0.001f);
            Assert.AreEqual(sa.Orientation, sb.Orientation);
            Assert.AreEqual(sa.Restrictions, sb.Restrictions);
            Assert.AreEqual(sa.SlotOptions, sb.SlotOptions);
        }

        [TestMethod]
        public void Load_ShipModule()
        {

        }
    }
}
