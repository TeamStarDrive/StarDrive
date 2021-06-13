using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Utils;

namespace UnitTests.Utils
{
    [TestClass]
    public class DoubleBufferedArrayTests : StarDriveTest
    {
        [TestMethod]
        public void AddDoesNotAffectFrontBuffer()
        {
            var arr = new DoubleBufferedArray<string>();

            arr.Add("ship1");
            var front = arr.GetItems();
            Assert.AreEqual(0, front.Length);
            arr.Add("ship2");
            Assert.AreEqual(0, front.Length);

            arr.ApplyChanges();
            Assert.AreEqual(0, front.Length, "Front should still be unmodified after ApplyChanges");

            var newFront = arr.GetItems();
            Assert.AreEqual(2, newFront.Length);
            arr.Add("ship3");
            Assert.AreEqual(2, newFront.Length);

            arr.ApplyChanges();
            Assert.AreEqual(2, newFront.Length);
            var newFront2 = arr.GetItems();
            Assert.AreEqual(3, newFront2.Length);
        }
    }
}
