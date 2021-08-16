using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class ShipsWeCanBuildTests : StarDriveTest
    {
        public ShipsWeCanBuildTests()
        {
            LoadStarterShips("Excalibur-Class Supercarrier", "Prototype Frigate",
                             "Flak Fang", "Rocket Inquisitor", "PLT-Defender");
            CreateUniverseAndPlayerEmpire("Human");
        }

        [TestCleanup]
        public void Teardown()
        {
            ReloadStarterShips();
        }

        [TestMethod]
        public void ShipsCannotBeUnlockedIfWeLackTech()
        {
            var ship = SpawnShip("Excalibur-Class Supercarrier", Player, Vector2.Zero);
            Player.ShipsWeCanBuild.Remove(ship.Name);

            // verify that we can not currently add wanted ship
            Player.UpdateShipsWeCanBuild();
            Assert.IsFalse(Player.ShipsWeCanBuild.Contains(ship.Name), $"{ship.Name} Without tech this should not have been added. ");
        }

        [TestMethod]
        public void ShipsWillBeUnlockedAfterTechUnlock()
        {
            var ship = SpawnShip("Excalibur-Class Supercarrier", Player, Vector2.Zero);
            var prototype = SpawnShip("Prototype Frigate", Player, Vector2.Zero);
            Player.ShipsWeCanBuild.Clear();

            UnlockAllTechsForShip(Player, ship.Name); // this must automatically unlock the ships
            Assert.IsTrue(Player.ShipsWeCanBuild.Contains(ship.Name), $"{ship.Name} Not found in ShipWeCanBuild");
            Assert.IsTrue(Player.canBuildCarriers, $"{ship.Name} did not set canBuildCarriers");

            UnlockAllTechsForShip(Player, prototype.Name);
            Assert.IsFalse(Player.ShipsWeCanBuild.Contains(prototype.Name), "Prototype ship added to shipswecanbuild");

            // Check that adding again does not does not trigger updates.
            Player.canBuildCarriers = false;
            Player.UpdateShipsWeCanBuild(new Array<string> { ship.BaseHull.HullName });
            Assert.IsFalse(Player.canBuildCarriers, "UpdateShipsWeCanBuild triggered unneeded updates");
        }

        [TestMethod]
        public void PlayerCreatedShipsAreUnlocked()
        {
            Player.ShipsWeCanBuild.Clear();
            UnlockAllTechsForShip(Player, "Rocket Inquisitor");
            string playerDesign = CreateTemplate("Rocket Inquisitor", Player, playerDesign:true);
            Assert.IsTrue(Player.ShipsWeCanBuild.Contains(playerDesign), "BUG: Player ship was not added to ShipsWeCanBuild");
        }

        [TestMethod]
        public void LockedHullsAreNotAddedToBuild()
        {
            Player.ShipsWeCanBuild.Clear();
            UnlockAllTechsForShip(Player, "Excalibur-Class Supercarrier");
            Player.UnlockedHullsDict.Clear(); // lock the hulls
            string playerDesign = CreateTemplate("Excalibur-Class Supercarrier", Player, playerDesign:true);
            Assert.IsFalse(Player.ShipsWeCanBuild.Contains(playerDesign), "BUG: Locked hull was added to ShipsWeCanBuild");
        }

        [TestMethod]
        public void EnemyCanUsePlayerDesignsIfAllowed()
        {
            Player.ShipsWeCanBuild.Clear();

            // add new enemy design
            GlobalStats.UsePlayerDesigns = true;
            UnlockAllTechsForShip(Enemy, "Flak Fang");
            string playerDesign1 = CreateTemplate("Flak Fang", Enemy, playerDesign:true);
            Assert.IsTrue(Enemy.ShipsWeCanBuild.Contains(playerDesign1), "Bug: Could not add valid design to shipswecanbuild");

            GlobalStats.UsePlayerDesigns = false;
            string playerDesign2 = CreateTemplate("Flak Fang", Enemy, playerDesign:true);
            Assert.IsFalse(Enemy.ShipsWeCanBuild.Contains(playerDesign2), "Use Player design restriction added to shipswecanbuild");
        }

        [TestMethod]
        public void ShouldFailToAddIncompatibleShipDesigns()
        {
            Player.ShipsWeCanBuild.Clear();
            // fail to add incompatible design
            string playerDesign1 = CreateTemplate("Supply Shuttle", Player, playerDesign:true);
            Assert.IsFalse(Player.ShipsWeCanBuild.Contains(playerDesign1), "Bug: Supply shuttle added to shipsWeCanBuild");
        }

        [TestMethod]
        public void ShouldAddBuildableStructure()
        {
            Player.ShipsWeCanBuild.Clear();
            string structure = CreateTemplate("PLT-Defender", Player, playerDesign:false);
            Assert.IsTrue(Player.ShipsWeCanBuild.Contains(structure), "Update Structures: ShipsWeCanBuild was not updated.");
            Assert.IsTrue(Player.structuresWeCanBuild.Contains("PLT-Defender"), "Update Structures: StructuresWeCanBuild Was Not Updated");
        }

        string CreateTemplate(string baseDesign, Ship_Game.Empire empire, bool playerDesign)
        {
            string newName;
            do
            {
                string key1 = RandomMath.IntBetween(1, 999999).ToString();
                string key2 = RandomMath.IntBetween(1, 999999).ToString();
                newName = baseDesign + $"-test-{key1}-test-{key2}";
            }
            while (ResourceManager.ShipTemplateExists(newName));

            Ship existingTemplate = ResourceManager.GetShipTemplate(baseDesign);
            ShipData newData = existingTemplate.shipData.GetClone();
            newData.Name = newName;
            ResourceManager.AddShipTemplate(newData, playerDesign:playerDesign);
            empire.UpdateShipsWeCanBuild();
            return newName;
        }

    }
}
