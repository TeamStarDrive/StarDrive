using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairMain : Goal
    {
        public const string ID     = "CorsairMain";
        public override string UID => ID;
        public Empire Player;
        public CorsairMain() : base(GoalType.CorsairAI)
        {
            Steps = new Func<GoalStep>[]
            {
               UpdatePaymentStatus,
               StartPirateActivity,
            };
        }
        public CorsairMain(Empire owner) : this()
        {
            empire = owner;
            Player = EmpireManager.Player;
        }


        GoalStep UpdatePaymentStatus()
        {
            Player = EmpireManager.Player;
            if (Player.GetPlanets().Count < 3 && !RandomMath.RollDice(10))
                return GoalStep.TryAgain; // Too small for now

            return RequestPayment() ? GoalStep.GoToNextStep : GoalStep.TryAgain;
        }

        bool RequestPayment()
        {
            if (Empire.Universe.StarDate % 10 > 0 || Player.PirateThreatLevel > 0)
                return false;

            // Every 10 years, the pirates will demand new payment
            if (ResourceManager.GetEncounter(empire, "First Contact", out Encounter e))
                EncounterPopup.Show(Empire.Universe, Player, empire, e);

            if (Player.PirateThreatLevel == 0)
                Player.SetPirateThreatLevel(Player.PirateThreatLevel + 1, paid: false);

            return true;
        }

        GoalStep StartPirateActivity()
        {
            Player = EmpireManager.Player;
            if (Player.TryGetRelations(empire, out Relationship rel) && rel.AtWar);
            // start raiding

            return GoalStep.RestartGoal;
        }
    }
}