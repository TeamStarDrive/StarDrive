using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;

namespace UnitTests
{
    public partial class TestImpactPredictor
    {
        public class Scenario
        {
            public static bool RunVisualSimulations = true;
            public static float SimSpeed = 1f;

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

            public SimResult SimulateImpact(Vector2 projectileVel)
            {
                var parameters = new SimParameters
                {
                    Step = (1f / 60f),
                    DelayBetweenSteps = 0.001f / SimSpeed,
                    ProjectileVelocity = projectileVel,
                    Duration = 10,
                    EnablePauses = true,
                };

                var impactSim = new ImpactSimulation(this, parameters);
                SimResult result = impactSim.RunAndWaitForResult();
                Console.WriteLine(result);
                return result;
            }

            public void TestAndSimulate(float expectedX, float expectedY, float projectileSpeed, bool forceSim = false)
            {
                if (forceSim || RunVisualSimulations)
                {
                    Vector2 p = Predict(projectileSpeed);
                    Console.WriteLine($"SimulateImpact: {p}");
                    SimulateImpact(Us.DirectionToTarget(p) * projectileSpeed);
                }

                var expected = new Vector2(expectedX, expectedY);
                TestPredict(expected, projectileSpeed);
            }
        }
    }
}