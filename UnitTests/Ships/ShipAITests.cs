using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass()]
    public class ShipAITests : StarDriveTest
    {
        public ShipAITests()
        {
            LoadStarterShips("Heavy Carrier mk5-b",
                             "Owlwok Freighter S",
                             "Subspace Projector");
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void IsTargetValidTest()
        {
            Empire us = Player;
            Ship ourShip = SpawnShip("Heavy Carrier mk5-b", us, Vector2.Zero);
            
            Ship theirShip    = SpawnShip("Owlwok Freighter S", Enemy, Vector2.Zero);
            ourShip.AI.Target = theirShip;
            var ourRelation   = us.GetRelations(Enemy);
            us.SetRelationsAsKnown(ourRelation, Enemy);

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
                Enemy.IsFaction = true;
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Faction: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction trade works like empire
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = true;
                ourRelation.Treaty_Trade = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction Trade: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction na pact
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = true;
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.IsFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.IsFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(null), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            Ship ourProjector = SpawnShip("Subspace Projector", Player, Vector2.Zero);

            // faction reset treaties and anger
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = true;
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Faction" + GetFailString(us, ourShip, theirShip, ourRelation));

            // violation tests.
            // anger inborders
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerShipsInOurBorders(100);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Anger Borders" + GetFailString(us, ourShip, theirShip, ourRelation));

            // anger military conflict
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerMilitaryConflict(100);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Anger Military:" + GetFailString(us, ourShip, theirShip, ourRelation));

            // anger territorial
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerTerritorialConflict(100);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), $"Territory: {GetFailString(us, ourShip, theirShip, ourRelation)}");

            // sanity test no environment
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us,ourShip, theirShip,ourRelation));

            ourProjector.InstantKill();

            // war tests
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = false;
                us.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), $"War: {GetFailString(us, ourShip, theirShip, ourRelation)}");

            // Empire attack can attacked tests.
            // faction attack napact
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.IsFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(Player.IsEmpireAttackable(Enemy), "Faction attack NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.IsFaction = true;
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
                ourShip.AI.HasPriorityTarget = true;
                ourRelation.Treaty_Alliance = true;
            });
            Empire.UpdateBilateralRelations(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Player Chooses Alliance Target: " + GetFailString(us, ourShip, theirShip, ourRelation));
        }

        [TestMethod]
        public void IsTargetValidTargetedBy()
        {
            CreateThirdMajorEmpire();

            Empire aggressive = Enemy;
            Empire peaceful   = ThirdMajor;

            aggressive.data.DiplomaticPersonality.Trustworthiness = 50;
            peaceful.data.DiplomaticPersonality.Trustworthiness   = 50;

            Ship aggressiveShip = SpawnShip("Heavy Carrier mk5-b", aggressive, Vector2.Zero);
            Ship peacefulShip   = SpawnShip("Heavy Carrier mk5-b", peaceful, Vector2.Zero);
            var aggressiveRel   = aggressive.GetRelations(peaceful);
            var peacefulRel     = peaceful.GetRelations(aggressive);

            aggressiveRel.UpdateRelationship(aggressive, peaceful);
            peacefulRel.UpdateRelationship(peaceful, aggressive);

            Assert.IsFalse(aggressiveShip.IsAttackable(peaceful, peacefulRel), "Aggressive Ship should not be attackable");
            Assert.IsFalse(peacefulShip.IsAttackable(aggressive, aggressiveRel), "Peaceful Ship should not be attackable");
            
            aggressiveRel.TotalAnger = 100;
            Assert.IsFalse(peaceful.IsEmpireAttackable(aggressive), "Peaceful Empire should not be attackable");
            Assert.IsFalse(aggressive.IsEmpireAttackable(peaceful), "Aggressive Empire should not be attackable");

            // Ship can be attacked even when empire is not attackable due to total anger (AttackForTransgressions)
            Assert.IsTrue(peacefulShip.IsAttackable(aggressive, aggressiveRel), "Peaceful Ship should be attackable since other party is angry");
            Assert.IsFalse(aggressiveShip.IsAttackable(peaceful, peacefulRel), "Aggressive ship should not be attackable, since Peaceful empire is not angry");

            aggressiveShip.AI.Target = peacefulShip;
            Assert.IsTrue(aggressiveShip.IsAttackable(peaceful, peacefulRel), "Aggressive ship should now be attackable since it is targeting the peaceful ship");
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
            theirShip.AI.ClearOrders();

            Enemy.IsFaction                 = false;
            Player.IsFaction                = false;
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