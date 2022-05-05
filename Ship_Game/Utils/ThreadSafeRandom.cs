using System;
using System.Threading;

namespace Ship_Game.Utils
{
    public class ThreadSafeRandom : RandomBase
    {
        // NOTE: This is really fast
        readonly ThreadLocal<Random> Randoms;
        protected override Random Rand => Randoms.Value;

        public ThreadSafeRandom() : this(0)
        {
        }

        public ThreadSafeRandom(int seed) : base(seed)
        {
            Randoms = new ThreadLocal<Random>(() => new Random(Seed));
        }
    }
}
