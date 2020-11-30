using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass()]
    public class ShipAITests : StarDriveTest
    {
        public ShipAITests()
        {
            CreateGameInstance();
            LoadStarterShips(new[] {"Excalibur-Class Supercarrier", "Owlwok Freighter S"});
        }

        void CreateTestEnv(out Empire empire, out Ship ship)
        {
            CreateUniverseAndPlayerEmpire(out empire);
            ship = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", empire, Vector2.Zero);
        }
        [TestMethod()]
        public void IsTargetValidTest()
        {
            CreateTestEnv(out Empire us, out Ship ourShip);
            
            Ship theirShip    = Ship.CreateShipAtPoint("Owlwok Freighter S", Enemy, Vector2.Zero);
            ourShip.AI.Target = theirShip;

            // basic qualifiers
            theirShip.Active = false;
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip));

            theirShip.Active = true;
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip));

            theirShip.engineState = Ship.MoveState.Warp;
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip));

            theirShip.engineState = Ship.MoveState.Sublight;
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip));

            // advanced qualifiers
            Enemy.isFaction = false;
            var ourRelation = us.GetRelations(Enemy);
            ourRelation.Known = true;

            // cant attack trade
            ourRelation.Treaty_Trade = true;
            theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip));

            // cant attack during peace.
            ourRelation.Treaty_Peace = true;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip));
            ourRelation.Treaty_Peace = false;

            // faction tests
            // still trading!
            Enemy.isFaction = true;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip));

            ourRelation.Treaty_Trade = false;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip));
            
            ourRelation.Treaty_NAPact = true;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip));

            theirShip.AI.ClearOrders();
            ourRelation.AddAngerShipsInOurBorders(1);
            ourRelation.AddAngerMilitaryConflict(1);
            ourRelation.AddAngerTerritorialConflict(1);
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip));


            theirShip.AI.ClearOrders();
            ourRelation.AddAngerShipsInOurBorders(100);
            ourRelation.AddAngerMilitaryConflict(1);
            ourRelation.AddAngerTerritorialConflict(1);
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip));

            theirShip.AI.ClearOrders();
            ourRelation.AddAngerShipsInOurBorders(1);
            ourRelation.AddAngerMilitaryConflict(100);
            ourRelation.AddAngerTerritorialConflict(1);
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip));

            // war tests
            Enemy.isFaction = false;
            us.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip));

        }
    }
}