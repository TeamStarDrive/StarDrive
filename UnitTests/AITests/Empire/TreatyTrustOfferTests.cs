using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class TreatyTrustOfferTests : StarDriveTest
    {
        Relationship AiToPlayer;
        Artifact[] Artifacts;
        float ArtifactValue;
        float TrustNeededForPact;

        void Setup(GameDifficulty difficulty = GameDifficulty.Normal)
        {
            CreateUniverseAndPlayerEmpire();
            UState.P.Difficulty = difficulty;
            Enemy.TestSetPersonality("Pacifist");
            AiToPlayer = Enemy.GetRelations(Player);
            AiToPlayer.AtWar = Player.GetRelations(Enemy).AtWar = false;
            AiToPlayer.TurnsKnown = 1000;
            AiToPlayer.TrustUsed = 0;
            Player.data.Traits.DiplomacyMod = 0;
            Player.GetRelations(Enemy).turnsSinceLastContact = 0; // no goodwill bonus on top of the gift's
            TrustNeededForPact = Enemy.data.DiplomaticPersonality.NAPact;
            Artifacts = ResourceManager.ArtifactsDict.Values.Where(a => a.DiplomacyMod == 0).Take(7).ToArray();

            ArtifactValue = (float)typeof(EmpireAI).GetProperty("ArtifactValue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Enemy.AI);
            AssertGreaterThan(ArtifactValue, 25f, "setup: one artifact is worth more than a gift can earn in trust");
        }

        // the player offers a pact and `playerGives` artifacts, and asks for `aiGives` artifacts
        string OfferPact(int playerGives, int aiGives, Offer.Attitude attitude = Offer.Attitude.Respectful)
        {
            var fromPlayer = new Offer { NAPact = true };
            var fromAi = new Offer { NAPact = true };
            for (int i = 0; i < playerGives; ++i)
            {
                Player.AddArtifact(Artifacts[i]);
                fromPlayer.ArtifactsOffered.Add(Artifacts[i].Name);
            }
            for (int i = playerGives; i < playerGives + aiGives; ++i)
            {
                Enemy.AddArtifact(Artifacts[i]);
                fromAi.ArtifactsOffered.Add(Artifacts[i].Name);
            }
            return Enemy.AI.AnalyzeOffer(fromPlayer, fromAi, Player, attitude);
        }

        // two artifacts, worth more than the largest anger relief a gift can bring
        string GiftTwoArtifacts()
        {
            var gift = new Offer();
            for (int i = 0; i < 2; ++i)
            {
                Player.AddArtifact(Artifacts[i]);
                gift.ArtifactsOffered.Add(Artifacts[i].Name);
            }
            return Enemy.AI.AnalyzeOffer(gift, new Offer(), Player, Offer.Attitude.Respectful);
        }

        [TestMethod]
        public void APactRefusedForTrustSaysSo()
        {
            Setup();
            AiToPlayer.Trust = TrustNeededForPact - 10;

            AssertEqual("OfferResponse_InsufficientTrust", OfferPact(playerGives: 0, aiGives: 0), "a pact refused for trust asked for a better offer");
            Assert.IsFalse(Enemy.IsNAPactWith(Player), "the pact was signed without the trust it needs");
        }

        [TestMethod]
        public void GoodsOfferedWithAPactEarnTheirTrustFirst()
        {
            Setup();
            AiToPlayer.Trust = TrustNeededForPact - 10;

            OfferPact(playerGives: 1, aiGives: 0);
            Assert.IsTrue(Enemy.IsNAPactWith(Player), "the gift's trust did not count toward the pact");
            AssertEqual(0.001f, TrustNeededForPact - 10 + 25, AiToPlayer.Trust, "the gift's trust was not kept once the pact was signed");
        }

        [TestMethod]
        public void EachGoodCountsToDecideButAcceptingEarnsOneGift()
        {
            Setup();
            AiToPlayer.Trust = TrustNeededForPact - 40;

            OfferPact(playerGives: 2, aiGives: 0); // 25 trust for each artifact closes a gap of 40
            Assert.IsTrue(Enemy.IsNAPactWith(Player), "two goods counted as one while deciding");
            AssertEqual(0.001f, TrustNeededForPact - 40 + 25, AiToPlayer.Trust, "accepting earned more than one gift's trust");
        }

        [TestMethod]
        public void GoodsWithoutATreatyEarnNoTrust()
        {
            Setup();
            AiToPlayer.Trust = 10;
            var fromPlayer = new Offer();
            var fromAi = new Offer();
            for (int i = 0; i < 3; ++i)
            {
                Player.AddArtifact(Artifacts[i]);
                fromPlayer.ArtifactsOffered.Add(Artifacts[i].Name);
            }
            Enemy.AddArtifact(Artifacts[3]);
            fromAi.ArtifactsOffered.Add(Artifacts[3].Name);

            AssertEqual("OfferResponse_Accept_Great", Enemy.AI.AnalyzeOffer(fromPlayer, fromAi, Player, Offer.Attitude.Respectful), "setup: the trade was refused");
            AssertEqual(0.001f, 10f, AiToPlayer.Trust, "a trade with no treaty earned a gift's trust");
        }

        [TestMethod]
        public void WithoutATreatyGoodsDoNotCountTowardTheTrustNeeded()
        {
            Setup();
            SetResearchPotential(Player, 100f);
            Technology tech = ResourceManager.TechsList.Where(t => t.Cost > 0).OrderBy(t => t.Cost).First();
            float techValue = tech.DiplomaticValueTo(Player, Enemy); // what the AI weighs giving it at, and the trust it needs
            AiToPlayer.Trust = techValue - 20;
            int artifacts = (int)Math.Ceiling((techValue + 20) / ArtifactValue) + 1;
            AssertGreaterThan(Artifacts.Length + 1, artifacts, "setup: not enough artifacts to outweigh the tech by 20 trust");

            var fromPlayer = new Offer();
            for (int i = 0; i < artifacts; ++i)
            {
                Player.AddArtifact(Artifacts[i]);
                fromPlayer.ArtifactsOffered.Add(Artifacts[i].Name);
            }
            var fromAi = new Offer();
            fromAi.TechnologiesOffered.Add(tech.UID);

            AssertEqual("OfferResponse_InsufficientTrust", Enemy.AI.AnalyzeOffer(fromPlayer, fromAi, Player, Offer.Attitude.Respectful),
                        "goods with no treaty counted toward the trust a tech needs");
            AssertEqual(0.001f, techValue - 20, AiToPlayer.Trust, "a refused trade changed trust");
        }

        static void SetResearchPotential(Ship_Game.Empire empire, float potential) // a tech's value is turns of research
        {
            typeof(EmpireResearch).GetProperty(nameof(EmpireResearch.MaxResearchPotential)).SetValue(empire.Research, potential);
        }

        [TestMethod]
        public void AskingForATechNeedsOnlyItsOwnTrust()
        {
            Setup();
            SetResearchPotential(Player, 100f);
            SetResearchPotential(Enemy, 100f);
            Technology[] techs = ResourceManager.TechsList.Where(t => t.Cost > 0).OrderBy(t => t.Cost).ToArray();
            Technology cheap = techs.First(), dear = techs.Last();
            float trustNeeded = cheap.DiplomaticValueTo(Player, Enemy);
            float given = dear.DiplomaticValueTo(Enemy, Player);
            AssertGreaterThan(given, trustNeeded * 2, "setup: the tech given is not worth much more than the one asked for");
            AiToPlayer.Trust = trustNeeded + (given - trustNeeded) / 4; // below the old need of half the difference on top

            var fromPlayer = new Offer();
            fromPlayer.TechnologiesOffered.Add(dear.UID);
            var fromAi = new Offer();
            fromAi.TechnologiesOffered.Add(cheap.UID);
            AssertEqual("OfferResponse_Accept_Great", Enemy.AI.AnalyzeOffer(fromPlayer, fromAi, Player, Offer.Attitude.Respectful),
                        "giving more than the tech asked for raised the trust it needs");
        }

        [TestMethod]
        public void PleadingCountsTheGiftsTrustToo()
        {
            Setup();
            AiToPlayer.Trust = TrustNeededForPact - 10;

            // four artifacts for three: a good offer, not a great one, which pleading takes even without the trust
            AssertEqual("OfferResponse_Accept_Good", OfferPact(playerGives: 4, aiGives: 3, Offer.Attitude.Pleading), "the gift's trust did not count when pleading");
        }

        [TestMethod]
        public void ARefusedOfferChangesNoTrustOrAnger()
        {
            Setup();
            AiToPlayer.Trust = TrustNeededForPact - 40;
            AiToPlayer.AddAngerDiplomaticConflict(30);

            AssertEqual("OfferResponse_InsufficientTrust", OfferPact(playerGives: 1, aiGives: 0), "a gift worth 25 trust closed a gap of 40");
            AssertEqual(0.001f, TrustNeededForPact - 40, AiToPlayer.Trust, "a refused offer still earned trust");
            AssertEqual(0.001f, 30f, AiToPlayer.Anger_DiplomaticConflict, "a refused offer still eased anger");
            Assert.IsTrue(Player.data.OwnedArtifacts.Contains(Artifacts[0]), "the refused gift changed hands");
        }

        [TestMethod]
        public void ARefusedThreatWithAGiftEarnsNoTrust()
        {
            Setup();
            AiToPlayer.Trust = 10;
            AiToPlayer.AddAngerDiplomaticConflict(30);
            AiToPlayer.TurnsSinceLastThreathened = 0; // threatened just now, so this threat is refused

            AssertEqual("OfferResponse_InsufficientFear", OfferPact(playerGives: 2, aiGives: 0, Offer.Attitude.Threaten), "setup: the threat was not refused");
            AssertEqual(0.001f, 10f, AiToPlayer.Trust, "a refused threat that gave more than it asked earned trust");
            AssertEqual(0.001f, 30f, AiToPlayer.Anger_DiplomaticConflict, "a refused threat that gave more than it asked eased anger");
        }

        [TestMethod]
        public void AnEvenTradeIsNoGift()
        {
            Setup();
            AiToPlayer.Trust = TrustNeededForPact - 10;

            AssertEqual("OfferResponse_InsufficientTrust", OfferPact(playerGives: 1, aiGives: 1), "goods paid for in kind earned trust");
            AssertEqual(0.001f, TrustNeededForPact - 10, AiToPlayer.Trust, "an even trade changed trust");
        }

        [TestMethod]
        public void TheGiftEasesAngerBeforeTheOfferIsValued()
        {
            Setup();
            AiToPlayer.Trust = 100;
            AiToPlayer.Anger_FromShipsInOurBorders = AiToPlayer.Anger_MilitaryConflict = AiToPlayer.Anger_TerritorialConflict = 0;
            AiToPlayer.AddAngerDiplomaticConflict(100);
            AiToPlayer.TotalAnger = 100;

            // three artifacts for two: at full anger worth half and refused, the eased anger lets it through
            OfferPact(playerGives: 3, aiGives: 2);
            Assert.IsTrue(Enemy.IsNAPactWith(Player), "the gift's anger relief did not count before the offer was valued");
            AssertGreaterThan(100f, AiToPlayer.Anger_DiplomaticConflict, "the gift's anger relief was not kept");
        }

        [TestMethod]
        public void AGiftEarnsLessTheHarderTheGame()
        {
            foreach (GameDifficulty difficulty in new[] { GameDifficulty.Normal, GameDifficulty.Hard, GameDifficulty.Brutal, GameDifficulty.Insane })
            {
                Setup(difficulty);
                AiToPlayer.Trust = 0;
                AiToPlayer.AddAngerDiplomaticConflict(60);

                AssertEqual("OfferResponse_Accept_Gift", GiftTwoArtifacts(), $"{difficulty}: the gift was refused");
                float divisor = (int)difficulty + 1;
                AssertEqual(0.001f, 25 / divisor, AiToPlayer.Trust, $"{difficulty}: trust earned");
                AssertEqual(0.001f, 60 - 50 / divisor, AiToPlayer.Anger_DiplomaticConflict, $"{difficulty}: anger left");
            }
        }
    }
}
