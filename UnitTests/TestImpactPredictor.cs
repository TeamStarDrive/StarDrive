using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
// ReSharper disable JoinDeclarationAndInitializer

namespace UnitTests
{
    [TestClass]
    public partial class TestImpactPredictor
    {
        Vector2 Pos(float x, float y) => new Vector2(x, y);
        
        static readonly Vector2 Zero    = new Vector2(0,0);
        static readonly Vector2 ZeroVel = new Vector2(0,0);
        static readonly Vector2 ZeroAcc = new Vector2(0,0);

        [TestMethod]
        public void TimeToTarget()
        {
            Assert.AreEqual(1f, ImpactPredictor.TimeToTarget(Pos(0,0), Pos(0,100), 100));
            Assert.AreEqual(0f, ImpactPredictor.TimeToTarget(Pos(0,0), Pos(0,100), 0));
        }

        [TestMethod]
        public void TargetStoppedUsStopped()
        {
            // Target is stopped right in front of us:
            //   * Tgt no vel
            //
            //   * Us  no vel
            var s = new Scenario(tgt:Pos(0,0), us:Pos(0,500), tgtVel:ZeroVel, usVel:ZeroVel);
            s.TestPredictStationary();
        }

        [TestMethod]
        public void TargetStoppedUsMovingToTarget()
        {
            //   * Tgt
            //
            //   A
            //   | 
            //   * Us
            var s1 = new Scenario(tgt:Pos(0, 500), us:Pos(0, 0), tgtVel:Zero, usVel:Vectors.Up * 100);
            s1.TestPredictStationary();

            //   * Us +Y
            //   |
            //   V
            //
            //   * Tgt
            var s2 = new Scenario(tgt:Pos(10, 10),  us:Pos(10, -10), tgtVel:Zero, usVel:Vectors.Down * 100);
            s2.TestPredictStationary();

            //  Us    Tgt
            //   *-->  *
            var s3 = new Scenario(tgt:Pos(10, 5),   us:Pos(-10, 5),  tgtVel:Zero, usVel:Vectors.Right * 100);
            s3.TestPredictStationary();

            //  Tgt    Us
            //   *  <--*
            var s4 = new Scenario(tgt:Pos(-10, 5),  us:Pos(10, 5),   tgtVel:Zero, usVel:Vectors.Left * 100);
            s4.TestPredictStationary();

            //   * Us diagonally flying towards Them
            //    \
            //     V
            //
            //       * Them
            var s5 = new Scenario(tgt:Pos(10, 10), us:Pos(-10, -10),  tgtVel:Zero, usVel:Vectors.BotRight * 100);
            s5.TestPredictStationary();

            //   * Them
            //
            //     A
            //      \
            //       * Us diagonally flying towards Them
            var s6 = new Scenario(tgt:Pos(-10, -10), us:Pos(10, 10),  tgtVel:Zero, usVel:Vectors.TopLeft * 100);
            s6.TestPredictStationary();
        }

        [TestMethod]
        public void UsStoppedTargetMovingToUs()
        {
            //   * Tgt
            //   |
            //   V
            //    
            //   * Us
            var s1 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Down*100, usVel:Zero);
            s1.TestAndSimulate(0, 45, 1000); // fast projectile, hits target before it can really move
            s1.TestAndSimulate(0, 0,  100);  // edge case, vel == speed, must return TargetPos
            s1.TestAndSimulate(0, 495, 1);   // ultra slow projectile
            s1.TestMovePos(expected:s1.Tgt);

            //   * Us +Y
            //   
            //   A
            //   |
            //   * Tgt
            var s2 = new Scenario(tgt:Pos(0, 500), us:Pos(0, 0), tgtVel:Vectors.Up*100, usVel:Zero);
            s2.TestAndSimulate(0, 454, 1000);
            s2.TestAndSimulate(0, 500, 100); // edge case, vel == speed, must return TargetPos
            s2.TestAndSimulate(0, 5, 1);
            s2.TestMovePos(expected:s2.Tgt);

            //  Us    Tgt
            //   *  <--*
            var s3 = new Scenario(tgt:Pos(500, 0), us:Pos(0, 0), tgtVel:Vectors.Left*100, usVel:Zero);
            s3.TestAndSimulate(454, 0, 1000);
            s3.TestAndSimulate(500, 0, 100); // edge case, vel == speed, must return TargetPos
            s3.TestAndSimulate(5, 0, 1);
            s3.TestMovePos(expected:s3.Tgt);

            //  Tgt    Us
            //   *-->  *
            var s4 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 0), tgtVel:Vectors.Right*100, usVel:Zero);
            s4.TestAndSimulate(45, 0, 1000);
            s4.TestAndSimulate(0, 0, 100); // edge case, vel == speed, must return TargetPos
            s4.TestAndSimulate(495, 0, 1);
            s4.TestMovePos(expected:s4.Tgt);

            //   * Us
            //    
            //     A
            //      \
            //       * Tgt
            var s5 = new Scenario(tgt:Pos(500, 500), us:Pos(0, 0), tgtVel:Vectors.TopLeft*100, usVel:Zero);
            s5.TestAndSimulate(438, 438, 1000);
            s5.TestAndSimulate(207, 207, 100);
            s5.TestAndSimulate(3, 3, 1);
            s5.TestMovePos(expected:s5.Tgt);

            //   * Tgt
            //    \
            //     V
            //      
            //       * Us diagonally flying towards Them
            var s6 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 500), tgtVel:Vectors.BotRight*100, usVel:Zero);
            s6.TestAndSimulate(62, 62, 1000);
            s6.TestAndSimulate(293, 293, 100);
            s6.TestAndSimulate(497, 497, 1);
            s6.TestMovePos(expected:s6.Tgt);
        }


        [TestMethod]
        public void UsStoppedTargetMovingAway()
        {
            // Tgt *---> 100m/s
            //     
            //     * Us
            var s1 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Right*100, usVel:Zero);
            s1.TestAndSimulate(  50.2f, 0, 1000);
            s1.TestAndSimulate( 288.7f, 0, 200); // 2x tgt speed, will catch up
            s1.TestAndSimulate(1091.0f, 0, 110); // will catch up in far future
            s1.TestAndSimulate(3526.7f, 0, 101); // will catch up in far future
            s1.TestAndSimulate( 500.0f, 0, s1.TgtVel.Length()); // no solution

            //  Us *
            //
            // Tgt *
            //     |
            //     V 100m/s
            var s2 = new Scenario(tgt:Pos(0, 500), us:Pos(0, 0), tgtVel:Vectors.Down*100, usVel:Zero);
            s2.TestAndSimulate(0, 555.5f, 1000); // SimResult at 0.555s
            s2.TestAndSimulate(0, 1000, 200);    // 2x tgt speed, will catch up
            s2.TestAndSimulate(0, 3000, 120);    // SimResult at 23s
            s2.TestAndSimulate(0, 1000, s2.TgtVel.Length()); // no solution
        }

        [TestMethod]
        public void UsStoppedTargetMovingDiagonally()
        {
            //       A
            //      /
            // Tgt *
            //     
            //     * Us
            var s1 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.TopRight*100, usVel:Zero);
            s1.TestAndSimulate(273, -273, 300);
            s1.TestAndSimulate(683, -683, 200);
            s1.TestAndSimulate(300, -300, 120); // no solution
            s1.TestAndSimulate(353.55f, -353.55f, s1.TgtVel.Length()); // no solution

            // Tgt *
            //      \
            //       V
            //
            //     * Us
            var s2 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.BotRight*100, usVel:Zero);
            //SimulateAndFire(s2, 120);
            s2.TestAndSimulate(130, 130, 300);
            s2.TestAndSimulate(183, 183, 200);
            s2.TestAndSimulate(300, 300, 120); // no solution

            s2.TestAndSimulate(353.55f, +353.55f, s2.TgtVel.Length(), false); // no solution

            // Tgt *
            //      \
            //       \
            //        \
            //  Us *   V super high speed, it will go past Us in < 1s
            //           so this should do full prediction because it's hopeless.
            var s3 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.BotRight*1000, usVel:Zero);
            s2.TestAndSimulate(137, 137, s3.TgtVel.Length()*0.2f, false); // no solution
        }

        [TestMethod]
        public void MovingInParallel1()
        {
            // Tgt *---->
            //  Us *--->
            var s1 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Right*100, usVel:Vectors.Right*80);
            s1.TestAndSimulate(176, 0, 300);
            s1.TestAndSimulate(288, 0, 200);
            s1.TestAndSimulate(753, 0, 120);

            // Tgt *----->
            //  Us *-->
            var s2 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Right*150, usVel:Vectors.Right*60);
            s2.TestAndSimulate(288, 0, 300);
            s2.TestAndSimulate(566, 0, 200);
            s2.TestAndSimulate(625, 0, 120); // no solution

            // Tgt *--->
            // <---* Us
            var s3 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Right*60, usVel:Vectors.Left*60);
            s3.TestAndSimulate(102, 0, 300);
            s3.TestAndSimulate(157, 0, 200);
            s3.TestAndSimulate(288, 0, 120); // no solution
        }

        [TestMethod]
        public void MovingInParallel2()
        {
            // Tgt *   * Us
            //     |   |
            //     V   V
            var s4 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 0), tgtVel:Vectors.Down*60, usVel:Vectors.Down*60);
            s4.TestAndSimulate(0, 102, 300);
            s4.TestAndSimulate(0, 157, 200);
            s4.TestAndSimulate(0, 288, 120); // no solution

            // Tgt *   * Us
            //     |   V
            //     V   
            var s5 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 0), tgtVel:Vectors.Down*100, usVel:Vectors.Down*50);
            s5.TestAndSimulate(0, 176, 300);
            s5.TestAndSimulate(0, 288, 200);
            s5.TestAndSimulate(0, 753, 120);

            // Tgt *   A
            //     |   |
            //     V   * Us
            var s6 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 500), tgtVel:Vectors.Down*100, usVel:Vectors.Up*100);
            s6.TestAndSimulate(0, 195, 300);
            s6.TestAndSimulate(0, 274, 200);
            s6.TestAndSimulate(0, 421, 120);

            // Tgt *    A
            //    /    /
            //   V    * Us
            var s7 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 500), tgtVel:Vectors.BotLeft*100, usVel:Vectors.TopRight*100);
            s7.TestAndSimulate(-267, 267, 300);
            s7.TestAndSimulate(-500, 500, 200);
            s7.TestAndSimulate(-589, 589, 120); // no solution
        }

        // This is the final boss. Most messed up situations happen here.
        [TestMethod]
        public void MovingChaotic1()
        {
            // Tgt    *    This is a typical intercept case, we are flanking them
            //         \
            //          V
            //  Us *--->
            var s1 = new Scenario(tgt:Pos(500, 0), us:Pos(0, 500), tgtVel:Vectors.BotRight*100, usVel:Vectors.Right*100);
            s1.TestAndSimulate(767, 267, 300);
            s1.TestAndSimulate(1000, 500, 200);
            s1.TestAndSimulate(1089, 589, 120);

            // Tgt    *    They are flanking us
            //       / 
            //      V    
            //  <----* Us
            var s2 = new Scenario(tgt:Pos(250, 0), us:Pos(0, 500), tgtVel:Vectors.BotLeft*200, usVel:Vectors.Left*100);
            s2.TestAndSimulate(48, 201, 300);
            s2.TestAndSimulate(0, 250, 200);
            s2.TestAndSimulate(-70, 321, 120);
            s2.TestAndSimulate(-1613, 1863, 60);
        }
    }
}
