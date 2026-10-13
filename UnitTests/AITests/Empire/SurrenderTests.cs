using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class SurrenderTests : StarDriveTest
    {
        public SurrenderTests()
        {
            CreateUniverseAndPlayerEmpire();
            CreateThirdMajorEmpire();
            Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
            Enemy.TestSetPersonality("Aggressive");
            ThirdMajor.TestSetPersonality("Aggressive");
            AddDummyPlanetToEmpire(new Vector2(100_000), ThirdMajor);
        }

        [TestMethod]
        public void AnEmpireThatLostItsLastPlanetIsDefeatedInsteadOfSurrendering()
        {
            AssertEqual(0, Enemy.GetPlanets().Count, "setup: the empire has lost its last planet");

            Assert.IsFalse(Enemy.TryMergeOrSurrender(ThirdMajor), "an empire with no planets left cannot surrender");
            Assert.IsFalse(Enemy.IsDefeated, "setup: nothing defeated it before its turn");
            Assert.IsTrue(Enemy.UpdateIsEmpireDefeated(), "its turn defeats it");
            Assert.IsNull(Enemy.data.AbsorbedBy, "and nobody absorbs it");
        }

        [TestMethod]
        public void AnEmpireThatStillHoldsAPlanetCanSurrender()
        {
            AddDummyPlanetToEmpire(new Vector2(200_000), Enemy);

            Assert.IsTrue(Enemy.TryMergeOrSurrender(ThirdMajor), "a beaten empire that still holds a planet surrenders");
            AssertEqual(ThirdMajor.data.Traits.Name, Enemy.data.AbsorbedBy, "to the empire beating it");
        }

        [TestMethod]
        public void NoEmpireSurrendersToOneThatHasLostItsLastPlanet()
        {
            AddDummyPlanetToEmpire(new Vector2(200_000), Enemy);
            ThirdMajor.GetPlanets()[0].SetOwner(Player);

            Assert.IsFalse(Enemy.TryMergeOrSurrender(ThirdMajor), "an empire with no planets left absorbs nobody");
            Assert.IsNull(Enemy.data.AbsorbedBy);
        }

        [TestMethod]
        public void InEliminationModeAnEmpireThatLostItsCapitalCannotSurrender()
        {
            Planet capital = AddDummyPlanetToEmpire(new Vector2(200_000), Enemy);
            AddDummyPlanetToEmpire(new Vector2(300_000), Enemy);
            Enemy.SetCapital(capital);
            capital.SetOwner(Player);
            UState.P.EliminationMode = true;
            Assert.IsFalse(Enemy.TryMergeOrSurrender(ThirdMajor), "in Elimination mode losing the capital ends the empire");

            UState.P.EliminationMode = false;
            Assert.IsTrue(Enemy.TryMergeOrSurrender(ThirdMajor), "control: otherwise it still holds a planet and surrenders");
        }

        [TestMethod]
        public void TheLateGameFederationSkipsAnEmpireWithNoPlanets()
        {
            AddDummyPlanetToEmpire(new Vector2(200_000), Enemy);
            Planet last = AddDummyPlanetToEmpire(new Vector2(300_000), ThirdMajor);
            UState.StarDate = 1100f;
            Player.TotalScore = 1000;
            Enemy.TotalScore = 400;
            ThirdMajor.TotalScore = 100;
            MethodInfo checkFederation = typeof(Ship_Game.Empire).GetMethod("CheckFederationVsPlayer", BindingFlags.NonPublic | BindingFlags.Instance);

            ThirdMajor.GetPlanets()[0].SetOwner(Player);
            last.SetOwner(Player);
            checkFederation.Invoke(Player, new object[] { UState });
            Assert.IsNull(ThirdMajor.data.AbsorbedBy, "the biggest AI does not absorb an empire that has lost its last planet");

            last.SetOwner(ThirdMajor);
            checkFederation.Invoke(Player, new object[] { UState });
            AssertEqual(Enemy.data.Traits.Name, ThirdMajor.data.AbsorbedBy, "control: one that still holds a planet is absorbed");
        }

        [TestMethod]
        public void TheLateGameFederationSkipsABiggestAIWithNoPlanets()
        {
            UState.StarDate = 1100f;
            Player.TotalScore = 1000;
            Enemy.TotalScore = 400;
            ThirdMajor.TotalScore = 100;
            MethodInfo checkFederation = typeof(Ship_Game.Empire).GetMethod("CheckFederationVsPlayer", BindingFlags.NonPublic | BindingFlags.Instance);

            checkFederation.Invoke(Player, new object[] { UState });
            Assert.IsNull(ThirdMajor.data.AbsorbedBy, "an AI that has lost its last planet absorbs nobody, however big its score");

            AddDummyPlanetToEmpire(new Vector2(200_000), Enemy);
            checkFederation.Invoke(Player, new object[] { UState });
            AssertEqual(Enemy.data.Traits.Name, ThirdMajor.data.AbsorbedBy, "control: once it holds a planet it absorbs the smaller AI");
        }

        [TestMethod]
        public void AlliesWithNoPlanetsLeftDoNotMerge()
        {
            AddHomeWorldToEmpire(new Vector2(400_000), Player);
            Relationship alliance = Enemy.GetRelations(ThirdMajor);
            alliance.Treaty_Alliance = true;
            alliance.Trust = 150;
            alliance.TotalAnger = 0;
            alliance.TurnsAbove95 = 100_000;
            Enemy.TotalScore = 100;
            ThirdMajor.TotalScore = 1000;
            SetPopulation(Player, 10);
            SetPopulation(Enemy, 0.1f);
            SetPopulation(ThirdMajor, 2);
            MethodInfo federate = typeof(Relationship).GetMethod("Federate", BindingFlags.NonPublic | BindingFlags.Instance);

            alliance.turnsSinceLastContact = 100_000;
            federate.Invoke(alliance, new object[] { Enemy, ThirdMajor });
            Assert.IsNull(Enemy.data.AbsorbedBy, "an AI that has lost its last planet does not merge into its ally");

            AddDummyPlanetToEmpire(new Vector2(200_000), Enemy);
            Planet allysLast = ThirdMajor.GetPlanets()[0];
            allysLast.SetOwner(Player);
            alliance.turnsSinceLastContact = 100_000;
            federate.Invoke(alliance, new object[] { Enemy, ThirdMajor });
            Assert.IsNull(Enemy.data.AbsorbedBy, "nor into an ally that has lost its last planet");

            allysLast.SetOwner(ThirdMajor);
            alliance.turnsSinceLastContact = 100_000;
            federate.Invoke(alliance, new object[] { Enemy, ThirdMajor });
            AssertEqual(ThirdMajor.data.Traits.Name, Enemy.data.AbsorbedBy, "control: allies that both hold planets merge");
        }

        [TestMethod]
        public void AnAIWithNoPlanetsLeftDoesNotJoinThePlayerThroughAFederationQuest()
        {
            Relationship toPlayer = Enemy.GetRelations(Player);
            toPlayer.FedQuest = new FederationQuest { type = QuestType.DestroyEnemy, EnemyName = ThirdMajor.data.Traits.Name };
            ThirdMajor.GetPlanets()[0].SetOwner(Player);
            Assert.IsTrue(ThirdMajor.UpdateIsEmpireDefeated(), "setup: the empire the quest asked the player to destroy falls");
            MethodInfo questAbsorb = typeof(Relationship).GetMethod("AttemptAIFederationAbsorb", BindingFlags.NonPublic | BindingFlags.Instance);

            questAbsorb.Invoke(toPlayer, new object[] { Enemy });
            Assert.IsNull(Enemy.data.AbsorbedBy, "an AI that has lost its last planet does not join the player, it is defeated");

            AddDummyPlanetToEmpire(new Vector2(200_000), Enemy);
            questAbsorb.Invoke(toPlayer, new object[] { Enemy });
            AssertEqual(Player.data.Traits.Name, Enemy.data.AbsorbedBy, "control: one that still holds a planet joins the player");
        }

        static void SetPopulation(Ship_Game.Empire empire, float billions)
            => typeof(Ship_Game.Empire).GetProperty(nameof(Ship_Game.Empire.TotalPopBillion)).SetValue(empire, billions);
    }
}
