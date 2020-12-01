using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
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
            var ourRelation = us.GetRelations(Enemy);
            ourRelation.Known = true;

            us.data.DiplomaticPersonality.Territorialism = 60;
            us.data.DiplomaticPersonality.Opportunism = 0.2f;
            us.data.DiplomaticPersonality.Trustworthiness = 80;

            // basic qualifiers
            theirShip.Active = false;
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.Active = true;
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.engineState = Ship.MoveState.Warp;
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.engineState = Ship.MoveState.Sublight;
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            // advanced qualifiers
            Enemy.isFaction = false;

            // cant attack trade
            ourRelation.Treaty_Trade = true;
            theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));
            ourRelation.Treaty_Trade = false;

            // cant attack during peace.
            ourRelation.Treaty_Peace = true;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));
            ourRelation.Treaty_Peace = false;

            // faction tests
            Enemy.isFaction = true;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            ourRelation.Treaty_Trade = true;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));
            ourRelation.Treaty_Trade = false;

            ourRelation.Treaty_NAPact = true;
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));
            ourRelation.Treaty_NAPact = false;

            // faction reset treaties and anger
            theirShip.AI.ClearOrders();
            ourRelation.Treaty_NAPact = false;
            ourRelation.Treaty_Trade = false;
            ourRelation.Treaty_Peace = false;
            ResetAnger(ourRelation);
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            // violation tests.
            Enemy.isFaction = false;
            theirShip.AI.ClearOrders();
            ResetAnger(ourRelation);
            ourRelation.AddAngerShipsInOurBorders(100);
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.AI.ClearOrders();
            ResetAnger(ourRelation);
            ourRelation.AddAngerMilitaryConflict(100);
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.AI.ClearOrders();
            ResetAnger(ourRelation);
            ourRelation.AddAngerTerritorialConflict(100);
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.AI.ClearOrders();
            ResetAnger(ourRelation);
            ourRelation.UpdateRelationship(us, Enemy);
            theirShip.SetProjectorInfluence(us, true);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us,ourShip, theirShip,ourRelation));


            // war tests
            Enemy.isFaction = false;
            us.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            ourRelation.UpdateRelationship(us, Enemy);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));
        }

        void ResetAnger(Relationship rel)
        {
            rel.AddAngerMilitaryConflict(-rel.Anger_MilitaryConflict + 5);
            rel.AddAngerShipsInOurBorders(-rel.Anger_FromShipsInOurBorders + 5);
            rel.AddAngerTerritorialConflict(-rel.Anger_TerritorialConflict + 5);
        }

        string GetFailString(Empire us, Ship ourShip, Ship theirShip, Relationship ourRelation)
        {
            return $"relation attack: {ourRelation.CanAttack}  ship attack: {theirShip.IsAttackable(us, ourRelation)} Loyalty attack {us.IsEmpireAttackable(Enemy, theirShip)}";
        }

    }
}