using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Collections
{
    [TestClass]
    public class TestEnumFlatMap
    {
        enum TestEnum
        {
            One,
            Two,
            Three,
            Four,
            Five
        }

        [TestMethod]
        public void CreateFlatMap()
        {
            var map = new EnumFlatMap<TestEnum, float>();
            map[TestEnum.One] = 1;
            map[TestEnum.Two] = 2;
            map[TestEnum.Three] = 3;
            map[TestEnum.Four] = 4;
            map[TestEnum.Five] = 5;
            Assert.AreEqual(1, map[TestEnum.One]);
            Assert.AreEqual(2, map[TestEnum.Two]);
            Assert.AreEqual(3, map[TestEnum.Three]);
            Assert.AreEqual(4, map[TestEnum.Four]);
            Assert.AreEqual(5, map[TestEnum.Five]);
        }
    }
}
