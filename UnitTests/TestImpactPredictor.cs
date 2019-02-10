using System;
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

        static Vector2 Predict(Vector2 pos, Vector2 vel, float speed,
                               Vector2 targetPos, Vector2 targetVel, Vector2 targetAcc)
        {
            var p = new ImpactPredictor(pos, vel, speed, targetPos, targetVel, targetAcc);
            return p.Predict(true);
        }

        static Vector2 PredictMovePos(Vector2 pos, Vector2 vel, float interceptSpeed,
                                      Vector2 targetPos, Vector2 targetVel, Vector2 targetAcc)
        {
            var p = new ImpactPredictor(pos, vel, interceptSpeed, targetPos, targetVel, targetAcc);
            return p.PredictMovePos();
        }

        static Vector2 Predict(in TargetUs t, float interceptSpeed)
        {
            var p = new ImpactPredictor(t.Us, t.UsVel, interceptSpeed, t.Tgt, t.TgtVel, ZeroAcc);
            return p.Predict(true);
        }

        static Vector2 PredictMovePos(in TargetUs t)
        {
            var p = new ImpactPredictor(t.Us, t.UsVel, 0f, t.Tgt, t.TgtVel, ZeroAcc);
            return p.PredictMovePos();
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
            Vector2 tgt = Pos(10, -10);
            Vector2 us  = Pos(10, +10);

            Assert.AreEqual(tgt, Predict(us, ZeroVel,   0, tgt, ZeroVel, ZeroAcc));
            Assert.AreEqual(tgt, Predict(us, ZeroVel, 100, tgt, ZeroVel, ZeroAcc));

            Assert.AreEqual(tgt, PredictMovePos(us, ZeroVel,   0, tgt, ZeroVel, ZeroAcc));
            Assert.AreEqual(tgt, PredictMovePos(us, ZeroVel, 100, tgt, ZeroVel, ZeroAcc));
        }

        struct TargetUs
        {
            public readonly Vector2 Tgt;
            public readonly Vector2 Us;
            public readonly Vector2 TgtVel;
            public readonly Vector2 UsVel;
            public TargetUs(Vector2 tgt, Vector2 us, Vector2 tgtVel, Vector2 usVel)
            {
                Tgt    = tgt;
                Us     = us;
                TgtVel = tgtVel;
                UsVel  = usVel;
            }
            public override string ToString() => $"tgt:{Tgt} us:{Us} tgtVel:{TgtVel} usVel:{UsVel}";
        }

        [TestMethod]
        public void TargetStoppedUsMovingToTarget()
        {
            var scenarios = new Array<TargetUs>
            {
                //   * Tgt
                //
                //   A
                //   | 
                //   * Us
                new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Zero, usVel:Vectors.Up * 2),
                //   * Us +Y
                //   |
                //   V
                //
                //   * Tgt
                new TargetUs(tgt:Pos(10, 10),  us:Pos(10, -10), tgtVel:Zero, usVel:Vectors.Down * 2),
                //  Us    Tgt
                //   *-->  *
                new TargetUs(tgt:Pos(10, 5),   us:Pos(-10, 5),  tgtVel:Zero, usVel:Vectors.Right * 2),
                //  Tgt    Us
                //   *  <--*
                new TargetUs(tgt:Pos(-10, 5),  us:Pos(10, 5),   tgtVel:Zero, usVel:Vectors.Left * 2),
                //   * Us diagonally flying towards Them
                //    \
                //     V
                //
                //       * Them
                new TargetUs(tgt:Pos(10, 10), us:Pos(-10, -10),  tgtVel:Zero, usVel:Vel(1,1) * 2),
                //   * Them
                //
                //     A
                //      \
                //       * Us diagonally flying towards Them
                new TargetUs(tgt:Pos(-10, -10), us:Pos(10, 10),  tgtVel:Zero, usVel:Vel(-1,-1) * 2)
            };

            foreach (TargetUs scenario in scenarios)
            {
                Console.WriteLine($"Scenario {scenario}");
                // regardless of speed, the position must always be TARGET
                Assert.AreEqual(scenario.Tgt, Predict(scenario, 10));
                Assert.AreEqual(scenario.Tgt, Predict(scenario, 5));
                Assert.AreEqual(scenario.Tgt, Predict(scenario, scenario.UsVel.Length()));

                // Make sure MovePos is also TARGET
                Assert.AreEqual(scenario.Tgt, PredictMovePos(scenario));
            }
        }

        [TestMethod]
        public void UsStoppedTargetMovingToUs()
        {
            var scenarios = new Array<TargetUs>
            {
                //   * Tgt
                //   |
                //   V
                //    
                //   * Us
                new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vectors.Down*2, usVel:Zero),
                //   * Us +Y
                //   
                //   A
                //   |
                //   * Tgt
                new TargetUs(tgt:Pos(10, 10),  us:Pos(10, -10), tgtVel:Vectors.Up*2, usVel:Zero),
                //  Us    Tgt
                //   *  <--*
                new TargetUs(tgt:Pos(10, 5),   us:Pos(-10, 5),  tgtVel:Vectors.Left*2, usVel:Zero),
                //  Tgt    Us
                //   *-->  *
                new TargetUs(tgt:Pos(-10, 5),  us:Pos(10, 5),   tgtVel:Vectors.Right*2, usVel:Zero),
                //   * Us
                //    
                //     A
                //      \
                //       * Them
                new TargetUs(tgt:Pos(10, 10), us:Pos(-10, -10),  tgtVel:Vel(-2,-2), usVel:Zero),
                //   * Them
                //    \
                //     V
                //      
                //       * Us diagonally flying towards Them
                new TargetUs(tgt:Pos(-10, -10), us:Pos(10, 10),  tgtVel:Vel(2,2), usVel:Zero)
            };

            foreach (TargetUs scenario in scenarios)
            {
                Console.WriteLine($"Scenario {scenario}");
                // regardless of speed, the position must always be TARGET
                Assert.AreEqual(scenario.Tgt, Predict(scenario, 10));
                Assert.AreEqual(scenario.Tgt, Predict(scenario, 5));
                Assert.AreEqual(scenario.Tgt, Predict(scenario, scenario.UsVel.Length()));

                // Make sure MovePos is also TARGET
                Assert.AreEqual(scenario.Tgt, PredictMovePos(scenario));
            }
        }

        [TestMethod]
        public void UsStoppedTargetMovingAway()
        {
            // Tgt *---> 2m/s
            //     
            //     * Us
            var scenario1 = new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vectors.Right*2f, usVel:Zero);
            
            Console.WriteLine($"Scenario {scenario1}");
            Assert.That.Equal(0.1f, Pos(10.4f, -10), Predict(scenario1, 100));
            Assert.That.Equal(0.1f, Pos(14.0f, -10), Predict(scenario1, 10));
            Assert.That.Equal(0.1f, Pos(30.0f, -10), Predict(scenario1, scenario1.TgtVel.Length())); // will never catch it

            //     A 2m/s
            //     |
            // Tgt *
            //     
            //     * Us
            var scenario2 = new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vectors.Up*2f, usVel:Zero);
            
            Console.WriteLine($"Scenario {scenario2}");
            Assert.That.Equal(0.1f, Pos(10, -10.4f), Predict(scenario2, 100));
            Assert.That.Equal(0.1f, Pos(10, -14.9f), Predict(scenario2, 10));
            Assert.That.Equal(0.1f, Pos(10, -30.0f), Predict(scenario2, scenario2.TgtVel.Length())); // will never catch it
        }

        [TestMethod]
        public void UsStoppedTargetMovingDiagonally()
        {
            //       A
            //      /
            // Tgt *
            //     
            //     * Us
            var scenario1 = new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(2,-2), usVel:Zero);
            
            Console.WriteLine($"Scenario {scenario1}");
            Assert.That.Equal(0.1f, Pos(10.4f, -10.4f), Predict(scenario1, 100));
            Assert.That.Equal(0.1f, Pos(15.1f, -15.1f), Predict(scenario1, 10));
            Assert.That.Equal(0.1f, Pos(24.1f, -24.1f), Predict(scenario1, scenario1.TgtVel.Length())); // will never catch it

            // Tgt *
            //      \
            //       V
            //
            //     * Us
            var scenario2 = new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(2,2), usVel:Zero);
            
            Console.WriteLine($"Scenario {scenario2}");
            Assert.That.Equal(0.1f, Pos(10.4f, -9.6f), Predict(scenario2, 100));
            Assert.That.Equal(0.1f, Pos(13.4f, -6.6f), Predict(scenario2, 10));
            Assert.That.Equal(0.1f, Pos(24.1f, +4.1f), Predict(scenario2, scenario2.TgtVel.Length())); // will never catch it

            // Tgt *
            //      \
            //       \
            //        \
            //  Us *   V super high speed, it will go past Us in 1 frame
            //           so this should do full prediction because it's hopeless.
            var scenario3 = new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(20,20), usVel:Zero);

            Console.WriteLine($"Scenario {scenario3}");
            Assert.That.Equal(0.1f, Pos(80.7f, 60.7f), Predict(scenario3, scenario3.TgtVel.Length()*0.2f)); // will never catch it
        }

        [TestMethod]
        public void UsStoppedTargetMovingDiagonally2()
        {
            // Tgt *
            //      \
            //       V
            //
            //     * Us
            var scenario1 = new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(2,3), usVel:Zero);

            Console.WriteLine($"Scenario {scenario1}");
            Assert.That.Equal(0.1f, Pos(10, -10), Predict(scenario1, scenario1.TgtVel.Length()*0.2f)); // will never catch it

            var scenario2 = new TargetUs(tgt:Pos(10, -10), us:Pos(10, 10), tgtVel:Vel(3,3), usVel:Zero);

            Console.WriteLine($"Scenario {scenario2}");
            Assert.That.Equal(0.1f, Pos(80.7f, 60.71f), Predict(scenario2, scenario2.TgtVel.Length()*0.2f)); // will never catch it

        }
    }
}
