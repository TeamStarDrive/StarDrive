using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Utils
{
    /// <summary>
    /// An implementation of RandomBase
    /// NOTE: This is not thread-safe, @see ThreadSafeRandom
    /// </summary>
    public class SeededRandom : RandomBase
    {
        protected override Random Rand { get; }

        public SeededRandom() : this(0)
        {
        }

        public SeededRandom(int seed) : base(seed)
        {
            Rand = new Random(Seed);
        }
    }
}
