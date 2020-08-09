using System;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public abstract class GoalBase
    {
        public Guid ID = Guid.NewGuid();
        public string OwnerName;
        protected Empire Owner;
        public int Step;
        public bool Held;
        public string StepName => Steps[Step].Method.Name;
        protected bool MainGoalCompleted;
        protected Func<GoalStep>[] Steps = Empty<Func<GoalStep>>.Array;
        protected Func<bool> Holding;
        readonly string GoalName;

        public abstract string UID { get; }

        public override string ToString() => $"{GoalName} Goal.{UID}";

        protected GoalBase() { }

        protected GoalBase(GoalBase source, string goalName)
        {
            ID                = source.ID;
            OwnerName         = source.OwnerName;
            Owner             = source.Owner;
            Step              = source.Step;
            Held              = source.Held;
            MainGoalCompleted = source.MainGoalCompleted;
            Steps             = source.Steps;
            Holding           = source.Holding;
            GoalName          = goalName;
        }

        protected GoalBase(string goalName)
        {
            GoalName = goalName;
        }

        protected GoalStep DummyStepTryAgain() => GoalStep.TryAgain;
        protected GoalStep DummyStepGoalComplete() => GoalStep.GoalComplete;
        protected GoalStep WaitMainGoalCompletion()
        {
            if (MainGoalCompleted)
            {
                MainGoalCompleted = false;
                if (Step == Steps.Length - 1)
                    return GoalStep.GoalComplete;
                return GoalStep.GoToNextStep;
            }
            return GoalStep.TryAgain;
        }

        public void NotifyMainGoalCompleted()
        {
            MainGoalCompleted = true;
        }

        // @note Goals are mainly evaluated during Empire update
        public virtual GoalStep Evaluate()
        {
            // CG hrmm i guess this should just be part of the goal enum.
            // But that will require more cleanup of the goals.
            if (Holding?.Invoke() == true)
                return GoalStep.TryAgain;

            if ((uint)Step >= Steps.Length)
            {
                Log.Error($"{GoalName} invalid Goal.Step: {Step}, Steps.Length: {Steps.Length}");
                RemoveThisGoal(); // don't crash, just remove the step
                return GoalStep.GoalFailed;
            }

            GoalStep result = Steps[Step].Invoke();
            switch (result)
            {
                case GoalStep.GoToNextStep: ++Step; break;
                case GoalStep.TryAgain: break;
                case GoalStep.GoalComplete:
                case GoalStep.GoalFailed:
                    RemoveThisGoal();
                    break;
                case GoalStep.RestartGoal:
                    Step = 0;
                    break;
            }
            return result;
        }

        protected abstract void RemoveThisGoal();

        public void AdvanceToNextStep()
        {
            ++Step;
            if ((uint)Step >= Steps.Length)
            {
                Log.Error($"{GoalName} invalid Goal.Step: {Step}, Steps.Length: {Steps.Length}");
                RemoveThisGoal();
            }
        }

        public void ChangeToStep(Func<GoalStep> step)
        {
            if (!Steps.Contains(step))
            {
                Log.Error($"{GoalName} invalid Goal.Step: {step}, Steps.Length: {Steps.Length}");
                RemoveThisGoal();
            }
            Step = Steps.IndexOf(step);
        }

        protected abstract void RestoreFromSave(string ownerName);

    }
}