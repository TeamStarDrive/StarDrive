using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class SharedSystemColonyGiftTests : StarDriveTest
    {
        Planet Gift;
        Planet Neighbour; // another planet in the gift's system
        Planet Nearby;    // a planet in a nearby system
        Relationship AiToPlayer;

        void Setup()
        {
            CreateUniverseAndPlayerEmpire();
            CreateThirdMajorEmpire();
            Enemy.TestSetPersonality("Cunning");
            Player.GetRelations(Enemy).AtWar = Enemy.GetRelations(Player).AtWar = false;
            Enemy.GetRelations(ThirdMajor).AtWar = ThirdMajor.GetRelations(Enemy).AtWar = false;
            Player.data.Traits.DiplomacyMod = 0; // pin, so the racial trait cannot zero the gift's value
            AiToPlayer = Enemy.GetRelations(Player);

            SolarSystem giftSystem = AddSystem(new Vector2(100_000));
            Gift = AddPlanet(giftSystem, new Vector2(105_000), "Gift Test Gift");
            Neighbour = AddPlanet(giftSystem, new Vector2(109_000), "Gift Test Neighbour");
            SolarSystem nearbySystem = AddSystem(new Vector2(200_000));
            Nearby = AddPlanet(nearbySystem, new Vector2(205_000), "Gift Test Nearby");
            UState.AddSolarSystem(giftSystem);
            UState.AddSolarSystem(nearbySystem);
            giftSystem.FiveClosestSystems = UState.GetFiveClosestSystems(giftSystem);
            Gift.SetOwner(Player);
        }

        SolarSystem AddSystem(Vector2 pos) => new(UState, pos) { Sun = SunType.RandomHabitableSun(UState.Random) };

        static Planet AddPlanet(SolarSystem system, Vector2 pos, string name)
        {
            var p = new Planet(system.Universe.CreateId(), system, pos, fertility: 1f, minerals: 1f, maxPop: 4f) { Name = name };
            p.OrbitalAngle = p.Position.AngleToTarget(system.Position);
            p.TestSetOrbitalRadius(p.Position.Distance(system.Position) + p.Radius);
            system.RingList.Add(new SolarSystem.Ring { Asteroids = false, OrbitalDistance = p.OrbitalRadius, Planet = p });
            system.PlanetList.Add(p);
            return p;
        }

        string GiftToEnemy()
        {
            var gift = new Offer();
            gift.ColoniesOffered.Add(Gift.Name);
            return Enemy.AI.AnalyzeOffer(gift, new Offer(), Player, Offer.Attitude.Respectful);
        }

        // the response, and the trust and anger it cost the player
        (string Response, float TrustLost, float AngerAdded) GiftToEnemyWithCost()
        {
            float trust = AiToPlayer.Trust;
            float anger = AiToPlayer.Anger_DiplomaticConflict;
            string response = GiftToEnemy();
            return (response, trust - AiToPlayer.Trust, AiToPlayer.Anger_DiplomaticConflict - anger);
        }

        void AssertPenalty((string Response, float TrustLost, float AngerAdded) result, float weight)
        {
            float multiplier = Enemy.DifficultyModifiers.WarBaitPenaltyMultiplier;
            AssertEqual("OfferResponse_Reject_NearRival", result.Response, "the AI did not refuse a colony that would set it against a rival");
            AssertEqual(0.001f, Enemy.PersonalityModifiers.WarBaitTrustLoss * multiplier * weight, result.TrustLost, "trust lost");
            AssertEqual(0.001f, Enemy.PersonalityModifiers.WarBaitAnger * multiplier * weight, result.AngerAdded, "anger added");
            Assert.AreEqual(Player, Gift.Owner, "the refused colony changed hands");
        }

        void AssertAccepted(string response, string why)
        {
            AssertEqual("OfferResponse_Accept_Gift", response, why);
            Assert.AreEqual(Enemy, Gift.Owner, why);
        }

        void DeclareWarOnThirdMajor() => Enemy.GetRelations(ThirdMajor).AtWar = ThirdMajor.GetRelations(Enemy).AtWar = true;

        [TestMethod]
        public void ARivalInTheSameSystemCostsTheFullPenalty()
        {
            Setup();
            Neighbour.SetOwner(ThirdMajor);
            Nearby.SetOwner(ThirdMajor);

            AssertPenalty(GiftToEnemyWithCost(), weight: 1f);
        }

        [TestMethod]
        public void ARivalInANearbySystemCostsAFifth()
        {
            Setup();
            Nearby.SetOwner(ThirdMajor);

            AssertPenalty(GiftToEnemyWithCost(), weight: 0.2f);
        }

        [TestMethod]
        public void AnEnemyInTheSystemIsRefusedWithoutAPenalty()
        {
            Setup();
            DeclareWarOnThirdMajor();
            Neighbour.SetOwner(ThirdMajor);

            var result = GiftToEnemyWithCost();
            AssertEqual("OfferResponse_Reject_NearEnemy", result.Response, "the AI took a colony beside its enemy");
            AssertEqual(0.001f, 0f, result.TrustLost, "refusing a colony beside an enemy cost trust");
            AssertEqual(0.001f, 0f, result.AngerAdded, "refusing a colony beside an enemy added anger");
            Assert.AreEqual(Player, Gift.Owner, "the refused colony changed hands");
        }

        [TestMethod]
        public void AnEnemyInANearbySystemIsRefusedToo()
        {
            Setup();
            DeclareWarOnThirdMajor();
            Nearby.SetOwner(ThirdMajor);

            AssertEqual("OfferResponse_Reject_NearEnemy", GiftToEnemy(), "the AI took a colony near its enemy");
            Assert.AreEqual(Player, Gift.Owner, "the refused colony changed hands");
        }

        [TestMethod]
        public void ARivalNearbyOutweighsAnEnemyInTheSystem()
        {
            Setup();
            DeclareWarOnThirdMajor();
            Neighbour.SetOwner(ThirdMajor);
            CreateThirdMajorEmpire(); // a second race, at peace with the AI
            Nearby.SetOwner(ThirdMajor);

            AssertPenalty(GiftToEnemyWithCost(), weight: 0.2f);
        }

        [TestMethod]
        public void ARivalWithOpenBordersDoesNotCount()
        {
            Setup();
            Enemy.SignTreatyWith(ThirdMajor, TreatyType.OpenBorders);
            Neighbour.SetOwner(ThirdMajor);

            AssertAccepted(GiftToEnemy(), "open borders with the neighbour should allow the gift");
        }

        [TestMethod]
        public void ARaceTheAiHasNotMetDoesNotCount()
        {
            Setup();
            var data = ResourceManager.MajorRaces.First(e => UState.GetEmpireByName(e.Name) == null);
            var stranger = UState.CreateEmpire(data, isPlayer: false);
            Assert.IsFalse(Enemy.IsKnown(stranger), "setup: the AI should not know the new race");
            Neighbour.SetOwner(stranger);

            AssertAccepted(GiftToEnemy(), "a race the AI has never met blocked the gift");
        }

        [TestMethod]
        public void TheGiversOwnColoniesDoNotCount()
        {
            Setup();
            Neighbour.SetOwner(Player);
            Nearby.SetOwner(Player);

            AssertAccepted(GiftToEnemy(), "the giver's own colonies blocked the gift");
        }

        [TestMethod]
        public void TheReceiversOwnColoniesDoNotCount()
        {
            Setup();
            Neighbour.SetOwner(Enemy);
            Nearby.SetOwner(Enemy);

            AssertAccepted(GiftToEnemy(), "the AI's own colonies blocked the gift");
        }

        [TestMethod]
        public void AFactionColonyDoesNotCount()
        {
            Setup();
            CreateAMinorFaction("Corsairs");
            Neighbour.SetOwner(Faction);

            AssertAccepted(GiftToEnemy(), "a faction, which signs no treaties, blocked the gift");
        }

        [TestMethod]
        public void ThePenaltyGrowsWithDifficulty()
        {
            CreateUniverseAndPlayerEmpire();
            AssertEqual(0.001f, 0.5f, new DifficultyModifiers(Enemy, GameDifficulty.Normal).WarBaitPenaltyMultiplier, "Normal");
            AssertEqual(0.001f, 1f, new DifficultyModifiers(Enemy, GameDifficulty.Hard).WarBaitPenaltyMultiplier, "Hard");
            AssertEqual(0.001f, 1.5f, new DifficultyModifiers(Enemy, GameDifficulty.Brutal).WarBaitPenaltyMultiplier, "Brutal");
            AssertEqual(0.001f, 2f, new DifficultyModifiers(Enemy, GameDifficulty.Insane).WarBaitPenaltyMultiplier, "Insane");
        }
    }
}
