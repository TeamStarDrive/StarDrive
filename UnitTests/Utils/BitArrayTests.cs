using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;

namespace UnitTests.Utils
{
    [TestClass]
    public class BitArrayTests : StarDriveTest
    {
        [TestMethod]
        public void CanSetAndCheckBitArrayValues()
        {
            const int count = 1333;
            var bits = new BitArray(count);
            Assert.IsFalse(bits.IsAnyBitsSet);

            for (int i = 0; i < count; ++i)
            {
                Assert.IsFalse(bits.IsSet(i), $"Bit {i} must be unset");
                bits.Set(i);
                Assert.IsTrue(bits.IsSet(i), $"Bit {i} must be set");
                Assert.IsTrue(bits.IsAnyBitsSet);
            }

            AssertEqual(count, bits.NumBitsSet());
        }

        [TestMethod]
        public void CanUnsetBits()
        {
            const int count = 1333;
            var bits = new BitArray(count);

            for (int i = 0; i < count; ++i)
            {
                bits.Set(i, true);
                Assert.IsTrue(bits.IsSet(i), $"Bit {i} must be set");
            }

            for (int i = 0; i < count; ++i)
            {
                Assert.IsTrue(bits.IsSet(i), $"Bit {i} must be set");
                bits.Unset(i);
                Assert.IsFalse(bits.IsSet(i), $"Bit {i} must be unset");
            }

            // final check: make sure there is no bit array corruption:
            for (int i = 0; i < count; ++i)
                Assert.IsFalse(bits.IsSet(i), $"Bit {i} must be unset");

            Assert.IsFalse(bits.IsAnyBitsSet);
            AssertEqual(0, bits.NumBitsSet());
        }

        [TestMethod]
        public void CanUnsetViaSetMethod()
        {
            const int count = 1333;
            var bits = new BitArray(count);
            for (int i = 0; i < count; ++i)
                bits.Set(i);

            for (int i = 0; i < count; ++i)
            {
                Assert.IsTrue(bits.IsSet(i), $"Bit {i} must be set");
                bits.Set(i, false);
                Assert.IsFalse(bits.IsSet(i), $"Bit {i} must be unset");
            }

            Assert.IsFalse(bits.IsAnyBitsSet);
            AssertEqual(0, bits.NumBitsSet());
        }

        
        [TestMethod]
        public void FirstSetBitIndex()
        {
            const int count = 256;
            var bits = new BitArray(count);

            AssertEqual(-1, bits.GetFirstSetBitIndex());

            bits.Set(63);
            AssertEqual(63, bits.GetFirstSetBitIndex());
            bits.Unset(63);

            for (int i = 0; i < count; ++i)
            {
                bits.Set(i);
                AssertEqual(i, bits.GetFirstSetBitIndex());
                bits.Unset(i);
            }

            AssertEqual(-1, bits.GetFirstSetBitIndex());
        }
    }
}
