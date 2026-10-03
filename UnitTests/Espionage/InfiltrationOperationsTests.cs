using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
            Empire player = loaded.UState.Player;
            float moneyBefore = player.Money;
            player.DoMoney();

            Assert.IsTrue(leeched > 0, "the enemy's income should have been leeched");
            Assert.AreEqual(leeched, player.TotalMoneyLeechedLastTurn, 0.001f, "the leech pending at save time must be paid after loading");
            Assert.AreEqual(player.NetIncome, player.Money - moneyBefore, 0.001f);
        }
    }
}
