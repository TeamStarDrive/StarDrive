using System;
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
    public class TestImpactPredictor
    {
        static ImpactSimWindow Window;
        
        Vector2 Pos(float x, float y) => new Vector2(x, y);
        Vector2 Vel(float x, float y) => new Vector2(x, y);
        Vector2 Acc(float x, float y) => new Vector2(x, y);
        Vector2 Vec(float x, float y) => new Vector2(x, y);
        
        static readonly Vector2 Zero    = new Vector2(0,0);
        static readonly Vector2 ZeroPos = new Vector2(0,0);
        static readonly Vector2 ZeroVel = new Vector2(0,0);
        static readonly Vector2 ZeroAcc = new Vector2(0,0);

        static bool RunVisualSimulations = true;

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
            
            public Vector2 Predict(float interceptSpeed)
            {
                var p = new ImpactPredictor(Us, UsVel, interceptSpeed, Tgt, TgtVel, ZeroAcc);
                return p.Predict(true);
            }

            public Vector2 PredictIterative(float interceptSpeed)
            {
                var p = new ImpactPredictor(Us, UsVel, interceptSpeed, Tgt, TgtVel, ZeroAcc);
                return p.PredictIterative();
            }

            public Vector2 PredictMovePos()
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
                Vector2 i = PredictIterative(interceptSpeed);
                if (!p.AlmostEqual(i))
                    Console.WriteLine($"DIFFERENCE QUAD-ITER: {p-i}");
                Assert.That.Equal(1f, expected, p);
                Assert.That.Equal(1f, expected, i);
                Assert.That.Equal(0.5f, p, i);
                return p;
            }

            public void TestMovePos(in Vector2 expected)
            {
                Assert.That.Equal(0.1f, expected, PredictMovePos());
            }

            public SimResult SimulateImpact(Vector2 projectileVel, string name)
            {
                var parameters = new SimParameters
                {
                    Step = 1f / 60f,
                    DelayBetweenSteps = 0.001f,
                    ProjectileVelocity = projectileVel,
                    Duration = 10,
                    EnablePauses = true,
                    Name = name,
                };

                if (Window == null)
                    Window = new ImpactSimWindow();

                var impactSim = new ImpactSimulation(Window, this, parameters);
                SimResult result = impactSim.WaitForResult();
                Console.WriteLine(result);
                return result;
            }

            public void TestAndSimulate(float expectedX, float expectedY, float projectileSpeed, bool forceSim = false)
            {
                if (forceSim || RunVisualSimulations)
                {
                    Vector2 p = Predict(projectileSpeed);
                    Console.WriteLine($"SimulateImpact QUAD: {p}");
                    SimulateImpact(Us.DirectionToTarget(p)*projectileSpeed, "QUAD");
                }

                var expected = new Vector2(expectedX, expectedY);
                TestPredict(expected, projectileSpeed);
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
            RunVisualSimulations = false;
            //   * Tgt
            //   |
            //   V
            //    
            //   * Us
            var s1 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Down*100, usVel:Zero);
            s1.TestAndSimulate(0, 45, 1000); // fast projectile, hits target before it can really move
            s1.TestAndSimulate(0, 500, 100); // slow projectile, tgt will collide with us
            s1.TestAndSimulate(0, 495, 1);   // ultra slow projectile
            s1.TestMovePos(expected:s1.Tgt);

            //   * Us +Y
            //   
            //   A
            //   |
            //   * Tgt
            var s2 = new Scenario(tgt:Pos(0, 500), us:Pos(0, 0), tgtVel:Vectors.Up*100, usVel:Zero);
            s2.TestAndSimulate(0, 454, 1000);
            s2.TestAndSimulate(0, 0, 100);
            s2.TestAndSimulate(0, 5, 1);
            s2.TestMovePos(expected:s2.Tgt);

            //  Us    Tgt
            //   *  <--*
            var s3 = new Scenario(tgt:Pos(500, 0), us:Pos(0, 0), tgtVel:Vectors.Left*100, usVel:Zero);
            s3.TestAndSimulate(454, 0, 1000, true);
            s3.TestAndSimulate(0, 0, 100, true);
            s3.TestAndSimulate(5, 0, 1, true);
            s3.TestMovePos(expected:s3.Tgt);

            //  Tgt    Us
            //   *-->  *
            var s4 = new Scenario(tgt:Pos(0, 0), us:Pos(500, 0), tgtVel:Vectors.Right*100, usVel:Zero);
            s4.TestAndSimulate(45, 0, 1000);
            s4.TestAndSimulate(500, 0, 100);
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
            //Simulate(s2, 120);
            s2.TestAndSimulate(0, 555.5f, 1000); // SimResult at 0.555s
            s2.TestAndSimulate(0, 1000, 200);    // 2x tgt speed, will catch up
            s2.TestAndSimulate(0, 3000, 120);    // SimResult at 23s
            s2.TestAndSimulate(0, 1000, s2.TgtVel.Length()); // no solution
        }

        [TestMethod]
        public void UsStoppedTargetMovingDiagonally()
        {
            RunVisualSimulations = false;
            //       A
            //      /
            // Tgt *
            //     
            //     * Us
            var s1 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.TopRight*100, usVel:Zero);
            //s1.PredictAndSimulateFire(200);
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
        public void MovingInParallel()
        {
            RunVisualSimulations = false;
            // Tgt *---->
            //
            //  Us *--->
            var s1 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Right*100, usVel:Vectors.Right*80);
            //SimulateAndFire(s1, 120);
            s1.TestAndSimulate(176, 0, 300);
            s1.TestAndSimulate(288, 0, 200);
            s1.TestAndSimulate(753, 0, 120);

            // Tgt *----->
            //  Us *-->
            var s2 = new Scenario(tgt:Pos(0, 0), us:Pos(0, 500), tgtVel:Vectors.Right*150, usVel:Vectors.Right*60);
            //SimulateAndFire(s2, 120);
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
    }
}
