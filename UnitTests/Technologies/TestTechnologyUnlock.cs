﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Technologies
{
    [TestClass]
    public class TestTechnologyUnlock : StarDriveTest
    {
        public TestTechnologyUnlock()
        {
            CreateUniverseAndPlayerEmpire();
            Universe.aw = new AutomationWindow(Universe);
            LoadStarterShips("TEST_Heavy Carrier mk1");
        }

        [TestMethod]
        public void TechUnlock()
        {
            TechEntry normalTech = Player.GetTechEntry("FrigateConstruction");
            Player.UnlockTech("FrigateConstruction", TechUnlockType.Normal);
            Assert.IsTrue(normalTech.Unlocked, "Unlocking tech via normal process failed");

            TechEntry eventTech = Player.GetTechEntry("Ancient Repulsor");
            Player.UnlockTech("Ancient Repulsor", TechUnlockType.Event);
            Assert.IsTrue(eventTech.Unlocked, "Unlocking tech via spy event failed");

            TechEntry diplomacyTech = Player.GetTechEntry("Cruisers");
            Player.UnlockTech("Cruisers", TechUnlockType.Diplomacy);
            Assert.IsTrue(diplomacyTech.Unlocked, "Unlocking tech via diplomacy process failed");

            TechEntry spyTech = Player.GetTechEntry("IndustrialFoundations");
            //spy tech currently has a random chance to unlock.
            for (int x =0; x< 100; x++)
            {
                Player.UnlockTech("IndustrialFoundations", TechUnlockType.Spy, Enemy);
                if (spyTech.Unlocked)
                    break;
            }
            Assert.IsTrue(spyTech.Unlocked,"Unlocking tech via spy process failed");
        }

        TechEntry UnlockTech(Empire empire, string UID)
        {
            TechEntry tech = empire.GetTechEntry(UID);
            empire.UnlockTech(tech.UID, TechUnlockType.Normal);
            return tech;
        }

        [TestMethod]
        public void HullUnlock()
        {
            TechEntry hullTech = UnlockTech(Player, "FrigateConstruction");
            int playerHulls = 0;
            foreach (string item in hullTech.GetUnLockableHulls(Player))
            {
                playerHulls++;
                Assert.IsTrue(Player.IsHullUnlocked(item), $"Standard hull tech not unlocked: {item}");
            }

            //Unlock hulls from other empire.
            Player.UnlockTech("FrigateConstruction", TechUnlockType.Diplomacy, Enemy);
            int foreignHulls = 0;
            foreach (string item in hullTech.GetUnLockableHulls(Player))
            {
                foreignHulls++;
                Assert.IsTrue(Player.IsHullUnlocked(item), $"Foreign hull not unlocked{item}");
            }
            Assert.IsTrue(foreignHulls > playerHulls, "Failed to unlock all expected hulls");
        }

        [TestMethod]
        public void ModuleUnlock()
        {
            TechEntry moduleTech = UnlockTech(Player, "FrigateConstruction");
            foreach (Technology.UnlockedMod item in moduleTech.GetUnlockableModules(Player))
                Assert.IsTrue(Player.IsModuleUnlocked(item.ModuleUID)
                    , $"Expected Module not Unlocked: {item.ModuleUID}");
        }

        [TestMethod]
        public void BuildingUnlock()
        {
            TechEntry buildingTech = UnlockTech(Player, "IndustrialFoundations");
            foreach (var item in buildingTech.Tech.BuildingsUnlocked)
                Assert.IsTrue(Player.IsBuildingUnlocked(item.Name)
                    ,$"Expected building not unlocked: {item.Name}");
        }

        [TestMethod]
        public void BonusUnlock()
        {
            TechEntry bonusTech = UnlockTech(Player, "Ace Training");
            foreach (var item in bonusTech.Tech.BonusUnlocked)
            {
                Assert.IsTrue(Player.data.BonusFighterLevels > 0, $"Bonus not unlocked: {item.Name}");
            }
        }

        [TestMethod]
        public void ModuleHealthBonusUnlock()
        {
            float expectedBonus = 0.15f;
            string hpTechName = "Nanoweave Metallurgy";
            string bonusName = "Hull Strengthening";
            Ship ship  = SpawnShip("TEST_Heavy Carrier mk1", Player, new Vector2(0,0));
            RunObjectsSim(TestSimStep);
            float expectedHealth = ship.Health * (1 + expectedBonus);
            TechEntry bonusTech = UnlockTech(Player, hpTechName);

            AssertEqual(hpTechName, bonusTech.Tech.UID, $"{hpTechName} not found.");
            Assert.IsTrue(bonusTech.Tech.BonusUnlocked.NotEmpty, $"No bonus unlocks found in {bonusTech.Tech.UID}");
            AssertEqual(bonusName, bonusTech.Tech.BonusUnlocked[0].Name, $"Expxcted bonus name: {bonusName}");

            float bonus = bonusTech.Tech.BonusUnlocked[0].Bonus;
            AssertEqual(expectedBonus, bonus, $"Bonus should be equal to expected bonus({expectedBonus})");
            AssertEqual(expectedHealth, ship.Health, $"Ship health after HP bonus " +
                $"unload should be {expectedBonus * 100}% more");
        }

        [TestMethod]
        public void AddResearchToTech()
        {
            TechEntry techProgress = Player.GetTechEntry("Ace Training");
            float cost = techProgress.TechCost;
            float leftOver;
            bool unlocked;
            leftOver = techProgress.AddToProgress(5, Player, out unlocked);
            Assert.IsTrue(techProgress.Progress.AlmostEqual(5f));
            Assert.IsTrue(leftOver.AlmostZero());
            Assert.IsFalse(unlocked, $"Tech should not have beeen unlocked: {techProgress.UID}");

            leftOver = techProgress.AddToProgress(cost, Player, out unlocked);
            Assert.IsTrue(leftOver.AlmostEqual(5));
            Assert.IsTrue(unlocked);
        }

        [TestMethod]
        public void BonusStacking()
        {
            TechEntry bonus = UnlockTech(Player, "Ace Training");
            foreach (var item in bonus.Tech.BonusUnlocked)
            {
                Assert.IsTrue(Player.data.BonusFighterLevels > 0, $"Bonus not unlocked {item.Name}");
                AssertEqual(0, Player.data.BonusFighterLevels - item.Bonus);
            }
            TechEntry bonusStack = UnlockTech(Player, "Ace Training");
            foreach (var item in bonusStack.Tech.BonusUnlocked)
            {
                Assert.IsTrue(Player.data.BonusFighterLevels > 0, "Bonus not unlocked");
                AssertEqual(0, Player.data.BonusFighterLevels - item.Bonus);
            }
        }

        [TestMethod]
        public void UnlockByConquest()
        {
            TechEntry[] playerTechs = Player.UnlockedTechs;
            UnlockTech(Enemy, "Centralized Banking");
            UnlockTech(Enemy, "Disintegrator Array");
            TechEntry[] enemyTechs = Enemy.UnlockedTechs;
            Player.AssimilateTech(Enemy);

            TechEntry[] playerTechs2 = Player.UnlockedTechs.Sorted(e => e.UID);
            TechEntry[] newUnlocks = playerTechs2.Except(playerTechs).Sorted(e => e.UID);

            string[] expected = playerTechs.Select(e => e.UID)
                                .Concat(enemyTechs.Select(e => e.UID))
                                .Unique()
                                .Sorted(e => e);
            string[] actual = playerTechs2.Select(e => e.UID);

            AssertEqual(2, newUnlocks.Length);
            AssertEqual("Centralized Banking", newUnlocks[0].UID);
            AssertEqual("Disintegrator Array", newUnlocks[1].UID);
            AssertEqual(Enemy.data.ShipType, newUnlocks[0].ConqueredSource[0]);
            AssertEqual(Enemy.data.ShipType, newUnlocks[1].ConqueredSource[0]);
            AssertEqual(Enemy.data.ShipType, newUnlocks[0].WasAcquiredFrom[0]);
            AssertEqual(Enemy.data.ShipType, newUnlocks[1].WasAcquiredFrom[0]);

            AssertEqual(expected, actual, "Assimilated techs should be equal to conquered empire techs");
        }
    }
}
