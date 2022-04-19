using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace Ship_Game.Utils
{
    /// <summary>
    /// Efficient fixed size bit-array
    /// </summary>
    public struct BitArray
    {
        readonly uint[] Values;

        public BitArray(int size)
        {
            Values = new uint[(size / 32) + 1];
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
}
