using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class PiratePaymentDirector : Goal
    {
        public const string ID = "PiratePaymentDirector";
        public override string UID => ID;
        private Pirates Pirates;
        public PiratePaymentDirector() : base(GoalType.PiratePaymentDirector)
        {
            Steps = new Func<GoalStep>[]
            {
               UpdatePaymentStatus,
               UpdatePirateActivity,
            };
        }
        public PiratePaymentDirector(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Payment Director for {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire.Pirates;
        }

        GoalStep UpdatePaymentStatus()
        {
            if (Pirates.VictimIsDefeated(TargetEmpire))
                return GoalStep.GoalFailed;

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
                Pirates.TryLevelUp();
            }
            else
            {
                // They did not pay! We will raid them
                Pirates.IncreaseThreatLevelFor(TargetEmpire);
                Pirates.AddGoalRaidDirector(TargetEmpire);
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
                Pirates.IncreaseThreatLevelFor(TargetEmpire);
                return false;
            }

            // They Paid at least once  (or it's our first demand), so we can continue milking money fom them
            Log.Info($"Pirate Payment Director for {TargetEmpire.Name} - Requesting payment");

            string encounterString = "Request Money";
            if (!Pirates.GetRelations(TargetEmpire).Known)
                Pirates.SetAsKnown(TargetEmpire);
            else
                encounterString = "Request More Money";

            if (ResourceManager.GetEncounter(Pirates.Owner, encounterString, out Encounter e))
            {
                e.MoneyRequested = ModifyMoneyRequested(e.MoneyRequested);
                EncounterPopup.Show(Empire.Universe, TargetEmpire, Pirates.Owner, e);
            }

            // We demanded payment for the first time, let the game begin
            if (TargetEmpire.PirateThreatLevel == -1)
                Pirates.IncreaseThreatLevelFor(TargetEmpire);

            return true;
        }

        int ModifyMoneyRequested(int originalPayment)
        {
            float payment = originalPayment * Pirates.Level.LowerBound(1) // Pirates own level
                                            * TargetEmpire.DifficultyModifiers.PiratePayModifier
                                            * TargetEmpire.GetPlanets().Count / 3;

            return payment.RoundTo10();
        }

        bool Paid => !Pirates.GetRelations(TargetEmpire).AtWar;
    }
}