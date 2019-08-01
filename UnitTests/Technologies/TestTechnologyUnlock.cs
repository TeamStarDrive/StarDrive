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
        public TestTechnologyUnlock()
        {
            LoadTechContent();
            CreateGameInstance();
        }

        void CreateTestEnv(out Empire empire)
        {
            CreateUniverseAndPlayerEmpire(out empire);
            Universe.aw = new AutomationWindow(Universe);
        }

        [TestMethod]
        public void TestTechUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "FrigateConstruction");
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
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "FrigateConstruction");
            foreach (string item in tech.GetUnLockableHulls(empire))
                Assert.IsTrue(empire.IsHullUnlocked(item));
        }

        [TestMethod]
        public void TestModuleUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "FrigateConstruction");
            foreach (Technology.UnlockedMod item in tech.GetUnlockableModules(empire))
                Assert.IsTrue(empire.IsModuleUnlocked(item.ModuleUID));
        }

        [TestMethod]
        public void TestBuildingUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "IndustrialFoundations");
            foreach (var item in tech.Tech.BuildingsUnlocked)
                Assert.IsTrue(empire.IsBuildingUnlocked(item.Name));
        }

        [TestMethod]
        public void TestBonusUnlock()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "Ace Training");
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(empire.data.BonusFighterLevels > 0);
                Assert.That.Equal(0, empire.data.BonusFighterLevels - item.Bonus);
            }
        }
        [TestMethod]
        public void TestBonusStacking()
        {
            CreateTestEnv(out Empire empire);
            TechEntry tech = UnlockTech(empire, "Ace Training");
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(empire.data.BonusFighterLevels > 0);
                Assert.That.Equal(0, empire.data.BonusFighterLevels - item.Bonus);
            }
            tech = UnlockTech(empire, "Ace Training");
            foreach (var item in tech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(empire.data.BonusFighterLevels > 0);
                Assert.That.Equal(0, empire.data.BonusFighterLevels - item.Bonus);
            }
        }

    }
}
