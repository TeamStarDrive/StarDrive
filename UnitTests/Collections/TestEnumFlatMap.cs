using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;

namespace UnitTests.Collections
{
    [TestClass]
    public class TestEnumFlatMap : StarDriveTest
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
            AssertEqual(1, map[TestEnum.One]);
            AssertEqual(2, map[TestEnum.Two]);
            AssertEqual(3, map[TestEnum.Three]);
            AssertEqual(4, map[TestEnum.Four]);
            AssertEqual(5, map[TestEnum.Five]);
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
            AssertEqual(0, map[TestEnum.One]);
            AssertEqual(0, map[TestEnum.Two]);
            AssertEqual(0, map[TestEnum.Three]);
            AssertEqual(0, map[TestEnum.Four]);
            AssertEqual(0, map[TestEnum.Five]);
        }

        [TestMethod]
        public void EnumerateValues()
        {
            var map = new EnumFlatMap<TestEnum, float>();
            AssertEqual(5, map.Values.Count());

            map[TestEnum.One] = 1;
            map[TestEnum.Two] = 2;
            map[TestEnum.Three] = 3;
            map[TestEnum.Four] = 4;
            map[TestEnum.Five] = 5;
            AssertEqual(5, map.Values.Count());

            (TestEnum, float)[] values = map.Values.ToArr();
            (TestEnum, float)[] expected =
            {
                (TestEnum.One, 1),
                (TestEnum.Two, 2),
                (TestEnum.Three, 3),
                (TestEnum.Four, 4),
                (TestEnum.Five, 5),
            };
            AssertEqual(expected, values);
        }
    }
}
