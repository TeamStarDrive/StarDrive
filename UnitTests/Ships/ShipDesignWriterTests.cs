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
            GC.Collect();
            long memStart = GC.GetTotalMemory(false);
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
        public void ShipDesignWriter_Correctness_StrArray()
        {
            var w = new ShipDesignWriter();
            w.Write("single", new[]{"string"});
            Assert.AreEqual("single=string\n", w.ToString());
            w.Write("slot", new []{"100","FighterBay","96,48"});
            Assert.AreEqual("single=string\nslot=100;FighterBay;96,48\n", w.ToString());
            w.Write("id", new []{"Johnson","117"});
            Assert.AreEqual("single=string\nslot=100;FighterBay;96,48\nid=Johnson;117\n", w.ToString());
        }

        [TestMethod]
        public void ShipDesignWriter_Perf_String()
        {
            const int iterations = 1000;
            const int loopsPerBuffer = 300;
            const string teststring = "teststring1234";

            var sdw = new ShipDesignWriter(); // NOTE: this is the usage pattern in SavedGame.cs
            Measure("SDW.char", iterations, () =>
            {
                sdw.Clear();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    sdw.Write(teststring);
                sdw.GetASCIIBytes();
            });
            Console.WriteLine($"SDW Capacity = {sdw.Capacity / 1024.0:0.0}KB");
            Measure("Sb.char", iterations, () =>
            {
                var w = new StringBuilder();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    w.Append(teststring);
                w.ToString();
            });
        }

        [TestMethod]
        public void ShipDesignWriter_Perf_Char()
        {
            const int iterations = 1000;
            const int loopsPerBuffer = 20000;

            var sdw = new ShipDesignWriter(); // NOTE: this is the usage pattern in SavedGame.cs
            Measure("SDW.string", iterations, () =>
            {
                sdw.Clear();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    sdw.Write(';');
                sdw.GetASCIIBytes();
            });
            Console.WriteLine($"SDW Capacity = {sdw.Capacity / 1024.0:0.0}KB");
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
            const int loopsPerBuffer = 800;

            var sdw = new ShipDesignWriter(); // NOTE: this is the usage pattern in SavedGame.cs
            Measure("SDW.keyvalstring", iterations, () =>
            {
                sdw.Clear();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    sdw.Write("shipName", "cookieCutter9000");
                sdw.GetASCIIBytes();
            });
            Console.WriteLine($"SDW Capacity = {sdw.Capacity / 1024.0:0.0}KB");
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



        [TestMethod]
        public void ShipDesignWriter_Perf_StrArray()
        {
            const int iterations = 1000;
            const int loopsPerBuffer = 800;
            string[] strArr =
            {
                "100.47", ";",
                "128,96", ";",
                "FighterBay", ";",
                "4,2", ";",
                "360", ";",
                "0", ";",
                "Advanced Hunter IIIa-S",
            };

            var sdw = new ShipDesignWriter(); // NOTE: this is the usage pattern in SavedGame.cs
            Measure("SDW.strarray", iterations, () =>
            {
                sdw.Clear();
                for (int i = 0; i < loopsPerBuffer; ++i)
                    sdw.Write("slot", strArr);
                sdw.GetASCIIBytes();
            });
            Console.WriteLine($"SDW Capacity = {sdw.Capacity / 1024.0:0.0}KB");
            Measure("Sb.strarray", iterations, () =>
            {
                var w = new StringBuilder();
                for (int i = 0; i < loopsPerBuffer; ++i)
                {
                    w.Append("slot");
                    w.Append('=');
                    for (int j = 0; j < strArr.Length; ++j)
                    {
                        w.Append(strArr[j]);
                        if (i != strArr.Length - 1)
                            w.Append(';');
                    }
                    w.Append('\n');
                }
                w.ToString();
            });
        }
    }
}
