using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Ships
{
    [TestClass]
    class ShipLoyaltyTests : StarDriveTest
    {
        public ShipLoyaltyTests()
        {
            CreateGameInstance();
        }

        [TestMethod]
        public void LoyaltyChangeDoesNotBreakSpatialLookup()
        {
            //var changeTo = EmpireManager.Empires.Find(e => e != empire);
            //s.LoyaltyChangeFromBoarding(changeTo,false);
        }
    }
}
