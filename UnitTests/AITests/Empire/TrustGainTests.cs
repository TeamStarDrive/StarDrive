using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Universe;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class TrustGainTests : StarDriveTest
    {
        Relationship AiToPlayer;

        void AtPeace(GameDifficulty difficulty, string aiPersonality)
        {
            CreateUniverseAndPlayerEmpire(settings: new UniverseParams { Difficulty = difficulty });
            AiToPlayer = Enemy.GetRelations(Player);
            AiToPlayer.AtWar = Player.GetRelations(Enemy).AtWar = false;
            Enemy.data.DiplomaticPersonality = new DTrait { Name = aiPersonality };
            Player.data.DiplomaticPersonality = new DTrait { Name = "Ruthless" };
            AiToPlayer.Trust = 0;
        }

        float TrustGainedInATurn()
        {
            float before = AiToPlayer.Trust;
            AiToPlayer.AdvanceRelationshipTurn(Enemy, Player);
            return AiToPlayer.Trust - before;
        }

        float TreatyBoundPacifistGain(GameDifficulty difficulty)
        {
            AtPeace(difficulty, "Pacifist");
            Enemy.SignTreatyWith(Player, TreatyType.NonAggression);
            Enemy.SignTreatyWith(Player, TreatyType.Trade);
            Enemy.SignTreatyWith(Player, TreatyType.OpenBorders);
            Enemy.SignTreatyWith(Player, TreatyType.Alliance);
            return TrustGainedInATurn();
        }

        float HonorableGainAfterSpiesWereKilled(GameDifficulty difficulty)
        {
            AtPeace(difficulty, "Honorable");
            AiToPlayer.SpiesKilled = 3;
            return TrustGainedInATurn();
        }

        [TestMethod]
        public void HarderDifficultySlowsAnAisTrustGainTowardThePlayer()
        {
            float normal = TreatyBoundPacifistGain(GameDifficulty.Normal);
            AssertGreaterThan(normal, 0f, "setup: a pacifist bound by four treaties must warm to the player");

            AssertEqual(0.00001f, normal / 2, TreatyBoundPacifistGain(GameDifficulty.Hard), "Hard halves the gain");
            AssertEqual(0.00001f, normal / 4, TreatyBoundPacifistGain(GameDifficulty.Insane), "Insane quarters it");
        }

        [TestMethod]
        public void DifficultyDoesNotSoftenALossOfTrust()
        {
            float normal = HonorableGainAfterSpiesWereKilled(GameDifficulty.Normal);
            AssertLessThan(normal, 0f, "setup: an honorable AI whose spies we killed must cool toward the player");

            AssertEqual(0.00001f, normal, HonorableGainAfterSpiesWereKilled(GameDifficulty.Insane),
                        "a loss of trust keeps its full speed at every difficulty");
        }
    }
}
