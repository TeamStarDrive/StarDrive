using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI.Research;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;


namespace UnitTests.Technologies
{
    [TestClass]
    public class TestShipCostMods : StarDriveTest
    {
        readonly ChooseTech TechChooser;

        public TestShipCostMods()
        {
            CreateUniverseAndPlayerEmpire();
            PrepareShipAndEmpireForShipTechTests();
            TechChooser = new ChooseTech(Enemy);
        }

        ResearchOptions CreateResearchMod()
        {
            var researchMods = new ResearchOptions();
            researchMods.LoadResearchOptions(Enemy);
            researchMods.ChangePriority(ResearchOptions.ShipCosts.Randomize, 0);
            return researchMods;
        }

        [TestMethod]
        public void LoadShipCostModifiers()
        {
            var researchMods = new ResearchOptions();
            researchMods.LoadResearchOptions(Enemy);
            Assert.IsTrue(researchMods.Count() > 0);
        }

        [TestMethod]
        public void TestBestCombatShipIsNotNull()
        {
            Enemy.AbsorbEmpire(Player);
            TechChooser.PickResearchTopic("CHEAPEST");
            Assert.IsTrue(TechChooser.LineFocus.BestCombatShip != null, "Best combat ship was null");
        }

        public int ShipPickerReturnsATechCost()
        {
            var lineFocus = new ShipPicker(CreateResearchMod());
            var ship = ResourceManager.Ships.GetDesign("Rocket Scout");
            int techCost = lineFocus.GetModifiedShipCost(ship, Enemy, 1);
            Assert.IsTrue(techCost > 0);
            return techCost;
        }

        [TestMethod]
        public void InfraCostIncreasesShipCost()
        {
            int baseCost = ShipPickerReturnsATechCost();
            var researchMods = CreateResearchMod();
            // adjust researchModsToMakeSureValuesWork
            researchMods.ChangePriority(ResearchOptions.ShipCosts.BalanceToInfraIntensity, 100);
            var lineFocus  = new ShipPicker(researchMods);
            var ship = ResourceManager.Ships.GetDesign("Rocket Scout");
            float techCost = lineFocus.GetModifiedShipCost(ship, Enemy, 1);
            Assert.IsTrue(techCost < baseCost);
        }

        [TestMethod]
        public void WeaponTagReducesShipCost()
        {
            int baseCost = ShipPickerReturnsATechCost();
            var researchMods = CreateResearchMod();
            // adjust researchModsToMakeSureValuesWork
            researchMods.ChangePriority(WeaponTag.Missile, 0.1f);
            var lineFocus  = new ShipPicker(researchMods);
            var ship = ResourceManager.Ships.GetDesign("Rocket Scout");
            float techCost = lineFocus.GetModifiedShipCost(ship, Enemy, 1);
            Assert.IsTrue(techCost < baseCost);
        }

        [TestMethod]
        public void TechTypeReducesShipCost()
        {
            int baseCost = ShipPickerReturnsATechCost();
            var researchMods = CreateResearchMod();
            // adjust researchModsToMakeSureValuesWork
            researchMods.ChangePriority(TechnologyType.ShipWeapons, 0.1f);
            var lineFocus  = new ShipPicker(researchMods);
            var ship = ResourceManager.Ships.GetDesign("Rocket Scout");
            float techCost = lineFocus.GetModifiedShipCost(ship, Enemy, 1);
            Assert.IsTrue(techCost < baseCost);
        }
    }
}