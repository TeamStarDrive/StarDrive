using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.EspionageTests
{
    [TestClass]
    public class UpriseTests : StarDriveTest
    {
        static readonly float[] ShareKeptByTheDamageTable = { 1f, 0.75f, 0.5f };

        [TestMethod]
        public void AnUpriseLeavesTheShareOfFertilityItsRollNames()
        {
            CreateUniverseAndPlayerEmpire();
            Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
            Planet colony = AddHomeWorldToEmpire(new Vector2(2000), Enemy);
            RacialTrait traits = Enemy.data.Traits;
            traits.EnvTerran = traits.EnvOceanic = traits.EnvSteppe = traits.EnvTundra = traits.EnvSwamp
                = traits.EnvDesert = traits.EnvIce = traits.EnvBarren = traits.EnvVolcanic = 0.5f;
            Player.data.OffensiveSpyBonus = 1000; // every roll but a disaster succeeds
            var uprise = new InfiltrationOpsUprise(Player, Enemy, levelCost: 100);

            int threeQuartersKept = 0;
            for (int i = 0; i < 60; ++i)
            {
                colony.SetBaseFertility(1f, 1f);
                AssertEqual(0.0001f, 0.5f, colony.Fertility, "setup: the colony's fertility must carry the race's environment modifier");

                uprise.CompleteOperation();

                float kept = colony.BaseFertility;
                bool inTable = false;
                foreach (float share in ShareKeptByTheDamageTable)
                    inTable |= kept.AlmostEqual(share, 0.0001f);
                Assert.IsTrue(inTable, $"an uprise left {kept} of the colony's fertility, which no roll in the damage table names");
                if (kept.AlmostEqual(0.75f, 0.0001f))
                    ++threeQuartersKept;
            }

            AssertGreaterThan(threeQuartersKept, 0, "setup: sixty successful uprises must include a roll of 4 to 7");
        }
    }
}
