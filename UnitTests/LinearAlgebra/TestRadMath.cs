using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests.LinearAlgebra
{
    [TestClass]
    public class TestRadMath : StarDriveTest
    {
        const float Tolerance = 0.01f;

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
        public void RadiansFromDegrees()
        {
            const float ToRad = RadMath.DegreeToRadian;

            // special cases, 0 must be 0 and 360 must be 2PI
            Assert.AreEqual(ToRad * (0f), (0f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f), (360f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f), (359.999f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f), (360.001f).ToRadians(), Tolerance);

            Assert.AreEqual(ToRad * (115f), (115f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (345f), (345f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (920f % 360f), (920f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (7300f % 360f), (7300f).ToRadians(), Tolerance);

            Assert.AreEqual(ToRad * (360f - 115f), (-115f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f - 345f), (-345f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f - (920f % 360f)), (-920f).ToRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f - (7300f % 360f)), (-7300f).ToRadians(), Tolerance);
        }

        [TestMethod]
        public void RadiansNormalized()
        {
            Assert.AreEqual(0f, (0f).AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(RadMath.TwoPI, RadMath.TwoPI.AsNormalizedRadians(), Tolerance);

            Assert.AreEqual(1f, (1f).AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(3f, (3f).AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(8f % RadMath.TwoPI, (8f).AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(64f % RadMath.TwoPI, (64f).AsNormalizedRadians(), Tolerance);

            Assert.AreEqual(RadMath.TwoPI-1f, (-1f).AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(RadMath.TwoPI-3f, (-3f).AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(RadMath.TwoPI-(8f % RadMath.TwoPI), (-8f).AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(RadMath.TwoPI-(64f % RadMath.TwoPI), (-64f).AsNormalizedRadians(), Tolerance);
        }

        [TestMethod]
        public void RadiansFromDegreesAndNormalized()
        {
            const float ToRad = RadMath.DegreeToRadian;

            // special cases, 0 must be 0 and 360 must be 2PI
            Assert.AreEqual(ToRad * (0f), (0f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f), (360f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f), (359.999f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f), (360.001f).ToRadians().AsNormalizedRadians(), Tolerance);

            Assert.AreEqual(ToRad * (115f), (115f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (345f), (345f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (920f % 360f), (920f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (7300f % 360f), (7300f).ToRadians().AsNormalizedRadians(), Tolerance);

            Assert.AreEqual(ToRad * (360f - 115f), (-115f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f - 345f), (-345f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f - (920f % 360f)), (-920f).ToRadians().AsNormalizedRadians(), Tolerance);
            Assert.AreEqual(ToRad * (360f - (7300f % 360f)), (-7300f).ToRadians().AsNormalizedRadians(), Tolerance);
        }

        static Vector2 OriginalOrbitPos(Vector2 orbitAround, float orbitalRadians, float orbitRadius)
        {
            return orbitAround.PointFromRadians(orbitalRadians, orbitRadius);
        }

        [TestMethod]
        public void OrbitalOffsetRotate()
        {
            var pos = new Vector2(0, -100);
            float step = 5f.ToRadians();

            Console.WriteLine("OrbitalOffsetRotate Basics");
            Assert.That.Equal(0.1f, new Vector2(0,-100), RadMath.OrbitalOffsetRotate(pos, 100, 0f));
            Assert.That.Equal(0.1f, new Vector2(0,+100), RadMath.OrbitalOffsetRotate(pos, 100, RadMath.PI));
            Assert.That.Equal(0.1f, new Vector2(+100,0), RadMath.OrbitalOffsetRotate(pos, 100, RadMath.PI*0.5f));
            Assert.That.Equal(0.1f, new Vector2(-100,0), RadMath.OrbitalOffsetRotate(pos, 100, RadMath.PI*1.5f));

            Console.WriteLine("OrbitalOffsetRotate: [-4PI to 4PI]");
            for (float a = RadMath.PI*-4; a < RadMath.PI*4; a += step)
            {
                Assert.That.Equal(0.1f, OriginalOrbitPos(Vector2.Zero, a, 100),
                                RadMath.OrbitalOffsetRotate(pos, 100, a));
            }

            Console.WriteLine("OrbitalOffsetRotate: integrate [0 to 4PI]");
            // guarantee max precision of 1 unit across 2 full orbits
            Vector2 orbitalPos = pos;
            for (float a = 0f; a < RadMath.TwoPI*2; a += step)
            {
                Assert.That.Equal(1f, OriginalOrbitPos(Vector2.Zero, a, 100), orbitalPos);
                orbitalPos = RadMath.OrbitalOffsetRotate(orbitalPos, 100, step);
            }
        }
        
        [TestMethod]
        public void TestOrbitalOffsetRotatePerf()
        {
            var center = new Vector2(0,0);
            float orbitRadius = 100f;
            float orbitStep = 5f;
            float orbitStepRads = orbitStep.ToRadians();

            // Orbital offset assumes Center is [0,0]
            Vector2 originalOffset = Vectors.Up*orbitRadius;

            Stopwatch s1 = Stopwatch.StartNew();
            Vector2 orbitPos = originalOffset;
            for (int i = 0; i < 5000000; ++i)
            {
                orbitPos = RadMath.OrbitalOffsetRotate(originalOffset, orbitRadius, orbitStepRads);
            }
            s1.Stop();
            Console.WriteLine($"RadMath OrbitalOffsetRotate: {s1.ElapsedMilliseconds}ms {orbitPos}");

            Stopwatch s2 = Stopwatch.StartNew();
            float orbitalRadians = 0f;
            orbitPos = default;
            for (int i = 0; i < 5000000; ++i)
            {
                orbitPos = OriginalOrbitPos(center, orbitalRadians, orbitRadius);
                orbitalRadians += orbitStepRads;
            }
            s2.Stop();
            Console.WriteLine($"Original OriginalOrbitPos: {s2.ElapsedMilliseconds}ms {orbitPos}");

            Assert.IsTrue(s1.Elapsed.TotalSeconds < s2.Elapsed.TotalSeconds,
                "RadMath OrbitalOffsetRotate implementation MUST be faster than Original");

            Console.WriteLine($"RadMath OrbitalOffsetRotate is {s2.Elapsed.TotalSeconds / s1.Elapsed.TotalSeconds:0.0}x faster");
        }
    }
}
