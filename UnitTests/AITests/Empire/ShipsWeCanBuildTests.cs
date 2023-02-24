using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class ShipsWeCanBuildTests : StarDriveTest
    {
        public ShipsWeCanBuildTests()
        {
            LoadStarterShips("Heavy Carrier mk5-b",
                             "Terran-Prototype",
                             "Fang Strafer",
                             "Platform Base mk1-a");
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
            var ship = SpawnShip("Heavy Carrier mk5-b", Player, Vector2.Zero);
            Player.RemoveBuildableShip(ship.ShipData);

            // verify that we can not currently add wanted ship
            Player.UpdateShipsWeCanBuild();
            Assert.IsFalse(Player.CanBuildShip(ship.ShipData), $"{ship.Name} Without tech this should not have been added. ");
        }

        [TestMethod]
        public void ShipsWillBeUnlockedAfterTechUnlock()
        {
            Player.ClearShipsWeCanBuild();
            var ship = SpawnShip("Heavy Carrier mk5-b", Player, Vector2.Zero);
            var prototype = SpawnShip("Terran-Prototype", Player, Vector2.Zero);

            UnlockAllTechsForShip(Player, ship.Name); // this must automatically unlock the ships
            Assert.IsTrue(Player.CanBuildShip(ship.ShipData), $"{ship.Name} Not found in ShipWeCanBuild");
            Assert.IsTrue(Player.canBuildCarriers, $"{ship.Name} did not set canBuildCarriers");

            UnlockAllTechsForShip(Player, prototype.Name);
            Assert.IsFalse(Player.CanBuildShip(prototype.ShipData), "Prototype ship added to shipswecanbuild");

            // Check that adding again does not does not trigger updates.
            Player.canBuildCarriers = false;
            Player.UpdateShipsWeCanBuild(new Array<string> { ship.BaseHull.HullName });
            Assert.IsFalse(Player.canBuildCarriers, "UpdateShipsWeCanBuild triggered unneeded updates");
        }

        [TestMethod]
        public void PlayerCreatedShipsAreUnlocked()
        {
            Player.ClearShipsWeCanBuild();
            UnlockAllTechsForShip(Player, "Rocket Scout");
            ShipDesign playerDesign = CreateTemplate("Rocket Scout", Player, playerDesign:true);
            Assert.IsTrue(Player.CanBuildShip(playerDesign), "BUG: Player ship was not added to ShipsWeCanBuild");
        }

        [TestMethod]
        public void LockedHullsAreNotAddedToBuild()
        {
            Player.ClearShipsWeCanBuild();
            UnlockAllTechsForShip(Player, "Heavy Carrier mk5-b");
            Player.UnlockedHullsDict.Clear(); // lock the hulls
            ShipDesign playerDesign = CreateTemplate("Heavy Carrier mk5-b", Player, playerDesign:true);
            Assert.IsFalse(Player.CanBuildShip(playerDesign), "BUG: Locked hull was added to ShipsWeCanBuild");
        }

        [TestMethod]
        public void EnemyCanUsePlayerDesignsIfAllowed()
        {
            Player.ClearShipsWeCanBuild();

            // add new enemy design
            UState.P.AIUsesPlayerDesigns = true;
            UnlockAllTechsForShip(Enemy, "Fang Strafer");
            ShipDesign playerDesign1 = CreateTemplate("Fang Strafer", Enemy, playerDesign:true);
            Assert.IsTrue(Enemy.CanBuildShip(playerDesign1), "Bug: Could not add valid design to shipswecanbuild");

            UState.P.AIUsesPlayerDesigns = false;
            ShipDesign playerDesign2 = CreateTemplate("Fang Strafer", Enemy, playerDesign:true);
            Assert.IsFalse(Enemy.CanBuildShip(playerDesign2), "Use Player design restriction added to shipswecanbuild");
        }

        [TestMethod]
        public void ShouldFailToAddIncompatibleShipDesigns()
        {
            Player.ClearShipsWeCanBuild();
            // fail to add incompatible design
            ShipDesign playerDesign1 = CreateTemplate("Supply Shuttle", Player, playerDesign:true);
            Assert.IsFalse(Player.CanBuildShip(playerDesign1), "Bug: Supply shuttle added to shipsWeCanBuild");
        }

        [TestMethod]
        public void ShouldAddBuildableStructure()
        {
            Player.ClearShipsWeCanBuild();
            ShipDesign structure = CreateTemplate("Platform Base mk1-a", Player, playerDesign:false);
            Assert.IsTrue(Player.CanBuildShip(structure), "Update Structures: ShipsWeCanBuild was not updated.");
            Assert.IsTrue(Player.CanBuildStation(structure), "Update Structures: StructuresWeCanBuild Was Not Updated");
        }

        ShipDesign CreateTemplate(string baseDesign, Ship_Game.Empire empire, bool playerDesign)
        {
            string newName;
            do
            {
                string key1 = empire.Random.Int(1, 999999).ToString();
                string key2 = empire.Random.Int(1, 999999).ToString();
                newName = baseDesign + $"-test-{key1}-test-{key2}";
            }
            while (ResourceManager.ShipTemplateExists(newName));

            Ship existingTemplate = ResourceManager.GetShipTemplate(baseDesign);
            ShipDesign newData = existingTemplate.ShipData.GetClone(newName);
            ResourceManager.AddShipTemplate(newData, playerDesign:playerDesign);
            empire.UpdateShipsWeCanBuild();
            return newData;
        }

    }
}
