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

            // cant attack during peace.
            ourRelation.Treaty_Peace = true;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip));
            ourRelation.Treaty_Peace = false;
            
            // cant attack trade
            ourRelation.Treaty_Trade = true;
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);

            Enemy.isFaction = true;
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip));
        }
    }
}