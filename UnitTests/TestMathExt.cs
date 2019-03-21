using System;
using Microsoft.Xna.Framework;
using Ship_Game;
using static System.Math;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class TestMathExt
    {
        // double vs float math will have a tiny difference in accuracy
        // this is the max deviation we allow
        private const double MaxErr = 0.0005;
        private const float Radius = 20f;
        private static readonly Vector2 A = RandomMath.Vector2D(Radius);
        private static readonly Vector2 B = RandomMath.Vector2D(Radius);

        private static readonly Vector2 Center  = RandomMath.Vector2D(Radius); // some center position
        private static readonly Vector2 Inside  = Center / 2;
        private static readonly Vector2 Outside = Center + new Vector2(Radius)*3;

        [TestMethod]
        public void TestDistance()
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
        public void TestInRadius()
        {
            Assert.IsTrue(Inside.InRadius(Center, Radius),  "InRadius failed, inside point should return true");
            Assert.IsTrue(Outside.InRadius(Center, Radius) == false, "InRadius failed, outside point should return false");
        }

        // StarDrive +Y is South and -Y is North
        private static readonly Vector2 N  = Center + new Vector2(0f, -50f);
        private static readonly Vector2 S  = Center + new Vector2(0f, +50f);
        private static readonly Vector2 E  = Center + new Vector2(+50f, 0f);
        private static readonly Vector2 W  = Center + new Vector2(-50f, 0f);
        private static readonly Vector2 NE = Center + new Vector2(+50f, -50f);
        private static readonly Vector2 NW = Center + new Vector2(-50f, -50f);
        private static readonly Vector2 SE = Center + new Vector2(+50f, +50f);
        private static readonly Vector2 SW = Center + new Vector2(-50f, +50f);

        [TestMethod]
        public void TestAngleToTarget()
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
        public void TestAngleToTargetSigned()
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
        public void TestRadiansToTarget()
        {
            Assert.AreEqual(PI*0.00, Center.RadiansToTarget(N),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(PI*0.25, Center.RadiansToTarget(NE), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(PI*0.50, Center.RadiansToTarget(E),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(PI*0.75, Center.RadiansToTarget(SE), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(PI*1.00, Center.RadiansToTarget(S),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(PI*1.25, Center.RadiansToTarget(SW), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(PI*1.50, Center.RadiansToTarget(W),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(PI*1.75, Center.RadiansToTarget(NW), MaxErr, "Radians to target is incorrect");
        }

        [TestMethod]
        public void TestRadiansToTargetSigned()
        {
            Assert.AreEqual(+PI*0.00, Center.RadiansToTargetSigned(N),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(+PI*0.25, Center.RadiansToTargetSigned(NE), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(+PI*0.50, Center.RadiansToTargetSigned(E),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(+PI*0.75, Center.RadiansToTargetSigned(SE), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(+PI*1.00, Center.RadiansToTargetSigned(S),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(-PI*1.25, Center.RadiansToTargetSigned(SW), MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(-PI*1.50, Center.RadiansToTargetSigned(W),  MaxErr, "Radians to target is incorrect");
            Assert.AreEqual(-PI*1.75, Center.RadiansToTargetSigned(NW), MaxErr, "Radians to target is incorrect");
        }

        [TestMethod]
        public void TestDegreesAndRadians()
        {
            Assert.AreEqual(180f, ((float)PI).ToDegrees(), MaxErr, "Radians to Degrees failed");
            Assert.AreEqual((float)PI, 180f.ToRadians(), MaxErr, "Degrees to Radians failed");
        }
    }
}
