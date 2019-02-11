using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
// ReSharper disable JoinDeclarationAndInitializer

namespace UnitTests
{
    [TestClass]
    public class TestImpactPredictor
    {
        Vector2 Pos(float x, float y) => new Vector2(x, y);
        Vector2 Vel(float x, float y) => new Vector2(x, y);
        Vector2 Acc(float x, float y) => new Vector2(x, y);
        Vector2 Vec(float x, float y) => new Vector2(x, y);
        
        static readonly Vector2 Zero    = new Vector2(0,0);
        static readonly Vector2 ZeroPos = new Vector2(0,0);
        static readonly Vector2 ZeroVel = new Vector2(0,0);
        static readonly Vector2 ZeroAcc = new Vector2(0,0);

        public class Scenario
        {
            public readonly Vector2 Tgt;
            public readonly Vector2 Us;
            public readonly Vector2 TgtVel;
            public readonly Vector2 UsVel;
            public Scenario(Vector2 tgt, Vector2 us, Vector2 tgtVel, Vector2 usVel)
            {
                Tgt    = tgt;
                Us     = us;
                TgtVel = tgtVel;
                UsVel  = usVel;
                Console.WriteLine("Scenario:");
                Console.WriteLine($"  tgt:{Str(Tgt),-16} v:{Str(TgtVel)}");
                Console.WriteLine($"   us:{Str(Us) ,-16} v:{Str(UsVel)}");
            }

            static string Str(in Vector2 pos)
            {
                return $"({pos.X.SignString(2),-4},{pos.Y.SignString(2)})";
            }

            public override string ToString() => $"tgt:{Tgt} tgtVel:{TgtVel}  us:{Us} usVel:{UsVel}";
            
            Vector2 Predict(float interceptSpeed)
            {
                var p = new ImpactPredictor(Us, UsVel, interceptSpeed, Tgt, TgtVel, ZeroAcc);
                return p.Predict(true);
            }

            Vector2 PredictIterative(float interceptSpeed)
            {
                var p = new ImpactPredictor(Us, UsVel, interceptSpeed, Tgt, TgtVel, ZeroAcc);
                return p.PredictIterative();
            }

            Vector2 PredictMovePos()
            {
                var p = new ImpactPredictor(Us, UsVel, 0f, Tgt, TgtVel, ZeroAcc);
                return p.PredictMovePos();
            }

            public void TestPredictAnySpeed(in Vector2 expected)
            {
                // regardless of speed, the position must always be TARGET
                Assert.That.Equal(0.1f, expected, Predict(interceptSpeed:1000));
                Assert.That.Equal(0.1f, expected, Predict(interceptSpeed:100));
                Assert.That.Equal(0.1f, expected, Predict(interceptSpeed:10));
                Assert.That.Equal(0.1f, expected, Predict(interceptSpeed:5));
                Assert.That.Equal(0.1f, expected, Predict(interceptSpeed:UsVel.Length()));
                Assert.That.Equal(0.1f, expected, Predict(interceptSpeed:0));
            }

            public Vector2 TestPredict(in Vector2 expected, float interceptSpeed)
            {
                Vector2 p = Predict(interceptSpeed);
                Assert.That.Equal(0.1f, expected, p);
                Assert.That.Equal(0.66f, expected, PredictIterative(interceptSpeed));
                return p;
            }

            public void TestMovePos(in Vector2 expected)
            {
                Assert.That.Equal(0.1f, expected, PredictMovePos());
            }

            public SimResult SimulateImpact(SimParameters sim)
            {
                using (var s = new ImpactSimulation(this, sim))
                {
                    return s.RunIntersectionSimulation();
                }
            }
        }

        
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
            var s = new Scenario(tgt:Pos(10,-10), us:Pos(10,10), tgtVel:ZeroVel, usVel:ZeroVel);
            s.TestPredict(s.Tgt, 0);
            s.TestPredict(s.Tgt, 100);
            s.TestMovePos(s.Tgt);
        }

        [TestMethod]
        public void TargetStoppedUsMovingToTarget()
        {
            //   * Tgt
            //
            //   A
            //   | 
            //   * Us
            var s1 = new Scenario(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Zero, usVel:Vectors.Up * 2);
            s1.TestPredictAnySpeed(expected:s1.Tgt);
            s1.TestMovePos(expected:s1.Tgt);

            //   * Us +Y
            //   |
            //   V
            //
            //   * Tgt
            var s2 = new Scenario(tgt:Pos(10, 10),  us:Pos(10, -10), tgtVel:Zero, usVel:Vectors.Down * 2);
            s2.TestPredictAnySpeed(expected:s2.Tgt);
            s2.TestMovePos(expected:s2.Tgt);

            //  Us    Tgt
            //   *-->  *
            var s3 = new Scenario(tgt:Pos(10, 5),   us:Pos(-10, 5),  tgtVel:Zero, usVel:Vectors.Right * 2);
            s3.TestPredictAnySpeed(expected:s3.Tgt);
            s3.TestMovePos(expected:s3.Tgt);

            //  Tgt    Us
            //   *  <--*
            var s4 = new Scenario(tgt:Pos(-10, 5),  us:Pos(10, 5),   tgtVel:Zero, usVel:Vectors.Left * 2);
            s4.TestPredictAnySpeed(expected:s4.Tgt);
            s4.TestMovePos(expected:s4.Tgt);

            //   * Us diagonally flying towards Them
            //    \
            //     V
            //
            //       * Them
            var s5 = new Scenario(tgt:Pos(10, 10), us:Pos(-10, -10),  tgtVel:Zero, usVel:Vel(1,1) * 2);
            s5.TestPredictAnySpeed(expected:s5.Tgt);
            s5.TestMovePos(expected:s5.Tgt);

            //   * Them
            //
            //     A
            //      \
            //       * Us diagonally flying towards Them
            var s6 = new Scenario(tgt:Pos(-10, -10), us:Pos(10, 10),  tgtVel:Zero, usVel:Vel(-1,-1) * 2);
            s6.TestPredictAnySpeed(expected:s6.Tgt);
            s6.TestMovePos(expected:s6.Tgt);
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
            s1.TestPredict(Pos(0, 55.5f), 1000); // fast projectile, hits target before it can really move
            s1.TestPredict(s1.Us, 100);          // slow projectile, tgt will collide with us
            s1.TestPredict(s1.Us, 1);            // ultra slow projectile
            s1.TestMovePos(expected:s1.Tgt);

            //   * Us +Y
            //   
            //   A
            //   |
            //   * Tgt
            var s2 = new Scenario(tgt:Pos(0, 500), us:Pos(0, 0), tgtVel:Vectors.Up*100, usVel:Zero);
            s2.TestPredict(Pos(0, 444.4f), 1000);
            s2.TestPredict(s2.Us, 100);
            s2.TestPredict(s2.Us, 1);
            s2.TestMovePos(expected:s2.Tgt);

            //  Us    Tgt
            //   *  <--*
            var s3 = new Scenario(tgt:Pos(500, 0), us:Pos(0, 0), tgtVel:Vectors.Left*100, usVel:Zero);
            s3.TestPredict(Pos(444.4f, 0), 1000);
            s3.TestPredict(s3.Us, 100);
            s3.TestPredict(s3.Us, 1);
            s3.TestMovePos(expected:s3.Tgt);

            //  Tgt    Us
            //   *-->  *
            var s4 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 0), tgtVel:Vectors.Right*100, usVel:Zero);
            s4.TestPredict(Pos(55.5f, 0), 1000);
            s4.TestPredict(s4.Us, 100);
            s4.TestPredict(s4.Us, 1);
            s4.TestMovePos(expected:s4.Tgt);

            //   * Us
            //    
            //     A
            //      \
            //       * Tgt
            var s5 = new Scenario(tgt:Pos(500, 500), us:Pos(0, 0), tgtVel:Vectors.TopLeft*100, usVel:Zero);
            s5.TestPredict(Pos(417.6f, 417.6f), 1000);
            s5.TestPredict(s5.Us, 100);
            s5.TestPredict(s5.Us, 1);
            s5.TestMovePos(expected:s5.Tgt);

            //   * Tgt
            //    \
            //     V
            //      
            //       * Us diagonally flying towards Them
            var s6 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 500), tgtVel:Vectors.BotRight*100, usVel:Zero);
            s6.TestPredict(Pos(82.3f, 82.3f), 1000);
            s6.TestPredict(s6.Us, 100);
            s6.TestPredict(s6.Us, 1);
            s6.TestMovePos(expected:s6.Tgt);
        }

        void Simulate(Scenario s, Vector2 projectileVel)
        {
            SimResult r1 = s.SimulateImpact(new SimParameters
            {
                Step = 1f / 60f,
                DelayBetweenSteps = 0.005f,
                ProjectileVelocity = projectileVel,
                Duration = 720,
            });
            Console.WriteLine($"{r1}");
        }

        [TestMethod]
        public void UsStoppedTargetMovingAway()
        {
            // Tgt *---> 100m/s
            //     
            //     * Us
            var s1 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Right*100, usVel:Zero);
            s1.TestPredict(Pos(  50.2f, 0), 1000);
            s1.TestPredict(Pos( 288.7f, 0), 200); // 2x tgt speed, will catch up
            s1.TestPredict(Pos(1091.0f, 0), 110); // will catch up in far future
            s1.TestPredict(Pos(3526.7f, 0), 101); // will catch up in far future
            s1.TestPredict(Pos( 500.0f, 0), 100); // no solution
            s1.TestPredict(Pos( 500.0f, 0), s1.TgtVel.Length()); // no solution

            //  Us *
            //
            // Tgt *
            //     |
            //     V 100m/s
            var s2 = new Scenario(tgt:Pos(0, 500), us:Pos(0, 0), tgtVel:Vectors.Down*100, usVel:Zero);
            //Simulate(s2, 120);
            s2.TestPredict(Pos(0, 555.5f), 1000); // SimResult at 0.555s
            s2.TestPredict(Pos(0, 1000f), 200);   // 2x tgt speed, will catch up
            s2.TestPredict(Pos(0, 3000f), 120);   // SimResult at 23s
            s2.TestPredict(Pos(0, 1000f), 100);   // no solution
            s2.TestPredict(Pos(0, 1000f), s2.TgtVel.Length()); // no solution
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
            var predicted = s1.TestPredict(Pos(500, -500), 100);

            Simulate(s1, s1.Us.DirectionToTarget(predicted)*100);
            s1.TestPredict(Pos(13.4f, -13.4f), 10);
            s1.TestPredict(Pos(24.1f, -24.1f), s1.TgtVel.Length()); // will never catch it

            // Tgt *
            //      \
            //       V
            //
            //     * Us
            var s2 = new Scenario(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(2,2), usVel:Zero);
            s2.TestPredict(Pos(10.4f, -9.6f), 100);
            s2.TestPredict(Pos(15.1f, -4.8f), 10);
            s2.TestPredict(Pos(24.1f, +4.1f), s2.TgtVel.Length()); // will never catch it

            // Tgt *
            //      \
            //       \
            //        \
            //  Us *   V super high speed, it will go past Us in 1 frame
            //           so this should do full prediction because it's hopeless.
            var s3 = new Scenario(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(20,20), usVel:Zero);
            s3.TestPredict(Pos(80.7f, 60.7f), s3.TgtVel.Length()*0.2f); // will never catch it
        }

        [TestMethod]
        public void UsStoppedTargetMovingDiagonally2()
        {
            // Tgt *
            //      \
            //       V
            //
            //     * Us
            var s1 = new Scenario(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(2,3), usVel:Zero);
            s1.TestPredict(Pos(65.4f, 73.2f), s1.TgtVel.Length()*0.2f); // will never catch it

            var s2 = new Scenario(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(3,3), usVel:Zero);
            s2.TestPredict(Pos(80.7f, 60.71f), s2.TgtVel.Length()*0.2f); // will never catch it
        }
    }
}
