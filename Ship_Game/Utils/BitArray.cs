using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /// <param name="index">Index of the bit</param>
        /// <returns>True if the bit is set, false otherwise.</returns>
        public bool IsSet(int index)
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
    }
}
