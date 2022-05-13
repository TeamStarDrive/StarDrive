using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Utils
{
    /// <summary>
    /// Extremely small bitset which can contain up to 32 flags
    /// </summary>
    public struct SmallBitSet
    {
        uint Values;

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
        public bool IsSet(int index)
        {
            return (Values & (1u << index)) != 0;
        }

        /// <returns>TRUE if any of the bits are set</returns>
        public bool IsAnyBitsSet => Values != 0;

        /// <returns>Number of bits that are set in this SmallBitSet</returns>
        public int NumBitsSet()
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
    }
}
