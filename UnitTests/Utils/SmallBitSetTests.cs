using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game.Utils;

namespace UnitTests.Utils;

[TestClass]
public class SmallBitSetTests : StarDriveTest
{
    [TestMethod]
    public void CanSetAndCheckBitSetValues()
    {
        var bits = new SmallBitSet();
        AssertFalse(bits.IsAnyBitsSet);
        AssertEqual(0, bits.NumBitsSet());

        for (int i = 0; i < 32; ++i)
        {
            AssertFalse(bits.IsSet(i), $"Bit {i} must be unset");
            bits.Set(i);
            AssertTrue(bits.IsSet(i), $"Bit {i} must be set");
            AssertTrue(bits.IsAnyBitsSet);
            AssertEqual(i+1, bits.NumBitsSet());
        }

        AssertEqual(32, bits.NumBitsSet());
    }

    [TestMethod]
    public void CanUnsetBits()
    {
        var bits = new SmallBitSet();

        for (int i = 0; i < 32; ++i)
        {
            bits.Set(i);
            AssertTrue(bits.IsSet(i), $"Bit {i} must be set");
        }
        
        AssertTrue(bits.IsAnyBitsSet);
        AssertEqual(32, bits.NumBitsSet());

        for (int i = 0; i < 32; ++i)
        {
            AssertTrue(bits.IsSet(i), $"Bit {i} must be set");
            bits.Unset(i);
            AssertFalse(bits.IsSet(i), $"Bit {i} must be unset");
        }
        
        AssertEqual(0, bits.NumBitsSet());

        // final check: make sure there is no bit array corruption:
        for (int i = 0; i < 32; ++i)
            AssertFalse(bits.IsSet(i), $"Bit {i} must be unset");

        AssertFalse(bits.IsAnyBitsSet);
        AssertEqual(0, bits.NumBitsSet());
    }

    [TestMethod]
    public void CanUnsetViaSetValueMethod()
    {
        var bits = new SmallBitSet();
        for (int i = 0; i < 32; ++i)
            bits.Set(i);

        for (int i = 0; i < 32; ++i)
        {
            AssertTrue(bits.IsSet(i), $"Bit {i} must be set");
            bits.SetValue(i, false);
            AssertFalse(bits.IsSet(i), $"Bit {i} must be unset");
        }

        AssertFalse(bits.IsAnyBitsSet);
        AssertEqual(0, bits.NumBitsSet());
    }

    [TestMethod]
    public void IsAnyBitsSetExcept()
    {
        var bits = new SmallBitSet();
        bits.Set(10);
        AssertFalse(bits.IsAnyBitsSetExcept(10));
        AssertTrue(bits.IsAnyBitsSetExcept(11));
        bits.Unset(10);

        for (int i = 0; i < 31; ++i)
        {
            bits.Set(i);
            AssertFalse(bits.IsAnyBitsSetExcept(i));
            AssertTrue(bits.IsAnyBitsSetExcept(i+1));
            bits.Unset(i);
        }
    }

    [TestMethod]
    public void FirstSetBitIndex()
    {
        var bits = new SmallBitSet();

        AssertEqual(-1, bits.GetFirstSetBitIndex());

        bits.Set(22);
        AssertEqual(22, bits.GetFirstSetBitIndex());
        bits.Unset(22);

        for (int i = 0; i < 32; ++i)
        {
            bits.Set(i);
            AssertEqual(i, bits.GetFirstSetBitIndex());
            bits.Unset(i);
        }

        AssertEqual(-1, bits.GetFirstSetBitIndex());
    }
}
