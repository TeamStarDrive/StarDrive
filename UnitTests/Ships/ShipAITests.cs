using System;
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
            var ourRelation   = us.GetRelations(Enemy);
            ourRelation.Known = true;

            us.data.DiplomaticPersonality.Territorialism  = 60;
            us.data.DiplomaticPersonality.Opportunism     = 0.2f;
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

            // cant attack trade
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.Treaty_Trade = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Trade: " +GetFailString(us, ourShip, theirShip, ourRelation));

            // cant attack during peace.
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.Treaty_Peace = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Peace: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction tests
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.isFaction = true;
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Faction: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction trade works like empire
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.isFaction = true;
                ourRelation.Treaty_Trade = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction Trade: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction na pact
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.isFaction = true;
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.isFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.isFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(null), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction reset treaties and anger
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.isFaction = true;
                theirShip.SetProjectorInfluence(us, true);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Faction" + GetFailString(us, ourShip, theirShip, ourRelation));

            // violation tests.
            // anger inborders
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerShipsInOurBorders(100);
                theirShip.SetProjectorInfluence(us, true);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Anger Borders" + GetFailString(us, ourShip, theirShip, ourRelation));

            // anger military conflict
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerMilitaryConflict(100);
                theirShip.SetProjectorInfluence(us, true);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Anger Military:" + GetFailString(us, ourShip, theirShip, ourRelation));

            // anger territorial
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerTerritorialConflict(100);
                theirShip.SetProjectorInfluence(us, true);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), $"Territory: {GetFailString(us, ourShip, theirShip, ourRelation)}");

            // sanity test no environment
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                theirShip.SetProjectorInfluence(us, true);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us,ourShip, theirShip,ourRelation));

            // war tests
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.isFaction = false;
                us.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), $"War: {GetFailString(us, ourShip, theirShip, ourRelation)}");

            // Empire attack can attacked tests.
            // faction attack napact
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.isFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(Player.IsEmpireAttackable(Enemy), "Faction attack NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.isFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            });
            Assert.IsTrue(Player.IsEmpireHostile(Enemy), "Faction NA Pact hostile: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // player tests
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourShip.AI.HasPriorityTarget = true;
            });
            Empire.UpdateBilateralRelations(us, Enemy);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Play chooses target" + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourShip.AI.HasPriorityTarget    = true;
                ourRelation.Treaty_Alliance     = true;
            });
            Empire.UpdateBilateralRelations(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Player Chooses Alliance Target: " + GetFailString(us, ourShip, theirShip, ourRelation));
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

        public void SetEnvironment(Empire us, Ship theirShip, Relationship ourRelation, Action setEnvironment)
        {
            theirShip.SetProjectorInfluence(us, false);
            theirShip.ResetProjectorInfluence();
            theirShip.AI.ClearOrders();

            Enemy.isFaction                 = false;
            Player.isFaction                = false;
            ourRelation.Treaty_NAPact       = false;
            ourRelation.Treaty_Trade        = false;
            ourRelation.Treaty_Peace        = false;
            ourRelation.PeaceTurnsRemaining = 0;
            ourRelation.AtWar               = false;
            ourRelation.PreparingForWar     = false;
            ourRelation.ActiveWar           = null;

            ResetAnger(ourRelation);
            theirShip.AI.ClearOrders();

            setEnvironment.Invoke();
            ourRelation.UpdateRelationship(us, Enemy);
        }
    }
}