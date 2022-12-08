using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace SDUtils;

/// <summary>
/// Efficient fixed size bit-array
/// </summary>
public struct BitArray
{
    public readonly uint[] Values;

    /// <summary>
    /// size: Total number of bits to support
    /// </summary>
    public BitArray(int size)
    {
        Values = new uint[(size / 32) + 1];
    }

    /// <summary>
    /// Construct a BitArray from an array of uint bits
    /// </summary>
    public BitArray(uint[] bits)
    {
        Values = bits;
    }

    // Maximum number of flags supported by this fixed size BitArray
    public int MaxFlags => Values.Length * 32;

    // Amount of MB memory used by this BitArray
    public float MegabytesUsed => (Values.Length * 4) / (1024f * 1024f);

    // Amount of KB memory used by this BitArray
    public float KilobytesUsed => (Values.Length * 4) / (1024f);

    // Clears all bits
    public void Clear()
    {
        Array.Clear(Values, 0, Values.Length);
    }

    /// <param name="index">Index of the bit</param>
    /// <returns>True if the bit is set, false otherwise.</returns>
    [Pure] public bool IsSet(int index)
    {
        int wordIndex = index / 32;
        uint mask = (uint)(1 << (index % 32));

        return (Values[wordIndex] & mask) != 0;
    }

    /// <param name="index">Index of the bit</param>
    /// <param name="value">True to set the bit, False to clear the bit</param>
    public void Set(int index, bool value)
    {
        int wordIndex = index / 32;
        uint mask = (uint)(1 << (index % 32));

        uint v = Values[wordIndex];
        Values[wordIndex] = value ? (v | mask) : (v & ~mask);
    }

    /// <param name="index">Index of the bit to set</param>
    public void Set(int index)
    {
        int wordIndex = index / 32;
        uint mask = (uint)(1 << (index % 32));

        Values[wordIndex] = (Values[wordIndex] | mask);
    }

    /// <param name="index">Index of the bit to clear</param>
    public void Unset(int index)
    {
        int wordIndex = index / 32;
        uint mask = (uint)(1 << (index % 32));

        Values[wordIndex] = (Values[wordIndex] & ~mask);
    }

    /// <returns>Index of the first bit which is set, or -1 if none</returns>
    public int GetFirstSetBitIndex()
    {
        for (int i = 0; i < Values.Length; ++i)
        {
            uint value = Values[i];
            if (value != 0)
            {
                for (int bit = 0; bit < 32; ++bit)
                {
                    uint mask = (uint)(1 << bit);
                    if ((value & mask) != 0)
                        return i*32 + bit;
                }
            }
        }
        return -1;
    }

    /// <returns>TRUE if any of the bits are set</returns>
    public bool IsAnyBitsSet
    {
        get
        {
            for (int i = 0; i < Values.Length; ++i)
                if (Values[i] != 0) return true;
            return false;
        }
    }

    /// <returns>Number of bits that are set in this SmallBitSet</returns>
    public int NumBitsSet()
    {
        int numSet = 0;
        for (int i = 0; i < Values.Length; ++i)
            numSet += NumBitsSet(Values[i]);
        return numSet;
    }

    static int NumBitsSet(uint values)
    {
        uint v = values; // count bits set in this (32-bit value)
        // store the total in c
        uint c = v - ((v >> 1) & 0x55555555);
        c = ((c >> 2) & 0x33333333) + (c & 0x33333333);
        c = ((c >> 4) + c) & 0x0F0F0F0F;
        c = ((c >> 8) + c) & 0x00FF00FF;
        c = ((c >> 16) + c) & 0x0000FFFF;
        return (int)c;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Values.Length; ++i)
        {
            uint word = Values[i];
            for (int j = 0; j < 32; ++j)
            {
                bool isSet = (word & (1 << j)) != 0;
                sb.Append(isSet ? '1' : '0');
            }
            sb.Append(" \n");
        }
        return sb.ToString();
    }

    public bool[] ToArray()
    {
        bool[] bits = new bool[Values.Length * 32];
        for (int i = 0; i < bits.Length; ++i)
            bits[i] = IsSet(i);
        return bits;
    }
}
