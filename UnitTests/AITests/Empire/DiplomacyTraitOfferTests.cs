using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class DiplomacyTraitOfferTests : StarDriveTest
    {
        Relationship AiToPlayer;

        // an AI that trusts the player and has known it long enough for any treaty
        void Setup(float playerDiplomacyMod)
        {
            CreateUniverseAndPlayerEmpire();
            Enemy.TestSetPersonality("Pacifist");
            AiToPlayer = Enemy.GetRelations(Player);
            AiToPlayer.AtWar = Player.GetRelations(Enemy).AtWar = false;
            AiToPlayer.TurnsKnown = 1000;
            AiToPlayer.Trust = 100;
            AiToPlayer.TrustUsed = 0;
            Player.data.Traits.DiplomacyMod = playerDiplomacyMod;
        }

        string OfferNonAggressionPact()
        {
            return Enemy.AI.AnalyzeOffer(new Offer { NAPact = true }, new Offer { NAPact = true }, Player, Offer.Attitude.Respectful);
        }

        // the player trades one artifact for one of the AI's, two goods of equal value
        string TradeArtifacts()
        {
            Artifact[] artifacts = ResourceManager.ArtifactsDict.Values.Where(a => a.DiplomacyMod == 0).Take(2).ToArray();
            Player.AddArtifact(artifacts[0]);
            Enemy.AddArtifact(artifacts[1]);
            var fromPlayer = new Offer();
            fromPlayer.ArtifactsOffered.Add(artifacts[0].Name);
            var fromAi = new Offer();
            fromAi.ArtifactsOffered.Add(artifacts[1].Name);
            return Enemy.AI.AnalyzeOffer(fromPlayer, fromAi, Player, Offer.Attitude.Respectful);
        }

        [TestMethod]
        public void ARepulsiveRaceCanSignAPlainTreaty()
        {
            Setup(playerDiplomacyMod: -0.2f);
            AssertEqual("OfferResponse_Accept_Fair", OfferNonAggressionPact(), "a trusted repulsive race was refused an even treaty");
            Assert.IsTrue(Enemy.IsNAPactWith(Player), "the pact was not signed");
        }

        [TestMethod]
        public void AnAlluringRaceGetsNoBonusOnAPlainTreaty()
        {
            Setup(playerDiplomacyMod: 0.2f);
            AssertEqual("OfferResponse_Accept_Fair", OfferNonAggressionPact(), "the trait still weighed the treaty itself");
        }

        [TestMethod]
        public void TheTraitStillWeighsTheGoodsOffered()
        {
            Setup(playerDiplomacyMod: -0.2f);
            AssertEqual("OfferResponse_Reject_PoorOffer_EnoughTrust", TradeArtifacts(), "a repulsive race's goods were not worth less");

            Setup(playerDiplomacyMod: 0.2f);
            AssertEqual("OfferResponse_Accept_Good", TradeArtifacts(), "an alluring race's goods were not worth more");
        }
    }
}
