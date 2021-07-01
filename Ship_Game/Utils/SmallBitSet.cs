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
    }
}
