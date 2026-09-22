using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Ship_Game;

namespace UnitTests.EspionageTests
{
    [TestClass]
    public class InfiltrationOperationsTests : StarDriveTest
    {
        [TestMethod]
        public void EveryOperationAnswersToItsOwnType()
        {
            CreateUniverseAndPlayerEmpire();
            Ship_Game.Espionage espionage = Player.GetEspionage(Enemy);
            espionage.SetInfiltrationLevelTo(Ship_Game.Espionage.MaxLevel);

            foreach (InfiltrationOpsType type in Enum.GetValues(typeof(InfiltrationOpsType)))
            {
                espionage.ActivateOpsIfAble(type);
                Assert.IsTrue(espionage.IsOperationActive(type), $"{type} did not register under its own type");
            }

            espionage.RemoveOperation(InfiltrationOpsType.DisruptProjection);
            Assert.IsFalse(espionage.IsOperationActive(InfiltrationOpsType.DisruptProjection), "removing by type must find it");
            Assert.IsTrue(espionage.IsOperationActive(InfiltrationOpsType.SlowResearch), "and must not take a different operation with it");
        }
    }
}
