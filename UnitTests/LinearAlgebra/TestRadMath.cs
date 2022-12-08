using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.LinearAlgebra
{
    [TestClass]
    public class TestRadMath : StarDriveTest
    {
        const float Tolerance = 0.01f;

        [TestMethod]
        public void FastSin()
        {
            AssertEqual(Tolerance, Math.Sin(0), RadMath.Sin(0));
            AssertEqual(Tolerance, Math.Sin(1), RadMath.Sin(1));
            AssertEqual(Tolerance, Math.Sin(-1), RadMath.Sin(-1));

            AssertEqual(Tolerance, Math.Sin(Math.PI), RadMath.Sin(RadMath.PI));
            AssertEqual(Tolerance, Math.Sin(2*Math.PI), RadMath.Sin(2*RadMath.PI));
            
            AssertEqual(Tolerance, Math.Sin(-Math.PI), RadMath.Sin(-RadMath.PI));
            AssertEqual(Tolerance, Math.Sin(-2*Math.PI), RadMath.Sin(-2*RadMath.PI));
            
            AssertEqual(Tolerance, Math.Sin(Math.PI*1.5), RadMath.Sin(RadMath.PI*1.5f));
            AssertEqual(Tolerance, Math.Sin(-Math.PI*1.5), RadMath.Sin(-RadMath.PI*1.5f));

            AssertEqual(Tolerance, Math.Sin(6), RadMath.Sin(6));
            AssertEqual(Tolerance, Math.Sin(-6), RadMath.Sin(-6));
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

            AssertLessThan(s1.Elapsed.TotalSeconds, s2.Elapsed.TotalSeconds,
                "RadMath Sin implementation MUST be faster than .NET Math");

            Console.WriteLine($"RadMath Sin is {s2.Elapsed.TotalSeconds / s1.Elapsed.TotalSeconds:0.0}x faster");
        }

        [TestMethod]
        public void FastCos()
        {
            AssertEqual(Tolerance, Math.Cos(0), RadMath.Cos(0));
            AssertEqual(Tolerance, Math.Cos(1), RadMath.Cos(1));
            AssertEqual(Tolerance, Math.Cos(-1), RadMath.Cos(-1));

            AssertEqual(Tolerance, Math.Cos(Math.PI), RadMath.Cos(RadMath.PI));
            AssertEqual(Tolerance, Math.Cos(2*Math.PI), RadMath.Cos(2*RadMath.PI));
            
            AssertEqual(Tolerance, Math.Cos(-Math.PI), RadMath.Cos(-RadMath.PI));
            AssertEqual(Tolerance, Math.Cos(-2*Math.PI), RadMath.Cos(-2*RadMath.PI));
            
            AssertEqual(Tolerance, Math.Cos(Math.PI*1.5), RadMath.Cos(RadMath.PI*1.5f));
            AssertEqual(Tolerance, Math.Cos(-Math.PI*1.5), RadMath.Cos(-RadMath.PI*1.5f));

            AssertEqual(Tolerance, Math.Cos(6), RadMath.Cos(6));
            AssertEqual(Tolerance, Math.Cos(-6), RadMath.Cos(-6));
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

            AssertLessThan(s1.Elapsed.TotalSeconds, s2.Elapsed.TotalSeconds,
                "RadMath Cos implementation MUST be faster than .NET Math");

            Console.WriteLine($"RadMath Cos is {s2.Elapsed.TotalSeconds / s1.Elapsed.TotalSeconds:0.0}x faster");
        }

        [TestMethod]
        public void FastRadsToDirection()
        {
            AssertEqual(Tolerance, RadiansToDirectionOrig(-1f), (-1f).RadiansToDirection());

            AssertEqual(Tolerance, RadiansToDirectionOrig(0f), (0f).RadiansToDirection());
            AssertEqual(Tolerance, RadiansToDirectionOrig(1f), (1f).RadiansToDirection());
            AssertEqual(Tolerance, RadiansToDirectionOrig(RadMath.TwoPI-1), (RadMath.TwoPI-1).RadiansToDirection());
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
            AssertEqual(Tolerance, ToRad * (0f), (0f).ToRadians());
            AssertEqual(Tolerance, ToRad * (360f), (360f).ToRadians());
            AssertEqual(Tolerance, ToRad * (360f), (359.999f).ToRadians());
            AssertEqual(Tolerance, ToRad * (360f), (360.001f).ToRadians());

            AssertEqual(Tolerance, ToRad * (115f), (115f).ToRadians());
            AssertEqual(Tolerance, ToRad * (345f), (345f).ToRadians());
            AssertEqual(Tolerance, ToRad * (920f % 360f), (920f).ToRadians());
            AssertEqual(Tolerance, ToRad * (7300f % 360f), (7300f).ToRadians());

            AssertEqual(Tolerance, ToRad * (360f - 115f), (-115f).ToRadians());
            AssertEqual(Tolerance, ToRad * (360f - 345f), (-345f).ToRadians());
            AssertEqual(Tolerance, ToRad * (360f - (920f % 360f)), (-920f).ToRadians());
            AssertEqual(Tolerance, ToRad * (360f - (7300f % 360f)), (-7300f).ToRadians());
        }

        [TestMethod]
        public void RadiansNormalized()
        {
            AssertEqual(Tolerance, 0f, (0f).AsNormalizedRadians());
            AssertEqual(Tolerance, RadMath.TwoPI, RadMath.TwoPI.AsNormalizedRadians());

            AssertEqual(Tolerance, 1f, (1f).AsNormalizedRadians());
            AssertEqual(Tolerance, 3f, (3f).AsNormalizedRadians());
            AssertEqual(Tolerance, 8f % RadMath.TwoPI, (8f).AsNormalizedRadians());
            AssertEqual(Tolerance, 64f % RadMath.TwoPI, (64f).AsNormalizedRadians());

            AssertEqual(Tolerance, RadMath.TwoPI-1f, (-1f).AsNormalizedRadians());
            AssertEqual(Tolerance, RadMath.TwoPI-3f, (-3f).AsNormalizedRadians());
            AssertEqual(Tolerance, RadMath.TwoPI-(8f % RadMath.TwoPI), (-8f).AsNormalizedRadians());
            AssertEqual(Tolerance, RadMath.TwoPI-(64f % RadMath.TwoPI), (-64f).AsNormalizedRadians());
        }

        [TestMethod]
        public void RadiansFromDegreesAndNormalized()
        {
            const float ToRad = RadMath.DegreeToRadian;

            // special cases, 0 must be 0 and 360 must be 2PI
            AssertEqual(Tolerance, ToRad * (0f), (0f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (360f), (360f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (360f), (359.999f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (360f), (360.001f).ToRadians().AsNormalizedRadians());

            AssertEqual(Tolerance, ToRad * (115f), (115f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (345f), (345f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (920f % 360f), (920f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (7300f % 360f), (7300f).ToRadians().AsNormalizedRadians());

            AssertEqual(Tolerance, ToRad * (360f - 115f), (-115f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (360f - 345f), (-345f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (360f - (920f % 360f)), (-920f).ToRadians().AsNormalizedRadians());
            AssertEqual(Tolerance, ToRad * (360f - (7300f % 360f)), (-7300f).ToRadians().AsNormalizedRadians());
        }

        static Vector2 OriginalOrbitPos(Vector2 orbitAround, float orbitalRadians, float orbitRadius)
        {
            return MathExt.PointFromRadians(orbitAround, orbitalRadians, orbitRadius);
        }

        [TestMethod]
        public void OrbitalOffsetRotate()
        {
            var pos = new Vector2(0, -100);
            float step = 5f.ToRadians();

            Console.WriteLine("OrbitalOffsetRotate Basics");
            AssertEqual(0.1f, new Vector2(0,-100), RadMath.OrbitalOffsetRotate(pos, 100, 0f));
            AssertEqual(0.1f, new Vector2(0,+100), RadMath.OrbitalOffsetRotate(pos, 100, RadMath.PI));
            AssertEqual(0.1f, new Vector2(+100,0), RadMath.OrbitalOffsetRotate(pos, 100, RadMath.PI*0.5f));
            AssertEqual(0.1f, new Vector2(-100,0), RadMath.OrbitalOffsetRotate(pos, 100, RadMath.PI*1.5f));

            Console.WriteLine("OrbitalOffsetRotate: [-4PI to 4PI]");
            for (float a = RadMath.PI*-4; a < RadMath.PI*4; a += step)
            {
                AssertEqual(0.1f, OriginalOrbitPos(Vector2.Zero, a, 100),
                                RadMath.OrbitalOffsetRotate(pos, 100, a));
            }

            Console.WriteLine("OrbitalOffsetRotate: integrate [0 to 4PI]");
            // guarantee max precision of 1 unit across 2 full orbits
            Vector2 orbitalPos = pos;
            for (float a = 0f; a < RadMath.TwoPI*2; a += step)
            {
                AssertEqual(1f, OriginalOrbitPos(Vector2.Zero, a, 100), orbitalPos);
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
