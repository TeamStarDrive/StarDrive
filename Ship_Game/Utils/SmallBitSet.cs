using System.Diagnostics.Contracts;

namespace Ship_Game.Utils;

/// <summary>
/// Extremely small bitset which can contain up to 32 flags
/// </summary>
public struct SmallBitSet
{
    public uint Values;

    /// <summary>
    /// Sets a bit, valid indexes are from [0..31]
    /// </summary>
    public void Set(int index)
    {
        Values |= (1u << index);
    }

    /// <summary>
    /// Unset a bit
    /// </summary>
    public void Unset(int index)
    {
        Values &= ~(1u << index);
    }

    /// <summary>
    /// Set or Unset a bit
    /// </summary>
    public void SetValue(int index, bool value)
    {
        if (value)
            Values |= (1u << index);
        else
            Values &= ~(1u << index);
    }

    /// <summary>
    /// TRUE if bit at index is set true
    /// </summary>
    [Pure] public readonly bool IsSet(int index)
    {
        return (Values & (1u << index)) != 0;
    }

    /// <summary>
    /// TRUE if any other index is set to true except for `exceptIndex`
    /// </summary>
    [Pure] public readonly bool IsAnyBitsSetExcept(int exceptIndex)
    {
        uint inverseBits = ~(1u << exceptIndex);
        return (Values & inverseBits) != 0;
    }

    /// <returns>TRUE if any of the bits are set</returns>
    [Pure] public readonly bool IsAnyBitsSet => Values != 0;

    /// <returns>Number of bits that are set in this SmallBitSet</returns>
    [Pure] public readonly int NumBitsSet()
    {
        uint v = Values; // count bits set in this (32-bit value)
        // store the total in c
        uint c = v - ((v >> 1) & 0x55555555);
        c = ((c >> 2) & 0x33333333) + (c & 0x33333333);
        c = ((c >> 4) + c) & 0x0F0F0F0F;
        c = ((c >> 8) + c) & 0x00FF00FF;
        c = ((c >> 16) + c) & 0x0000FFFF;
        return (int)c;
    }

    /// <returns>Index of the first bit which is set, or -1 if none</returns>
    [Pure] public readonly int GetFirstSetBitIndex()
    {
        uint value = Values;
        if (value != 0)
        {
            for (int bit = 0; bit < 32; ++bit)
            {
                uint mask = (uint)(1 << bit);
                if ((value & mask) != 0)
                    return bit;
            }
        }
        return -1;
    }
}
