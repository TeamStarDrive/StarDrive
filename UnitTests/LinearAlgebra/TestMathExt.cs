using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using SDGraphics;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using Ship_Game.Utils;

namespace UnitTests.LinearAlgebra
{
    [TestClass]
    public class TestMathExt : StarDriveTest
    {
        // double vs float math will have a tiny difference in accuracy
        // this is the max deviation we allow
        const double MaxErr = 0.0005;
        const float Radius = 20f;
        static readonly RandomBase Random = new SeededRandom();
        static readonly Vector2 A = Random.Vector2D(Radius);
        static readonly Vector2 B = Random.Vector2D(Radius);

        static readonly Vector2 Center  = Random.Vector2D(Radius); // some center position
        static readonly Vector2 Inside  = Center / 2;
        static readonly Vector2 Outside = Center + new Vector2(Radius)*3;

        [TestMethod]
        public void Distance()
        {
            // test for consistency; reference implementations from XNA
            float dist1   = XnaVector2.Distance(A, B);
            float sqdist1 = XnaVector2.DistanceSquared(A, B);
            float dist2   = A.Distance(B);
            float sqdist2 = A.SqDist(B);
            AssertEqual(MaxErr, dist1,   dist2,       "MathExt.Distance is inconsistent");
            AssertEqual(MaxErr, sqdist1, sqdist2,     "MathExt.SqDist is inconsistent");
            AssertEqual(MaxErr, sqdist2, dist2*dist2, "MathExt.Distance or MathExt.SqDist is inconsistent");
        }

        [TestMethod]
        public void InRadius()
        {
            Assert.IsTrue(Inside.InRadius(Center, Radius),  "InRadius failed, inside point should return true");
            Assert.IsTrue(Outside.InRadius(Center, Radius) == false, "InRadius failed, outside point should return false");
        }

        // StarDrive +Y is South and -Y is North
        static readonly Vector2 N  = Center + new Vector2(0f, -50f);
        static readonly Vector2 S  = Center + new Vector2(0f, +50f);
        static readonly Vector2 E  = Center + new Vector2(+50f, 0f);
        static readonly Vector2 W  = Center + new Vector2(-50f, 0f);
        static readonly Vector2 NE = Center + new Vector2(+50f, -50f);
        static readonly Vector2 NW = Center + new Vector2(-50f, -50f);
        static readonly Vector2 SE = Center + new Vector2(+50f, +50f);
        static readonly Vector2 SW = Center + new Vector2(-50f, +50f);

        [TestMethod]
        public void AngleToTarget()
        {
            AssertEqual(MaxErr, 00f,  Center.AngleToTarget(N),  "Degrees to target is incorrect");
            AssertEqual(MaxErr, 45f,  Center.AngleToTarget(NE), "Degrees to target is incorrect");
            AssertEqual(MaxErr, 90f,  Center.AngleToTarget(E),  "Degrees to target is incorrect");
            AssertEqual(MaxErr, 135f, Center.AngleToTarget(SE), "Degrees to target is incorrect");
            AssertEqual(MaxErr, 180f, Center.AngleToTarget(S),  "Degrees to target is incorrect");
            AssertEqual(MaxErr, 225f, Center.AngleToTarget(SW), "Degrees to target is incorrect");
            AssertEqual(MaxErr, 270f, Center.AngleToTarget(W),  "Degrees to target is incorrect");
            AssertEqual(MaxErr, 315f, Center.AngleToTarget(NW), "Degrees to target is incorrect");
        }

        [TestMethod]
        public void TestRadiansToTarget()
        {
            AssertEqual(MaxErr, System.Math.PI*0.00, Center.RadiansToTarget(N),  "Radians to target is incorrect");
            AssertEqual(MaxErr, System.Math.PI*0.25, Center.RadiansToTarget(NE), "Radians to target is incorrect");
            AssertEqual(MaxErr, System.Math.PI*0.50, Center.RadiansToTarget(E),  "Radians to target is incorrect");
            AssertEqual(MaxErr, System.Math.PI*0.75, Center.RadiansToTarget(SE), "Radians to target is incorrect");
            AssertEqual(MaxErr, System.Math.PI*1.00, Center.RadiansToTarget(S),  "Radians to target is incorrect");
            AssertEqual(MaxErr, System.Math.PI*1.25, Center.RadiansToTarget(SW), "Radians to target is incorrect");
            AssertEqual(MaxErr, System.Math.PI*1.50, Center.RadiansToTarget(W),  "Radians to target is incorrect");
            AssertEqual(MaxErr, System.Math.PI*1.75, Center.RadiansToTarget(NW), "Radians to target is incorrect");
        }

        [TestMethod]
        public void TestDegreesAndRadians()
        {
            AssertEqual(MaxErr, 180f, ((float)System.Math.PI).ToDegrees(), "Radians to Degrees failed");
            AssertEqual(MaxErr, (float)System.Math.PI, 180f.ToRadians(), "Degrees to Radians failed");
        }

        
        [TestMethod]
        public void AlmostEqual()
        {
            Assert.IsTrue(0f.AlmostEqual( MathExt.DefaultTolerance));
            Assert.IsTrue(0f.AlmostEqual(-MathExt.DefaultTolerance));
            Assert.IsTrue(0f.AlmostEqual( MathExt.DefaultTolerance - 0.0000001f));
            Assert.IsTrue(0f.AlmostEqual(-MathExt.DefaultTolerance + 0.0000001f));
            Assert.IsFalse(0f.AlmostEqual( MathExt.DefaultTolerance + 0.0000001f));
            Assert.IsFalse(0f.AlmostEqual(-MathExt.DefaultTolerance - 0.0000001f));

            Assert.IsTrue(0f.NotEqual(MathExt.DefaultTolerance*2));
            Assert.IsTrue(0f.NotEqual(MathExt.DefaultTolerance*-2));
            Assert.IsFalse(0f.NotEqual( MathExt.DefaultTolerance - 0.0000001f));
            Assert.IsFalse(0f.NotEqual(-MathExt.DefaultTolerance + 0.0000001f));

            Assert.IsTrue(MathExt.DefaultTolerance.AlmostZero());
            Assert.IsTrue((-MathExt.DefaultTolerance).AlmostZero());
            Assert.IsFalse(( MathExt.DefaultTolerance + 0.0000001f).AlmostZero());
            Assert.IsFalse((-MathExt.DefaultTolerance - 0.0000001f).AlmostZero());

            Assert.IsTrue((MathExt.DefaultTolerance + 0.0000001f).NotZero());
            Assert.IsTrue((-MathExt.DefaultTolerance - 0.0000001f).NotZero());
            Assert.IsFalse((MathExt.DefaultTolerance - 0.0000001f).NotZero());
            Assert.IsFalse((-MathExt.DefaultTolerance + 0.0000001f).NotZero());
        }

        [TestMethod]
        public void RoundingUtils()
        {
            AssertEqual(50,  31.RoundUpToMultipleOf(50));
            AssertEqual(100, 81.RoundUpToMultipleOf(50));

            AssertEqual(0,  31.RoundDownToMultipleOf(50));
            AssertEqual(50, 81.RoundDownToMultipleOf(50));

            AssertEqual(10, 0.5f.RoundTo10());
            AssertEqual(80, 75.5f.RoundTo10());
        }
    }
}
