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
        public const string ID = "CorsairMain";
        public override string UID => ID;
        private Empire Pirates;  // For better code readability, we are the pirates
        public CorsairMain() : base(GoalType.CorsairMain)
        {
            Steps = new Func<GoalStep>[]
            {
               UpdatePaymentStatus,
               StartPirateActivity,
            };
        }
        public CorsairMain(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            TargetEmpire.SetPirateThreatLevel(-1); // at start, the pirates threat level vs. victim is not active
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- New Corsair Main vs. {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire;
        }

        GoalStep UpdatePaymentStatus()
        {
            if (TargetEmpire.GetPlanets().Count < 3 || !RandomMath.RollDice(100)) //  TODO need to be 10, 100 is for testing
                return GoalStep.TryAgain; // Too small for now

            return RequestPayment() ? GoalStep.GoToNextStep : GoalStep.TryAgain;
        }

        GoalStep StartPirateActivity()
        {
            if (Pirates.GetRelations(TargetEmpire).AtWar)
            {
                TargetEmpire.SetPirateThreatLevel(TargetEmpire.PirateThreatLevel + 1);
                Pirates.GetEmpireAI().Goals.Add(new CorsairMissionDirector(Pirates, TargetEmpire));
            }

            return GoalStep.RestartGoal;
        }

        bool RequestPayment()
        {
            // Every 10 years, the pirates will demand new payment or immediately if the threat level is -1 (initial)
            if (Empire.Universe.StarDate % 10 > 0 && TargetEmpire.PirateThreatLevel > -1)
                return false;

            string encounterString = "Request Money";
            if (!TargetEmpire.GetRelations(Pirates).Known)
                TargetEmpire.SetRelationsAsKnown(Pirates);
            else
                encounterString = "Request More Money";

            if (ResourceManager.GetEncounter(Pirates, encounterString, out Encounter e))
            {
                e.MoneyRequested = MoneyRequested(e.MoneyRequested);
                EncounterPopup.Show(Empire.Universe, TargetEmpire, Pirates, e);
            }

            // Now that we demanded money, let the game begin
            TargetEmpire.SetPirateThreatLevel(0);

            return true;
        }

        int MoneyRequested(int originalPayment)
        {
            float payment = originalPayment * Pirates.PirateThreatLevel.LowerBound(1) // Pirates own level
                                            * TargetEmpire.DifficultyModifiers.PiratePayModifier
                                            * TargetEmpire.GetPlanets().Count / 3;

            return payment.RoundTo10();
        }
    }
}