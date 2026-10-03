using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class OpenBordersOfferTests : StarDriveTest
    {
        Relationship AiToPlayer;

        // An AI that has been trading with the player long enough to consider open borders
        void SetupMaturedTradePartners(float trust)
        {
            CreateUniverseAndPlayerEmpire();
            AiToPlayer = Enemy.GetRelations(Player);
            Relationship playerToAi = Player.GetRelations(Enemy);
            AiToPlayer.AtWar = playerToAi.AtWar = false;
            Enemy.SignTreatyWith(Player, TreatyType.NonAggression);
            Enemy.SignTreatyWith(Player, TreatyType.Trade);
            AiToPlayer.Treaty_Trade_TurnsExisted = playerToAi.Treaty_Trade_TurnsExisted = 500;
            AiToPlayer.Trust = trust;
            AiToPlayer.TrustUsed = 0;
            Player.data.Traits.DiplomacyMod = 0; // pin, so the racial trait cannot tip the quality
        }

        static Offer OpenBorders() => new Offer { OpenBorders = true };

        string AnalyzePlayerOffer(Offer fromPlayer, Offer fromAi)
        {
            return Enemy.AI.AnalyzeOffer(fromPlayer, fromAi, Player, Offer.Attitude.Respectful);
        }

        [TestMethod]
        public void OpenBordersIsNotAFreeGift()
        {
            // the treaty is signed bilaterally, so offering it one-sided is not a gift and
            // must not buy its way past the trust the AI does not have
            SetupMaturedTradePartners(trust: 0);
            string response = AnalyzePlayerOffer(OpenBorders(), new Offer());

            Assert.AreNotEqual("OfferResponse_Accept_Gift", response,
                "A one-sided open borders offer was accepted as a free gift");
            Assert.IsFalse(Enemy.IsOpenBordersTreaty(Player),
                "An AI with no trust in the player opened its borders anyway");
        }

        [TestMethod]
        public void OpenBordersCostsTheSameWhicheverSideOffersIt()
        {
            SetupMaturedTradePartners(trust: 100);
            string oneSided = AnalyzePlayerOffer(OpenBorders(), new Offer());

            SetupMaturedTradePartners(trust: 100);
            string mutual = AnalyzePlayerOffer(OpenBorders(), OpenBorders());

            AssertEqual(mutual, oneSided,
                "Which side ticked the open borders box changed how the AI valued the same treaty");
        }

        [TestMethod]
        public void OpenBordersIsStillObtainableWithTrust()
        {
            SetupMaturedTradePartners(trust: 100);
            string response = AnalyzePlayerOffer(OpenBorders(), OpenBorders());

            Assert.AreNotEqual("OfferResponse_Reject_Insulting", response,
                "A mutual open borders treaty was treated as an insult");
            Assert.IsTrue(Enemy.IsOpenBordersTreaty(Player),
                $"A trusted AI refused a mutual open borders treaty, answered '{response}'");
        }
    }
}
