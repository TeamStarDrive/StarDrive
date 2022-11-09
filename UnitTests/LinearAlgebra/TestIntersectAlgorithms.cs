using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.LinearAlgebra
{
    [TestClass]
    public class TestIntersectAlgorithms : StarDriveTest
    {
        [TestMethod]
        public void RayCircleIntersects()
        {
            var center = new Vector2(0, 0);

            // Intersect through the circle from OUTSIDE, DIAGONALLY
            // a \
            //   ( )
            //     \ b
            var start = new Vector2(-30, -20);
            var end   = new Vector2(+30, +20);
            Assert.IsTrue(center.RayCircleIntersect(30f, start, end, out float intersect));
            AssertEqual(0.01f, 6.05f, intersect);
            //   /
            // ( )
            // /
            start = new Vector2(+30, -20);
            end   = new Vector2(-30, +20);
            Assert.IsTrue(center.RayCircleIntersect(30f, start, end, out intersect));
            AssertEqual(0.01f, 6.05f, intersect); // we are perfectly touching the edge

            // Huge radius, but the line starts from inside
            //            )
            //  c    /    )
            //      /     )
            start = new Vector2(+40, -20);
            end   = new Vector2(+20, +20);
            Assert.IsTrue(center.RayCircleIntersect(158, start, end, out intersect));
            AssertEqual(1f, intersect); // we are perfectly touching the edge

            // Intersect through the circle from OUTSIDE, HORIZONTALLY
            // a ---|  |--> b
            start = new Vector2(-20, 0);
            end   = new Vector2(+20, 0);
            Assert.IsTrue(center.RayCircleIntersect(20f, start, end, out intersect));
            AssertEqual(0f, intersect); // we are perfectly touching the edge
            Assert.IsTrue(center.RayCircleIntersect(10f, start, end, out intersect));
            AssertEqual(10f, intersect);

            // Intersect while inside the circe, horizontally
            // | --> |
            start = new Vector2(-20, 0);
            end   = new Vector2(+20, 0);
            Assert.IsTrue(center.RayCircleIntersect(40f, start, end, out intersect));
            AssertEqual(1f, intersect);

            // Intersect STARTS from inside the circle, horizontally
            // ---|-->o   |
            start = new Vector2(-30, 0);
            end   = new Vector2(0, 0);
            Assert.IsTrue(center.RayCircleIntersect(20f, start, end, out intersect));
            AssertEqual(10f, intersect);

            // Intersect STARTS from inside the circle, horizontally
            // |   o  -|--->
            start = new Vector2(10, 0);
            end   = new Vector2(40, 0);
            Assert.IsTrue(center.RayCircleIntersect(20f, start, end, out intersect));
            AssertEqual(1f, intersect);
        }

        [TestMethod]
        public void RayCircleNoIntersection()
        {
            var center = new Vector2(0, 0);

            // NO intersect, we are outside of the circle, horizontally
            // |    |  ---->
            var start = new Vector2(21, 0);
            var end   = new Vector2(50, 0);
            Assert.IsFalse(center.RayCircleIntersect(20f, start, end, out float intersect));
            AssertEqual(float.NaN, intersect); // distance is 20 from the edge of the circle

            // NO intersect, we are outside of the circle, horizontally
            // ---->  |  o  |
            start = new Vector2(-80, 0);
            end   = new Vector2(-40, 0);
            Assert.IsFalse(center.RayCircleIntersect(20f, start, end, out intersect));
            AssertEqual(float.NaN, intersect); // distance is 20 from the edge of the circle

            // From edge to center
            // |-->o   |
            start = new Vector2(-20, 0);
            end   = new Vector2(0, 0);
            Assert.IsTrue(center.RayCircleIntersect(20f, start, end, out intersect));
            AssertEqual(0f, intersect); // distance is 20 from the edge of the circle

            // From edge outwards
            // |  *  |---->
            start = new Vector2(20, 0);
            end   = new Vector2(40, 0);
            Assert.IsTrue(center.RayCircleIntersect(20f, start, end, out intersect));
            AssertEqual(0f, intersect); // distance is 20 from the edge of the circle

            // Trying to get determinant to 0
            // | -*>  |
            start = new Vector2(-4.0f, 0);
            end   = new Vector2(+4.0f, 0);
            Assert.IsTrue(center.RayCircleIntersect(4f, start, end, out intersect));
            AssertEqual(0f, intersect); // distance is 20 from the edge of the circle
        }

        [TestMethod]
        public void ClosestPointOnLineSimple()
        {
            // Horizontal line, perfectly balanced for an easy picking
            //   o
            // --x->
            var start = new Vector2(-10, 10);
            var end   = new Vector2(+20, 10);
            Vector2 point = new Vector2(5,0).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, new Vector2(5, 10), point);

            // Horizontal line with START point as the closest
            //   o
            //   x--->
            start = new Vector2(5, 10);
            end   = new Vector2(15, 10);
            point = new Vector2(5,0).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, start, point);

            // Horizontal line with END point as the closest
            //     o
            // --->x
            start = new Vector2(-10, 10);
            end   = new Vector2(5, 10);
            point = new Vector2(5,0).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, end, point);

            // Horizontal line with START point as the closest
            //   o  x--->
            start = new Vector2(11, 0);
            end   = new Vector2(23, 0);
            point = new Vector2(5,0).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, start, point);

            // Horizontal line with END point as the closest
            // --->x  o
            start = new Vector2(-20, 0);
            end   = new Vector2(-10, 0);
            point = new Vector2(5,0).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, end, point);
        }

        [TestMethod]
        public void ClosestPointOnLineIntersectCenter()
        {
            // Horizontal line with center itself as the closest
            // ---o---
            var start = new Vector2(-15, 5);
            var end   = new Vector2(+15, 5);
            Vector2 point = new Vector2(5).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, new Vector2(5), point);

            // Vertical line with center itself as the closest
            // |
            // o
            // |
            start = new Vector2(5, -15);
            end   = new Vector2(5, +15);
            point = new Vector2(5).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, new Vector2(5), point);

            // Diagonal line with center itself as the closest
            // \
            //  o
            //   \
            start = new Vector2(-10, -10);
            end   = new Vector2(+20, +20);
            point = new Vector2(5).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, new Vector2(5), point);

            // Diagonal line with center itself as the closest
            //   /
            //  o
            // /
            start = new Vector2(+20, -10);
            end   = new Vector2(-10, +20);
            point = new Vector2(5).FindClosestPointOnLine(start, end);
            AssertEqual(0.001f, new Vector2(5), point);
        }
    }
}
