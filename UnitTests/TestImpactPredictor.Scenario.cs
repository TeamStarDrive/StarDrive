using System;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests
{
    public partial class TestImpactPredictor
    {
        public class Scenario
        {
            public static bool RunVisualSimulations = false;
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

            public Vector2 PredictMovePos()
            {
                var p = new ImpactPredictor(Us, UsVel, 0f, Tgt, TgtVel, ZeroAcc);
                return p.PredictMovePos();
            }

            public void TestPredictStationary()
            {
                // regardless of speed, the position must always be TARGET
                AssertEqual(0.1f, Tgt, Predict(interceptSpeed:1000));
                AssertEqual(0.1f, Tgt, Predict(interceptSpeed:100));
                AssertEqual(0.1f, Tgt, Predict(interceptSpeed:10));
                AssertEqual(0.1f, Tgt, Predict(interceptSpeed:5));
                AssertEqual(0.1f, Tgt, Predict(interceptSpeed:UsVel.Length()));
                AssertEqual(0.1f, Tgt, Predict(interceptSpeed:0));
                AssertEqual(1f, Tgt, PredictMovePos());
            }

            public Vector2 TestPredict(in Vector2 expected, float interceptSpeed)
            {
                Vector2 p = Predict(interceptSpeed);
                AssertEqual(1f, expected, p);
                return p;
            }

            public void TestMovePos(in Vector2 expected)
            {
                AssertEqual(0.1f, expected, PredictMovePos());
            }

            public SimResult SimulateImpact(SimParameters sim)
            {
                ImpactSimulation impactSim = new(Game, this, sim);

                EnableMockInput(false); // switch from mocked input to real input
                Game.ShowAndRun(screen: impactSim); // run the sim
                EnableMockInput(true); // restore the mock input

                Console.WriteLine(impactSim.Result);
                return impactSim.Result;
            }

            public void TestAndSimulate(float expectedX, float expectedY, float projectileSpeed,
                                        bool forceSim = false, SimParameters sim = null)
            {
                if (forceSim || RunVisualSimulations)
                {
                    if (sim == null)
                    {
                        Vector2 p = Predict(projectileSpeed);
                        Console.WriteLine($"SimulateImpact: {p}");
                        sim = new()
                        {
                            SimSpeed = SimSpeed,
                            Prediction = p,
                            ProjVelStart = Us.DirectionToTarget(p) * projectileSpeed,
                        };
                    }
                    SimulateImpact(sim);
                }

                var expected = new Vector2(expectedX, expectedY);
                TestPredict(expected, projectileSpeed);
            }
        }
    }
}