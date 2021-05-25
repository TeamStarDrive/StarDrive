using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Utils;

namespace UnitTests.Collections
{
    [TestClass]
    class SafeArrayTests : StarDriveTest
    {

        [TestMethod]
        public void Add()
        {
            var arr = new SafeArray<int>();
            arr.Add(1);
            Assert.AreEqual(arr.Count, 1, "Count should be 1");
            Assert.AreEqual(arr.Capacity, 4, "Capacity should be 4");
            arr.Add(2);
            arr.Add(3);
            arr.Add(4);
            arr.Add(5);
            Assert.AreEqual(5, arr.Count, "Count should be 5");
            Assert.AreEqual(8, arr.Capacity, "Capacity should grow aligned to 4, expected 8");
        }
    }
}
