using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class CorsairPaymentDirector : Goal
    {
        public const string ID = "CorsairPaymentDirector";
        public override string UID => ID;
       private Pirates Corsairs;
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

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- New Corsair Payment Director for {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Corsairs = empire.Pirates;
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
                Corsairs.TryLevelUp();
            }
            else
            {
                // They did not pay! We will raid them
                Corsairs.IncreaseThreatLevelFor(TargetEmpire);
                Corsairs.AddGoalCorsairRaidDirector(TargetEmpire);
            }

            return GoalStep.RestartGoal;
        }

        bool RequestPayment()
        {
            // Every 10 years, the pirates will demand new payment or immediately if the threat level is -1 (initial)
            if (Empire.Universe.StarDate % 10 > 0 && TargetEmpire.PirateThreatLevel > -1)
                return false;

            // If they did not pay, don't ask for another payment, let them crawl to
            // us when they are ready to pay and increase out threat level to them
            if (!Paid)
            {
                Corsairs.IncreaseThreatLevelFor(TargetEmpire);
                return false;
            }

            // They Paid at least once  (or it's our first demand), so we can continue milking money fom them
            Log.Info($"Pirate Payment Director for {TargetEmpire.Name} - Requesting payment");

            string encounterString = "Request Money";
            if (!Corsairs.GetRelations(TargetEmpire).Known)
                Corsairs.SetAsKnown(TargetEmpire);
            else
                encounterString = "Request More Money";

            if (ResourceManager.GetEncounter(Corsairs.Owner, encounterString, out Encounter e))
            {
                e.MoneyRequested = ModifyMoneyRequested(e.MoneyRequested);
                EncounterPopup.Show(Empire.Universe, TargetEmpire, Corsairs.Owner, e);
            }

            // We demanded payment for the first time, let the game begin
            if (TargetEmpire.PirateThreatLevel == -1)
                Corsairs.IncreaseThreatLevelFor(TargetEmpire);

            return true;
        }

        int ModifyMoneyRequested(int originalPayment)
        {
            float payment = originalPayment * Corsairs.Level.LowerBound(1) // Pirates own level
                                            * TargetEmpire.DifficultyModifiers.PiratePayModifier
                                            * TargetEmpire.GetPlanets().Count / 3;

            return payment.RoundTo10();
        }

        bool Paid => !Corsairs.GetRelations(TargetEmpire).AtWar;
    }
}