using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;

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

        [TestMethod]
        public void AResearchDisruptionCutsResearchByItsShareAndWearsOffEveryTurn()
        {
            CreateUniverseAndPlayerEmpire();
            Ship_Game.Espionage espionage = Player.GetEspionage(Enemy);
            espionage.SetInfiltrationLevelTo(4);

            espionage.DisruptResearch(8f);
            espionage.DisruptResearch(5f);
            AssertEqual(0.0001f, 8f, espionage.ResearchDisruption, "a weaker result keeps the stronger cut that is still wearing off");

            Enemy.Research.UpdateNetResearch();
            AssertEqual(0.0001f, 0.92f, Enemy.Research.DisruptionMultiplier, "an 8% cut lets 92% of their research through, every turn");
            AssertEqual(80, Enemy.Research.DisruptionTurnsLeft, "it wears off in 80 turns at normal pace");

            espionage.Update(Player.CalcTotalEspionageWeight());
            AssertEqual(0.0001f, 7.9f, espionage.ResearchDisruption, "the cut shrinks by 0.1 points a turn without any operation running");
        }

        [TestMethod]
        public void AResearchDisruptionWearsOffSlowerAtASlowerPace()
        {
            CreateUniverseAndPlayerEmpire();
            UState.P.Pace = 10f;
            Ship_Game.Espionage espionage = Player.GetEspionage(Enemy);
            espionage.SetInfiltrationLevelTo(4);
            espionage.DisruptResearch(8f);

            espionage.Update(Player.CalcTotalEspionageWeight());

            AssertEqual(0.0001f, 7.99f, espionage.ResearchDisruption, "at 10x pace the cut shrinks by 0.01 points a turn");
            AssertEqual(799, espionage.ResearchDisruptionTurnsLeft, "and lasts ten times as many turns");
        }

        [TestMethod]
        public void AResearchDisruptionEndsOnTheTurnItsCountdownReachesZero()
        {
            CreateUniverseAndPlayerEmpire();
            Ship_Game.Espionage espionage = Player.GetEspionage(Enemy);
            espionage.SetInfiltrationLevelTo(4);
            espionage.DisruptResearch(8f);
            AssertEqual(80, espionage.ResearchDisruptionTurnsLeft, "setup: an 8% cut lasts 80 turns at normal pace");

            for (int turnsLeft = 79; turnsLeft >= 0; --turnsLeft)
            {
                espionage.Update(Player.CalcTotalEspionageWeight());
                AssertEqual(turnsLeft, espionage.ResearchDisruptionTurnsLeft, "the countdown drops by one every turn");
                AssertEqual(turnsLeft > 0, espionage.CanSlowResearch, $"the cut holds until the countdown reaches zero ({espionage.ResearchDisruption} left)");
            }
        }

        [TestMethod]
        public void AResearchDisruptionEndsWhenTheInfiltrationFallsBelowLevel4()
        {
            CreateUniverseAndPlayerEmpire();
            Ship_Game.Espionage espionage = Player.GetEspionage(Enemy);
            espionage.SetInfiltrationLevelTo(4);
            espionage.DisruptResearch(10f);

            espionage.ReduceInfiltrationLevel();

            AssertEqual(0.0001f, 0f, espionage.ResearchDisruption, "the cut is cleared when the infiltration level drops below 4");
        }

        [TestMethod]
        public void AnOldSavedDisruptionChanceLoadsAsACut()
        {
            CreateUniverseAndPlayerEmpire();
            Ship_Game.Espionage espionage = Player.GetEspionage(Enemy);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Ship_Game.Espionage).GetProperty("SlowResearchChance", flags)!.SetValue(espionage, 80);

            typeof(Ship_Game.Espionage).GetMethod("OnDeserialized", flags)!.Invoke(espionage, null);

            AssertEqual(0.0001f, 8f, espionage.ResearchDisruption, "an old save's chance of 80 loads as an 8% cut");
        }

        [TestMethod]
        public void LeechedMoneyIsPaidOnce()
        {
            CreateUniverseAndPlayerEmpire();
            Player.GetEspionage(Enemy).SetInfiltrationLevelTo(5);
            Enemy.data.FlatMoneyBonus = 1000;

            float moneyBefore = Player.Money;
            Enemy.DoMoney();
            Assert.AreEqual(moneyBefore, Player.Money, 0.001f, "nothing is paid while the victim's money is counted");
            Player.DoMoney();

            float leeched = Player.TotalMoneyLeechedLastTurn;
            Assert.IsTrue(leeched > 0, "the enemy's income should have been leeched");
            Assert.AreEqual(leeched, Player.GetEspionage(Enemy).TotalMoneyLeeched, 0.001f, "the income line shows what was leeched");
            Assert.AreEqual(Player.NetIncome, Player.Money - moneyBefore, 0.001f,
                            "leeched money reaches the treasury only through the Money Leeched income line");
        }

        [TestMethod]
        public void LeechFromAnEmpireDefeatedThatTurnIsStillPaid()
        {
            CreateUniverseAndPlayerEmpire();
            Player.GetEspionage(Enemy).SetInfiltrationLevelTo(5);
            Enemy.data.FlatMoneyBonus = 1000;

            Enemy.DoMoney();
            Enemy.SetAsDefeated();
            Player.DoMoney();

            float leeched = Player.GetEspionage(Enemy).TotalMoneyLeeched;
            Assert.IsTrue(leeched > 0, "the enemy's income should have been leeched");
            Assert.AreEqual(leeched, Player.TotalMoneyLeechedLastTurn, 0.001f);
        }

        [TestMethod]
        public void LeechedMoneySurvivesSaveAndLoad()
        {
            CreateUniverseAndPlayerEmpire();
            UState.StarDate = 1042.5f;
            Player.GetEspionage(Enemy).SetInfiltrationLevelTo(5);
            Enemy.data.FlatMoneyBonus = 1000;

            Enemy.DoMoney();
            Universe.EndOfTurnUpdate(UState.Empires, FixedSimTime.Zero);
            float leeched = Player.GetEspionage(Enemy).TotalMoneyLeeched;

            SavedGame save = Universe.Save("UnitTest.LeechedMoney", throwOnError: true);
            UniverseScreen loaded = LoadSave(save.SaveFile);
            Empire player = loaded.UState.Player;
            float moneyBefore = player.Money;
            player.DoMoney();

            Assert.IsTrue(leeched > 0, "the enemy's income should have been leeched");
            Assert.AreEqual(leeched, player.TotalMoneyLeechedLastTurn, 0.001f, "the leech pending at save time must be paid after loading");
            Assert.AreEqual(player.NetIncome, player.Money - moneyBefore, 0.001f);
        }
    }
}
