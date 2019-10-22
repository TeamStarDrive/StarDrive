using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Technologies
{
    [TestClass]
    public class TestTechnologyUnlock : StarDriveTest
    {
        private Empire MajorEnemy;
        public TestTechnologyUnlock()
        {
            LoadTechContent();
            CreateGameInstance();
            CreateTestEnv(out _);
        }

        void CreateTestEnv(out Empire empire)
        {
            CreateUniverseAndPlayerEmpire(out empire);
            MajorEnemy = EmpireManager.CreateEmpireFromEmpireData(ResourceManager.MajorRaces[1]);
            Universe.aw = new AutomationWindow(Universe);
        }

        [TestMethod]
        public void TestTechUnlock()
        {
            TechEntry tech = Player.GetTechEntry("FrigateConstruction");
            Player.UnlockTech("FrigateConstruction", TechUnlockType.Normal);
            Assert.IsTrue(tech.Unlocked);
            //Ancient Repulsor
            tech = Player.GetTechEntry("Ancient Repulsor");
            Player.UnlockTech("Ancient Repulsor", TechUnlockType.Event);
            Assert.IsTrue(tech.Unlocked);

            tech = Player.GetTechEntry("Cruisers");
            Player.UnlockTech("Cruisers", TechUnlockType.Diplomacy);
            Assert.IsTrue(tech.Unlocked);

            tech = Player.GetTechEntry("IndustrialFoundations");
            for (int x =0; x< 100; x++)
            {
                Player.UnlockTech("IndustrialFoundations", TechUnlockType.Spy, MajorEnemy);
                if (tech.Unlocked)
                    break;
            }
            Assert.IsTrue(tech.Unlocked);
        }

        TechEntry UnlockTech(Empire empire, string UID)
        {
            TechEntry tech = empire.GetTechEntry(UID);
            empire.UnlockTech(tech.UID, TechUnlockType.Normal);
            return tech;
        }

        [TestMethod]
        public void TestHullUnlock()
        {
            TechEntry tech = UnlockTech(Player, "FrigateConstruction");
            int playerHulls = 0;
            foreach (string item in tech.GetUnLockableHulls(Player))
            {
                playerHulls++;
                Assert.IsTrue(Player.IsHullUnlocked(item));
            }
            Player.UnlockTech("FrigateConstruction", TechUnlockType.Diplomacy, MajorEnemy);
            //test unlock foreign hull
            int foreignHulls = 0;
            foreach (string item in tech.GetUnLockableHulls(Player))
            {
                foreignHulls++;
                Assert.IsTrue(Player.IsHullUnlocked(item));
            }
            Assert.IsTrue(foreignHulls > playerHulls);
        }

        [TestMethod]
        public void TestModuleUnlock()
        {
            TechEntry tech = UnlockTech(Player, "FrigateConstruction");
            foreach (Technology.UnlockedMod item in tech.GetUnlockableModules(Player))
                Assert.IsTrue(Player.IsModuleUnlocked(item.ModuleUID));
        }

        [TestMethod]
        public void TestBuildingUnlock()
        {
            TechEntry tech = UnlockTech(Player, "IndustrialFoundations");
            foreach (var item in tech.Tech.BuildingsUnlocked)
                Assert.IsTrue(Player.IsBuildingUnlocked(item.Name));
        }

        [TestMethod]
        public void TestBonusUnlock()
        {
            TechEntry tech = UnlockTech(Player, "Ace Training");
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(Player.data.BonusFighterLevels > 0);
            }
        }
        [TestMethod]
        public void TestAddResearchToTech()
        {
            TechEntry tech = Player.GetTechEntry("Ace Training");
            float cost = tech.TechCost;
            float leftOver;
            bool unlocked;
            leftOver = tech.AddToProgress(5, Player, out unlocked);
            Assert.IsTrue(tech.Progress.AlmostEqual(5f));
            Assert.IsTrue(leftOver.AlmostZero());
            Assert.IsTrue(!unlocked);

            leftOver = tech.AddToProgress(cost, Player, out unlocked);
            Assert.IsTrue(leftOver.AlmostEqual(5));
            Assert.IsTrue(unlocked);

        }
        [TestMethod]
        public void TestBonusStacking()
        {
            TechEntry tech = UnlockTech(Player, "Ace Training");
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(Player.data.BonusFighterLevels > 0);
                Assert.That.Equal(0, Player.data.BonusFighterLevels - item.Bonus);
            }
            tech = UnlockTech(Player, "Ace Training");
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(Player.data.BonusFighterLevels > 0);
                Assert.That.Equal(0, Player.data.BonusFighterLevels - item.Bonus);
            }
        }

    }
}
