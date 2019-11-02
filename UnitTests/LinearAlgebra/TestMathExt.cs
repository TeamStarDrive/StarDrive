using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests.LinearAlgebra
{
    [TestClass]
    public class TestMathExt
    {
        // double vs float math will have a tiny difference in accuracy
        // this is the max deviation we allow
        const double MaxErr = 0.0005;
        const float Radius = 20f;
        static readonly Vector2 A = RandomMath.Vector2D(Radius);
        static readonly Vector2 B = RandomMath.Vector2D(Radius);

        static readonly Vector2 Center  = RandomMath.Vector2D(Radius); // some center position
        static readonly Vector2 Inside  = Center / 2;
        static readonly Vector2 Outside = Center + new Vector2(Radius)*3;

        [TestMethod]
        public void Distance()
        {
            // test for consistency; reference implementations from XNA
            float dist1   = Vector2.Distance(A, B);
            float sqdist1 = Vector2.DistanceSquared(A, B);
            float dist2   = A.Distance(B);
            float sqdist2 = A.SqDist(B);
            Assert.AreEqual(dist1,   dist2,       MaxErr, "MathExt.Distance is inconsistent");
            Assert.AreEqual(sqdist1, sqdist2,     MaxErr, "MathExt.SqDist is inconsistent");
            Assert.AreEqual(sqdist2, dist2*dist2, MaxErr, "MathExt.Distance or MathExt.SqDist is inconsistent");
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
            Assert.AreEqual(00f,  Center.AngleToTarget(N),  MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(45f,  Center.AngleToTarget(NE), MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(90f,  Center.AngleToTarget(E),  MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(135f, Center.AngleToTarget(SE), MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(180f, Center.AngleToTarget(S),  MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(225f, Center.AngleToTarget(SW), MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(270f, Center.AngleToTarget(W),  MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(315f, Center.AngleToTarget(NW), MaxErr, "Degrees to target is incorrect");
        }

        [TestMethod]
        public void AngleToTargetSigned()
        {
            Assert.AreEqual(+00f,  Center.AngleToTargetSigned(N),  MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(+45f,  Center.AngleToTargetSigned(NE), MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(+90f,  Center.AngleToTargetSigned(E),  MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(+135f, Center.AngleToTargetSigned(SE), MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(+180f, Center.AngleToTargetSigned(S),  MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(-225f, Center.AngleToTargetSigned(SW), MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(-270f, Center.AngleToTargetSigned(W),  MaxErr, "Degrees to target is incorrect");
            Assert.AreEqual(-315f, Center.AngleToTargetSigned(NW), MaxErr, "Degrees to target is incorrect");
        }

        [TestMethod]
        public void RadiansToTarget()
        {
            Assert.AreEqual(System.Math.PI*0.00, Center.RadiansToTarget(N),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(System.Math.PI*0.25, Center.RadiansToTarget(NE), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(System.Math.PI*0.50, Center.RadiansToTarget(E),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(System.Math.PI*0.75, Center.RadiansToTarget(SE), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(System.Math.PI*1.00, Center.RadiansToTarget(S),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(System.Math.PI*1.25, Center.RadiansToTarget(SW), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(System.Math.PI*1.50, Center.RadiansToTarget(W),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(System.Math.PI*1.75, Center.RadiansToTarget(NW), MaxErr, "Radians to target is incorrect");
        }

        [TestMethod]
        public void RadiansToTargetSigned()
        {
            Assert.AreEqual(+System.Math.PI*0.00, Center.RadiansToTargetSigned(N),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(+System.Math.PI*0.25, Center.RadiansToTargetSigned(NE), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(+System.Math.PI*0.50, Center.RadiansToTargetSigned(E),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(+System.Math.PI*0.75, Center.RadiansToTargetSigned(SE), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(+System.Math.PI*1.00, Center.RadiansToTargetSigned(S),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(-System.Math.PI*1.25, Center.RadiansToTargetSigned(SW), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(-System.Math.PI*1.50, Center.RadiansToTargetSigned(W),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(-System.Math.PI*1.75, Center.RadiansToTargetSigned(NW), MaxErr, "Radians to target is incorrect");
        }

        [TestMethod]
        public void DegreesAndRadians()
        {
            Assert.AreEqual(180f, ((float)System.Math.PI).ToDegrees(), MaxErr, "Radians to Degrees failed");
            Assert.AreEqual((float)System.Math.PI, 180f.ToRadians(), MaxErr, "Degrees to Radians failed");
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
            Assert.AreEqual(50,  31.RoundUpToMultipleOf(50));
            Assert.AreEqual(100, 81.RoundUpToMultipleOf(50));

            Assert.AreEqual(0,  31.RoundDownToMultipleOf(50));
            Assert.AreEqual(50, 81.RoundDownToMultipleOf(50));

            Assert.AreEqual(10, 0.5f.RoundTo10());
            Assert.AreEqual(80, 75.5f.RoundTo10());
        }
    }
}
