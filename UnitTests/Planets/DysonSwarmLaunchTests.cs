using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets
{
    // a colony in a Dyson swarm system launches a swarm sat every launcher fire delay / (level * 2)
    [TestClass]
    public class DysonSwarmLaunchTests : StarDriveTest
    {
        readonly Planet P;
        readonly DysonSwarm Swarm;

        public DysonSwarmLaunchTests()
        {
            CreateUniverseAndPlayerEmpire();
            P = AddHomeWorldToEmpire(new Vector2(1000), Player);
            P.System.SetDysonSwarmType(1);
            P.System.ActivateDysonSwarm(Player);
            Swarm = P.System.DysonSwarm;

            // sats launch only while the swarm is behind its controllers; Swarm.Update would also load the ring textures
            typeof(DysonSwarm).GetProperty(nameof(DysonSwarm.ControllerCompletion))!.SetValue(Swarm, 1f);
        }

        void SetLevel(int level)
        {
            float[] billions = { 0, 0.3f, 1f, 3f, 7f, 12f };
            P.Population = billions[level] * 1000f;
            P.UpdateDevelopmentLevel();
            Assert.AreEqual(level, P.Level, "The test population did not give the planet the level it needs");
        }

        [TestMethod]
        public void AtASlowSimTheSwarmKeepsItsLaunchRate()
        {
            const int stepsPerSecond = 10;
            ResourceManager.GetWeaponTemplate(DysonSwarm.DysonSwarmLauncherTemplate, out IWeaponTemplate launcher);
            int level = Enumerable.Range(2, 4).First(l =>
            {
                float stepsPerLaunch = launcher.FireDelay / (l * 2f) * stepsPerSecond;
                float part = stepsPerLaunch - (float)Math.Floor(stepsPerLaunch);
                return part > 0.25f && part < 0.75f;
            });
            SetLevel(level);
            float delay = launcher.FireDelay / (level * 2);

            var step = new FixedSimTime(1f / stepsPerSecond);
            float seconds = 200 * delay; // rounding each delay up to whole steps would lose dozens of launches
            int sats = Swarm.NumSwarmSats;
            for (int i = 0; i < (int)Math.Round(seconds * stepsPerSecond); ++i)
            {
                P.ProdHere = P.Storage.Max;
                P.Update(step);
            }

            int launched = Swarm.NumSwarmSats - sats;
            Assert.IsTrue(launched > 0, "setup: the colony launched no swarm sats");
            int expected = (int)(seconds / delay);
            Assert.IsTrue(Math.Abs(launched - expected) <= 1,
                $"At {stepsPerSecond} sim steps per second a level {level} colony launched {launched} swarm sats in {seconds:0} s, every {delay:0.###} s would be {expected}");
        }
    }
}
