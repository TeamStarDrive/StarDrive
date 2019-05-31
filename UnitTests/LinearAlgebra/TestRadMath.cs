using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests.LinearAlgebra
{
    [TestClass]
    public class TestRadMath
    {
        const float Tolerance = 0.001f;

        [TestMethod]
        public void FastSin()
        {
            Assert.AreEqual(Math.Sin(0), RadMath.Sin(0), Tolerance);
            Assert.AreEqual(Math.Sin(1), RadMath.Sin(1), Tolerance);
            Assert.AreEqual(Math.Sin(-1), RadMath.Sin(-1), Tolerance);

            Assert.AreEqual(Math.Sin(Math.PI), RadMath.Sin(RadMath.PI), Tolerance);
            Assert.AreEqual(Math.Sin(2*Math.PI), RadMath.Sin(2*RadMath.PI),Tolerance);
            
            Assert.AreEqual(Math.Sin(-Math.PI), RadMath.Sin(-RadMath.PI), Tolerance);
            Assert.AreEqual(Math.Sin(-2*Math.PI), RadMath.Sin(-2*RadMath.PI), Tolerance);
            
            Assert.AreEqual(Math.Sin(Math.PI*1.5), RadMath.Sin(RadMath.PI*1.5f), Tolerance);
            Assert.AreEqual(Math.Sin(-Math.PI*1.5), RadMath.Sin(-RadMath.PI*1.5f), Tolerance);

            Assert.AreEqual(Math.Sin(6), RadMath.Sin(6), Tolerance);
            Assert.AreEqual(Math.Sin(-6), RadMath.Sin(-6), Tolerance);
        }
        
        [TestMethod]
        public void TestFastSinPerf()
        {
            float x = 0; double y = 0;
            float step = (2*RadMath.PI) / 10000;

            Stopwatch s1 = Stopwatch.StartNew();
            for (int i = -5000000; i < 5000000; ++i)
                x = RadMath.Sin(i * step);
            s1.Stop();
            Console.WriteLine($"RadMath Sin: {s1.ElapsedMilliseconds}ms {x}");

            Stopwatch s2 = Stopwatch.StartNew();
            for (int i = -5000000; i < 5000000; ++i)
                y = Math.Sin(i * step);
            s2.Stop();
            Console.WriteLine($".NETMath Sin: {s2.ElapsedMilliseconds}ms {y}");

            Assert.IsTrue(s1.Elapsed.TotalSeconds < s2.Elapsed.TotalSeconds,
                "RadMath Sin implementation MUST be faster than .NET Math");

            Console.WriteLine($"RadMath Sin is {s2.Elapsed.TotalSeconds / s1.Elapsed.TotalSeconds:0.0}x faster");
        }

        [TestMethod]
        public void FastCos()
        {
            Assert.AreEqual(Math.Cos(0), RadMath.Cos(0), Tolerance);
            Assert.AreEqual(Math.Cos(1), RadMath.Cos(1), Tolerance);
            Assert.AreEqual(Math.Cos(-1), RadMath.Cos(-1), Tolerance);

            Assert.AreEqual(Math.Cos(Math.PI), RadMath.Cos(RadMath.PI), Tolerance);
            Assert.AreEqual(Math.Cos(2*Math.PI), RadMath.Cos(2*RadMath.PI),Tolerance);
            
            Assert.AreEqual(Math.Cos(-Math.PI), RadMath.Cos(-RadMath.PI), Tolerance);
            Assert.AreEqual(Math.Cos(-2*Math.PI), RadMath.Cos(-2*RadMath.PI), Tolerance);
            
            Assert.AreEqual(Math.Cos(Math.PI*1.5), RadMath.Cos(RadMath.PI*1.5f), Tolerance);
            Assert.AreEqual(Math.Cos(-Math.PI*1.5), RadMath.Cos(-RadMath.PI*1.5f), Tolerance);

            Assert.AreEqual(Math.Cos(6), RadMath.Cos(6), Tolerance);
            Assert.AreEqual(Math.Cos(-6), RadMath.Cos(-6), Tolerance);
        }
        
        [TestMethod]
        public void TestFastCosPerf()
        {
            float x = 0; double y = 0;
            float step = (2*RadMath.PI) / 10000;

            Stopwatch s1 = Stopwatch.StartNew();
            for (int i = -5000000; i < 5000000; ++i)
                x = RadMath.Cos(i * step);
            s1.Stop();
            Console.WriteLine($"RadMath Cos: {s1.ElapsedMilliseconds}ms {x}");

            Stopwatch s2 = Stopwatch.StartNew();
            for (int i = -5000000; i < 5000000; ++i)
                y = Math.Cos(i * step);
            s2.Stop();
            Console.WriteLine($".NETMath Cos: {s2.ElapsedMilliseconds}ms {y}");

            Assert.IsTrue(s1.Elapsed.TotalSeconds < s2.Elapsed.TotalSeconds,
                "RadMath Cos implementation MUST be faster than .NET Math");

            Console.WriteLine($"RadMath Cos is {s2.Elapsed.TotalSeconds / s1.Elapsed.TotalSeconds:0.0}x faster");
        }

        [TestMethod]
        public void FastRadsToDirection()
        {
            Assert.That.Equal(Tolerance, RadiansToDirectionOrig(-1f), (-1f).RadiansToDirection());

            Assert.That.Equal(Tolerance, RadiansToDirectionOrig(0f), (0f).RadiansToDirection());
            Assert.That.Equal(Tolerance, RadiansToDirectionOrig(1f), (1f).RadiansToDirection());
            Assert.That.Equal(Tolerance, RadiansToDirectionOrig(RadMath.TwoPI-1), (RadMath.TwoPI-1).RadiansToDirection());
        }
        
        // original radians to direction using .NET Sin/Cos
        static Vector2 RadiansToDirectionOrig(float radians)
        {
            return new Vector2((float)Math.Sin(radians), -(float)Math.Cos(radians));
        }

        [TestMethod]
        public void TestFastRadsToDirectionPerf()
        {
            Vector2 x = default;
            float step = (2*RadMath.PI) / 10000;

            Stopwatch s1 = Stopwatch.StartNew();
            for (int i = -5000000; i < 5000000; ++i)
                x = (i * step).RadiansToDirection();
            s1.Stop();
            Console.WriteLine($"RadMath RadsToDir: {s1.ElapsedMilliseconds}ms {x}");

            Stopwatch s2 = Stopwatch.StartNew();
            for (int i = -5000000; i < 5000000; ++i)
                x = RadiansToDirectionOrig(i * step);
            s2.Stop();
            Console.WriteLine($"Original RadsToDir: {s2.ElapsedMilliseconds}ms {x}");

            Assert.IsTrue(s1.Elapsed.TotalSeconds < s2.Elapsed.TotalSeconds,
                "RadMath RadsToDir implementation MUST be faster than Original");

            Console.WriteLine($"RadMath RadsToDir is {s2.Elapsed.TotalSeconds / s1.Elapsed.TotalSeconds:0.0}x faster");
        }
    }
}
