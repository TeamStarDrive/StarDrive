using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipDesignWriterTests : StarDriveTest
    {
        static void Measure(string name, int iterations, Action action)
        {
            long memStart = GC.GetTotalMemory(true);
            var t = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
                action();
            long memEnd = GC.GetTotalMemory(false);
            Console.WriteLine($"{name} elapsed: {t.ElapsedMillis:0.0}ms +{(memEnd-memStart)/(1024*1024.0):0.00}MB");
        }

        [TestMethod]
        public void ShipDesignWriter_Correctness_String()
        {
            var w = new ShipDesignWriter();
            w.Write("teststring");
            Assert.AreEqual("teststring", w.ToString());
            w.Write("1234");
            Assert.AreEqual("teststring1234", w.ToString());
        }

        [TestMethod]
        public void ShipDesignWriter_Correctness_Char()
        {
            var w = new ShipDesignWriter();
            w.Write('c');
            Assert.AreEqual("c", w.ToString());
            w.Write("d");
            Assert.AreEqual("cd", w.ToString());
        }

        [TestMethod]
        public void ShipDesignWriter_Correctness_KeyValStr()
        {
            var w = new ShipDesignWriter();
            w.Write("shipName", "cookieCutter9000");
            Assert.AreEqual("shipName=cookieCutter9000\n", w.ToString());
            w.Write("id", "90");
            Assert.AreEqual("shipName=cookieCutter9000\nid=90\n", w.ToString());
        }

        [TestMethod]
        public void ShipDesignWriter_Perf_String()
        {
            const int iterations = 1000;
            const int loopsPerBuffer = 300;
            Measure("SDW.char", iterations, () =>
            {
                var w = new ShipDesignWriter();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    w.Write("teststring");
                w.GetASCIIBytes();
            });
            Measure("Sb.char", iterations, () =>
            {
                var w = new StringBuilder();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    w.Append("teststring");
                w.ToString();
            });
        }

        [TestMethod]
        public void ShipDesignWriter_Perf_Char()
        {
            const int iterations = 1000;
            const int loopsPerBuffer = 2000;
            Measure("SDW.string", iterations, () =>
            {
                var w = new ShipDesignWriter();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    w.Write(';');
                w.GetASCIIBytes();
            });
            Measure("Sb.string", iterations, () =>
            {
                var w = new StringBuilder();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    w.Append(';');
                w.ToString();
            });
        }

        [TestMethod]
        public void ShipDesignWriter_Perf_KeyValStr()
        {
            const int iterations = 1000;
            const int loopsPerBuffer = 300;
            Measure("SDW.keyvalstring", iterations, () =>
            {
                var w = new ShipDesignWriter();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    w.Write("shipName", "cookieCutter9000");
                w.GetASCIIBytes();
            });
            Measure("Sb.keyvalstring", iterations, () =>
            {
                var w = new StringBuilder();
                for (int i = 0; i < loopsPerBuffer; ++i)
                {
                    w.Append("shipName");
                    w.Append('=');
                    w.Append("cookieCutter9000");
                    w.Append('\n');
                }
                w.ToString();
            });
        }
    }
}
