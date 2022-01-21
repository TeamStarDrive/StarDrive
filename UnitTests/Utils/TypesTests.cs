using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Utils
{
    [TestClass]
    public class TypesTests
    {
        [Flags]
        enum Flags
        {
            None,
            First  = (1 << 1),
            Second = (1 << 2),
            Third  = (1 << 3),
            Fourth = (1 << 4),
            Fifth  = (1 << 5),
        }

        [TestMethod]
        public void EnumFlagsPerformance()
        {
            Flags flags = Flags.Second | Flags.Third;
            Assert.IsTrue(flags.IsSet(Flags.Third));
            Assert.IsFalse(flags.IsSet(Flags.Fifth));

            const int iterations = 100000;

            var t1 = new PerfTimer(start:true);
            for (int i = 0; i < iterations; ++i)
            {
                bool x = (flags & Flags.Third) != 0;
                bool y = (flags & Flags.Fifth) != 0;
            }
            float e1 = t1.ElapsedMillis;
            Log.Write($"flags & flag rawbits: {e1:0.###}ms");

            var t2 = new PerfTimer(start:true);
            for (int i = 0; i < iterations; ++i)
            {
                flags.IsSet(Flags.Third);
                flags.IsSet(Flags.Fifth);
            }
            float e2 = t2.ElapsedMillis;
            Log.Write($"Enum.IsSet extension: {e2:0.###}ms");

            var t3 = new PerfTimer(start:true);
            for (int i = 0; i < iterations; ++i)
            {
                flags.HasFlag(Flags.Third);
                flags.HasFlag(Flags.Fifth);
            }
            float e3 = t3.ElapsedMillis;
            Log.Write($"Enum.HasFlag builtin: {e3:0.###}ms");
        }
    }
}
