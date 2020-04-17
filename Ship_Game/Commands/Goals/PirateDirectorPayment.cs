using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class PirateDirectorPayment : Goal
    {
        public const string ID = "PirateDirectorPayment";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateDirectorPayment() : base(GoalType.PirateDirectorPayment)
        {
            Steps = new Func<GoalStep>[]
            {
               UpdatePaymentStatus,
               UpdatePirateActivity,
            };
        }

        public PirateDirectorPayment(Empire owner, Empire targetEmpire) : this()
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

            int victimPlanets = TargetEmpire.GetPlanets().Count;

            if (victimPlanets > Pirates.MinimumColoniesForPayment
                || victimPlanets == Pirates.MinimumColoniesForPayment 
                && RandomMath.RollDice(100)) //  TODO need to be 10, 100 is for testing
            {
                return RequestPayment() ? GoalStep.GoToNextStep : GoalStep.TryAgain;
            }

            return GoalStep.TryAgain; // Too small for now
        }

        GoalStep UpdatePirateActivity()
        {
            if (Pirates.PaidBy(TargetEmpire))
            {
                // Ah, so they paid us,  we can use this money to expand our business 
                Pirates.TryLevelUp();
                Pirates.ResetPaymentTimerFor(TargetEmpire);
                Pirates.DecreaseThreatLevelFor(TargetEmpire);
                Pirates.GetRelations(TargetEmpire).Treaty_NAPact       = true;
                TargetEmpire.GetRelations(Pirates.Owner).Treaty_NAPact = true;
            }
            else
            {
                // They did not pay! We will raid them
                Pirates.IncreaseThreatLevelFor(TargetEmpire);
                Pirates.AddGoalDirectorRaid(TargetEmpire);
            }

            return GoalStep.RestartGoal;
        }

        bool RequestPayment()
        {
            // If the timer is done, the pirates will demand new payment or immediately if the threat level is -1 (initial)
            if (Pirates.PaymentTimerFor(TargetEmpire) > 0 && Pirates.ThreatLevelFor(TargetEmpire) > -1)
            {
                Pirates.DecreasePaymentTimerFor(TargetEmpire);
                return false;
            }

            // If the player did not pay, don't ask for another payment, let them crawl to
            // us when they are ready to pay and increase out threat level to them
            if (!Pirates.PaidBy(TargetEmpire) && TargetEmpire.isPlayer)
            {
                Pirates.IncreaseThreatLevelFor(TargetEmpire);
                return false;
            }

            // They Paid at least once  (or it's our first demand), so we can continue milking money fom them
            Log.Info(ConsoleColor.Green,$"Pirates: {empire.Name} Payment Director - Demanding payment from {TargetEmpire.Name}");

            if (!Pirates.GetRelations(TargetEmpire).Known)
                Pirates.SetAsKnown(TargetEmpire);

            if (TargetEmpire.isPlayer)
                Encounter.ShowEncounterPopUpFactionInitiated(Pirates.Owner, Empire.Universe, GetMoneyRequestedModifier());
            else
                DemandMoneyFromAI();

            // We demanded payment for the first time, let the game begin
            if (Pirates.ThreatLevelFor(TargetEmpire) == -1)
                Pirates.IncreaseThreatLevelFor(TargetEmpire);

            return true;
        }

        float GetMoneyRequestedModifier()
        {
            float modifier = (Pirates.ThreatLevelFor(TargetEmpire) + 1).Clamped(1, Pirates.Level)
                             * TargetEmpire.DifficultyModifiers.PiratePayModifier
                             * TargetEmpire.GetPlanets().Count / Pirates.Owner.data.MinimumColoniesForStartPayment;

            return modifier;
        }

        void DemandMoneyFromAI()
        {
            bool error = true; ;
            if (Encounter.GetEncounterForAI(Pirates.Owner, 0, out Encounter e))
            {
                if (e.BaseMoneyDemanded > 0)
                {
                    error             = false;
                    int moneyDemand   = (e.BaseMoneyDemanded * GetMoneyRequestedModifier()).RoundTo10();
                    float chanceToPay = 1 - moneyDemand/TargetEmpire.Money.LowerBound(1);
                    chanceToPay       = chanceToPay.LowerBound(0) * 100 / ((int)CurrentGame.Difficulty+1);
                        
                    if (RandomMath.RollDice(chanceToPay)) // We can expand that with AI personality
                    {
                        TargetEmpire.AddMoney(-e.BaseMoneyDemanded);
                        TargetEmpire.GetEmpireAI().EndWarFromEvent(Pirates.Owner);
                        Log.Info(ConsoleColor.Green, $"Pirates: {empire.Name} Payment Director " +
                                                     $"- got payment from {TargetEmpire.Name}");
                    }
                    else
                    {
                        TargetEmpire.GetEmpireAI().DeclareWarFromEvent(Pirates.Owner, WarType.SkirmishWar);
                        Log.Info(ConsoleColor.Green, $"Pirates: {empire.Name} Payment Director " +
                                                     $"- {TargetEmpire.Name} refused to pay!");
                    }
                }
            }

            if (error)
                Log.Warning($"Could not find BaseMoneyRequest in {Pirates.Owner.Name} encounters for {TargetEmpire.Name}. " +
                            $"Make sure there is a step 0 encounter for {Pirates.Owner.Name} in encounter dialogs and " +
                            $"with <BaseMoneyRequested> xml tag");
        }
    }
}