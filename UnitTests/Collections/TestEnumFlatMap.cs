using System;
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

        [TestMethod]
        public void ClearEnumFlatMap()
        {
            var map = new EnumFlatMap<TestEnum, float>();
            map[TestEnum.One] = 1;
            map[TestEnum.Two] = 2;
            map[TestEnum.Three] = 3;
            map[TestEnum.Four] = 4;
            map[TestEnum.Five] = 5;
            map.Clear();
            Assert.AreEqual(0, map[TestEnum.One]);
            Assert.AreEqual(0, map[TestEnum.Two]);
            Assert.AreEqual(0, map[TestEnum.Three]);
            Assert.AreEqual(0, map[TestEnum.Four]);
            Assert.AreEqual(0, map[TestEnum.Five]);
        }

        [TestMethod]
        public void EnumerateValues()
        {
            var map = new EnumFlatMap<TestEnum, float>();
            Assert.AreEqual(5, map.Values.Count());

            map[TestEnum.One] = 1;
            map[TestEnum.Two] = 2;
            map[TestEnum.Three] = 3;
            map[TestEnum.Four] = 4;
            map[TestEnum.Five] = 5;
            Assert.AreEqual(5, map.Values.Count());

            (TestEnum, float)[] values = map.Values.ToArray();
            (TestEnum, float)[] expected =
            {
                (TestEnum.One, 1),
                (TestEnum.Two, 2),
                (TestEnum.Three, 3),
                (TestEnum.Four, 4),
                (TestEnum.Five, 5),
            };
            Assert.That.Equal(expected, values);
        }
    }
}
