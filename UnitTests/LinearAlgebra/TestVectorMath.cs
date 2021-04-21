using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests.LinearAlgebra
{
    [TestClass]
    public class TestVectorMath
    {
        static Vector2 Vec(float x, float y) => new Vector2(x, y);

        [TestMethod]
        public void TestDotProduct()
        {
            // same direction yields 1
            Assert.AreEqual(1f,  Vec(1, 0).Dot( Vec(1, 0) ));
            Assert.AreEqual(1f,  Vec(0, 1).Dot( Vec(0, 1) ));

            // magnitude matters, so we need normalize
            Assert.AreEqual(1f,  Vec(8, 0).Normalized().Dot( Vec(10, 0).Normalized() ));

            // reverse direction yields -1
            Assert.AreEqual(-1f,  Vec(-1, 0).Dot( Vec(+1, 0) ));
            Assert.AreEqual(-1f,  Vec(+1, 0).Dot( Vec(-1, 0) )); // order doesn't matter

            // perpendicular directions yield 0
            Assert.AreEqual(0f,  Vec(-1, 0).Dot( Vec(0, +1) ));
            Assert.AreEqual(0f,  Vec(-1, 0).Dot( Vec(0, -1) ));
            Assert.AreEqual(0f,  Vec(+1, 0).Dot( Vec(0, +1) ));
            Assert.AreEqual(0f,  Vec(+1, 0).Dot( Vec(0, -1) ));

            // and magnitude does not matter
            Assert.AreEqual(0f,  Vec(+2, +2).Dot( Vec(-2, +2) ));
            Assert.AreEqual(0f,  Vec(+2, -2).Dot( Vec(+2, +2) ));
        }

        [TestMethod]
        public void TestIsVectorReverseOf()
        {
            Assert.IsTrue(Vec(+1, -1).IsOppositeOf(Vec(-1, +1)));
            Assert.IsTrue(Vec(-1, +1).IsOppositeOf(Vec(+1, -1))); // order is not important

            // vectors pointing at same dir are not reverse
            Assert.IsFalse(Vec(0,1).IsOppositeOf(Vec(0,1)));

            // perpendicular vectors are not reverse
            Assert.IsFalse(Vec(0,1).IsOppositeOf(Vec(1,0)));
            // perpendicular towards top-left vs towards top-right
            Assert.IsFalse(Vec(-1,-1).IsOppositeOf(Vec(+1, -1)));

            // magnitude of vectors is not important
            Assert.IsTrue(Vec(-1,-1).IsOppositeOf(Vec(100,100)));
            Assert.IsFalse(Vec(1,1).IsOppositeOf(Vec(100,100)));

            // perfectly reversed vectors, some float error expected
            Assert.IsTrue(Vec(1,1).IsOppositeOf(Vec(-1,-1)));
            // super strict:
            Assert.IsTrue(Vec(1,1).IsOppositeOf(Vec(-1,-1), tolerance:1.0f));

            // vectors that cross, but are not reverse:
            // -->  vs   /
            //          V
            Assert.IsFalse(Vec(1,0).IsOppositeOf(Vec(-0.5f,0.5f)));
            // however, if we lower tolerance, then it's ok:
            Assert.IsTrue(Vec(1,0).IsOppositeOf(Vec(-0.5f,0.5f), tolerance:0.25f));
        }

        [TestMethod]
        public void TestAngleDifference()
        {
            float AngleDifference((float x, float y) a, (float x, float y) b)
                => Vectors.AngleDifference(new Vector2(a.x, a.y), new Vector2(b.x, b.y));

            // facing opposite side, angle difference should 1 PI, regardless of orientation
            Assert.AreEqual(RadMath.PI, AngleDifference( (0, +1), (0, -1) ));
            Assert.AreEqual(RadMath.PI, AngleDifference( (0, -1), (0, +1) ));
            Assert.AreEqual(RadMath.PI, AngleDifference( (+1, 0), (-1, 0) ));
            Assert.AreEqual(RadMath.PI, AngleDifference( (-1, 0), (+1, 0) ));

            // if facing same direction, make sure we don't get a NaN
            Assert.AreEqual(0f, AngleDifference( (1,1), (1,1) ));
            Assert.AreEqual(0f, AngleDifference( (-1,1), (-1,1) ));
            Assert.AreEqual(0f, AngleDifference( (1,-1), (1,-1) ));
            Assert.AreEqual(0f, AngleDifference( (-1,-1), (-1,-1) ));

            // 90 degs
            Assert.AreEqual(RadMath.HalfPI, AngleDifference( (0,1), (1,0) ));
            Assert.AreEqual(RadMath.HalfPI, AngleDifference( (0,1), (-1,0) ));
        }

        [TestMethod]
        public void TestAngleDifferenceNANBug()
        {
            var wantedForward  = new Vector2(0.79299283f, -0.609231055f);
            var currentForward = new Vector2(0.793037832f, -0.6091724f);
            float difference = Vectors.AngleDifference(wantedForward, currentForward);
            Assert.IsFalse(float.IsNaN(difference), "Vectors.AngleDifference() should not be NAN");
        }
    }
}
