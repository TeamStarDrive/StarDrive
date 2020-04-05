using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairPaymentDirector : Goal
    {
        public const string ID = "CorsairPaymentDirector";
        public override string UID => ID;
        private Empire Pirates;  // For better code readability, we are the pirates
        public CorsairPaymentDirector() : base(GoalType.CorsairPaymentDirector)
        {
            Steps = new Func<GoalStep>[]
            {
               UpdatePaymentStatus,
               UpdatePirateActivity,
            };
        }
        public CorsairPaymentDirector(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            TargetEmpire.SetPirateThreatLevel(-1); // at start, the pirates threat level vs. victim is not active
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- New Corsair Payment Director for {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire;
        }

        GoalStep UpdatePaymentStatus()
        {
            if (TargetEmpire.GetPlanets().Count < 3
                || TargetEmpire.GetPlanets().Count == 3 && !RandomMath.RollDice(100)) //  TODO need to be 10, 100 is for testing
            {
                return GoalStep.TryAgain; // Too small for now
            }

            return RequestPayment() ? GoalStep.GoToNextStep : GoalStep.TryAgain;
        }

        GoalStep UpdatePirateActivity()
        {
            if (Paid)
            {
                // Ah, so they paid us,  we can use this money to expand our business 
                Pirates.CorsairsTryLevelUp();
            }
            else
            {
                // They did not pay! We will raid them
                TargetEmpire.IncreasePirateThreatLevel();
                Pirates.GetEmpireAI().Goals.Add(new CorsairRaidDirector(Pirates, TargetEmpire));
            }

            /*
            if (Paid)
            {
                // Ah, so they paid us,  we can use this money to expand our business 
                Pirates.CorsairsTryLevelUp();
            }
            else
            {
                // They did not pay! We will raid them
                TargetEmpire.IncreasePirateThreatLevel();

                var goals = Pirates.GetEmpireAI().Goals;
                using (goals.AcquireWriteLock())
                {
                    if (!goals.Any(g => g.type == GoalType.CorsairRaidDirector && g.TargetEmpire == TargetEmpire))
                        Pirates.GetEmpireAI().Goals.Add(new CorsairRaidDirector(Pirates, TargetEmpire));
                }
            }
            */

            return GoalStep.RestartGoal;
        }

        bool RequestPayment()
        {
            // If they did not pay, don't ask for another payment, let them crawl to
            // us when they are ready to pay
            if (!Paid)
                return false;

            // Every 10 years, the pirates will demand new payment or immediately if the threat level is -1 (initial)
            if (Empire.Universe.StarDate % 10 > 0 && TargetEmpire.PirateThreatLevel > -1)
                return false;

            Log.Info($"Pirate Payment Director for {TargetEmpire.Name} - Requesting payment");

            string encounterString = "Request Money";
            if (!Pirates.GetRelations(TargetEmpire).Known)
                Pirates.SetRelationsAsKnown(TargetEmpire);
            else
                encounterString = "Request More Money";

            if (ResourceManager.GetEncounter(Pirates, encounterString, out Encounter e))
            {
                e.MoneyRequested = ModifyMoneyRequested(e.MoneyRequested);
                EncounterPopup.Show(Empire.Universe, TargetEmpire, Pirates, e);
            }

            // Now that we demanded money, let the game begin
            TargetEmpire.SetPirateThreatLevel(0);

            return true;
        }

        int ModifyMoneyRequested(int originalPayment)
        {
            float payment = originalPayment * Pirates.PirateThreatLevel.LowerBound(1) // Pirates own level
                                            * TargetEmpire.DifficultyModifiers.PiratePayModifier
                                            * TargetEmpire.GetPlanets().Count / 3;

            return payment.RoundTo10();
        }

        bool Paid => !Pirates.GetRelations(TargetEmpire).AtWar;
    }
}