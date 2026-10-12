using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using UnitTests.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.StoryAndEvents
{
    // an artifact can be found once per game, and one found by an event works like one received in a trade
    [TestClass]
    public class ArtifactDiscoveryTests : StarDriveTest
    {
        readonly Artifact[] All;
        readonly bool[] Discovered; // global, other tests' games may have found some

        public ArtifactDiscoveryTests()
        {
            CreateUniverseAndPlayerEmpire();
            All = ResourceManager.ArtifactsDict.Values.ToArray();
            Discovered = All.Select(a => a.Discovered).ToArray();
        }

        void RestoreDiscovered()
        {
            for (int i = 0; i < All.Length; ++i)
                All[i].Discovered = Discovered[i];
        }

        [TestMethod]
        public void AnArtifactFoundByAnEventRefreshesSensorRanges()
        {
            AddHomeWorldToEmpire(new Vector2(200_000), Player); // the event grants its ships at the capital
            Artifact sensor = All.FirstOrDefault(a => a.SensorMod > 0f);
            Assert.IsNotNull(sensor, "setup: an artifact with a sensor range bonus");
            var outcome = new Outcome
            {
                GrantArtifact = true,
                TroopsToSpawn = new(), FriendlyShipsToSpawn = new(), PirateShipsToSpawn = new(), RemnantShipsToSpawn = new()
            };
            try
            {
                foreach (Artifact a in All)
                    a.Discovered = a != sensor;
                Player.ForceUpdateSensorRadiuses = false;

                outcome.CheckOutComes(null, null, Player, popup: null);

                Assert.IsTrue(Player.data.OwnedArtifacts.Contains(sensor), "setup: the event did not grant the sensor artifact");
                Assert.IsTrue(Player.ForceUpdateSensorRadiuses, "the sensor ranges were not refreshed for the new sensor bonus");
            }
            finally
            {
                RestoreDiscovered();
            }
        }

        [TestMethod]
        public void AfterALoadOnlyTheArtifactsSomeoneHoldsAreFound()
        {
            Artifact held = All[0];
            Artifact notHeld = All[1];
            try
            {
                Player.AddArtifact(held);
                held.Discovered = false;   // as in a session that has not seen this game yet
                notHeld.Discovered = true; // found in another game of the same session

                BinarySerializerTests.SerDes(UState);

                Assert.IsTrue(held.Discovered, "an artifact the player holds could be found again after loading the save");
                Assert.IsFalse(notHeld.Discovered, "an artifact found in another game cannot be found in this one");
            }
            finally
            {
                RestoreDiscovered();
            }
        }
    }
}
