using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    // an artifact's diplomacy bonus belongs to whoever holds it, however it was found or traded
    [TestClass]
    public class ArtifactTradeTests : StarDriveTest
    {
        public ArtifactTradeTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        static void Give(Ship_Game.Empire from, Ship_Game.Empire to, Artifact art)
        {
            var gift = new Offer();
            gift.ArtifactsOffered.Add(art.Name);
            from.AI.AcceptOffer(gift, new Offer(), from, to, Offer.Attitude.Respectful, 1f);
        }

        [TestMethod]
        public void ATradedArtifactTakesItsDiplomacyBonusAlong()
        {
            Artifact art = ResourceManager.ArtifactsDict.Values.First(a => a.DiplomacyMod > 0f);
            float enemyTrait = Enemy.data.Traits.DiplomacyMod;
            float playerTrait = Player.data.Traits.DiplomacyMod;
            float playerOngoing = Player.data.OngoingDiplomaticModifier;

            Enemy.data.OwnedArtifacts.Add(art); // found by an event, as Outcome does
            art.GrantBonuses(Enemy, popup: null);
            float enemyBonus = art.GetDiplomacyBonus(Enemy.data);
            AssertEqual(0.0001f, enemyTrait + enemyBonus, Enemy.data.Traits.DiplomacyMod, "setup: the event did not give the AI the artifact's diplomacy bonus");

            Give(Enemy, Player, art);
            AssertEqual(0.0001f, enemyTrait, Enemy.data.Traits.DiplomacyMod, "The AI kept the diplomacy bonus of the artifact it gave away");
            AssertEqual(0.0001f, playerTrait + art.GetDiplomacyBonus(Player.data), Player.data.Traits.DiplomacyMod,
                "The player did not get the diplomacy bonus of the artifact it received");

            Give(Player, Enemy, art);
            AssertEqual(0.0001f, playerTrait, Player.data.Traits.DiplomacyMod, "Passing on a traded artifact cost the player diplomacy");
            AssertEqual(0.0001f, playerOngoing, Player.data.OngoingDiplomaticModifier, "The player kept a bonus from an artifact it no longer holds");
            AssertEqual(0.0001f, enemyTrait + enemyBonus, Enemy.data.Traits.DiplomacyMod, "The AI did not get the bonus back with the artifact");
        }

        [TestMethod]
        public void TheLastArtifactLeftToFindGivesItsBonus()
        {
            AddHomeWorldToEmpire(new Vector2(200_000), Player); // the event grants its ships at the capital
            Artifact[] all = ResourceManager.ArtifactsDict.Values.ToArray();
            bool[] discovered = all.Select(a => a.Discovered).ToArray(); // global, other tests' games may have found some
            Artifact last = all.First(a => a.DiplomacyMod > 0f);
            float trait = Player.data.Traits.DiplomacyMod;
            var outcome = new Outcome
            {
                GrantArtifact = true,
                TroopsToSpawn = new(), FriendlyShipsToSpawn = new(), PirateShipsToSpawn = new(), RemnantShipsToSpawn = new()
            };
            try
            {
                foreach (Artifact a in all)
                    a.Discovered = a != last;

                outcome.CheckOutComes(null, null, Player, popup: null);
                Assert.IsTrue(Player.data.OwnedArtifacts.Contains(last), "setup: the event did not grant the last artifact");
                AssertEqual(0.0001f, trait + last.GetDiplomacyBonus(Player.data), Player.data.Traits.DiplomacyMod,
                    "The last artifact left to find did not give its diplomacy bonus");
                AssertEqual(0, outcome.MoneyGranted, "The last artifact left to find was paid out as money as well");
            }
            finally
            {
                for (int i = 0; i < all.Length; ++i)
                    all[i].Discovered = discovered[i];
            }
        }
    }
}
