using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantStoryBalancers : Goal
    {
        public const string ID = "RemnantStoryBalancers";
        public override string UID => ID;
        private Remnants Remnants;

        public RemnantStoryBalancers() : base(GoalType.RemnantStoryBalancers)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateFirstPortal,
                NotifyPlayer,
                MonitorAndEngage
            };
        }

        public RemnantStoryBalancers(Empire owner) : this()
        {
            empire = owner;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {empire.Name} Story: Ancient Balancers ----");
        }

        public sealed override void PostInit()
        {
            Remnants = empire.Remnants;
        }

        void EngageStrongest()
        {
            if (!Remnants.CanDoAnotherEngagement(out _))
                return;

            Empire strongest = EmpireManager.MajorEmpires.FindMax(e => e.CurrentMilitaryStrength);
            Remnants.Goals.Add(new RemnantBalancersEngage(empire, strongest));
        }

        bool CreatePortal()
        {
            if (!Remnants.CreatePortal(out Ship portal, out string systemName))
                return false;

            Remnants.Goals.Add(new RemnantPortal(empire, portal, systemName));
            return true;
        }

        GoalStep CreateFirstPortal()
        {
            if (!CreatePortal())
            {
                Log.Warning($"Could not create a portal for {empire.data.Name}, they will not be activated!");
                return GoalStep.GoalFailed;
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep NotifyPlayer()
        {
            // todo notify player of story development
            return GoalStep.GoToNextStep;
        }

        GoalStep MonitorAndEngage()
        {
            if (Remnants.NumPortals() == 0)
            {
                empire.SetAsDefeated();
                Empire.Universe.NotificationManager.AddEmpireDiedNotification(empire);
                return GoalStep.GoalFailed;
            }

            if (Remnants.TryLevelUpByDate(out int newLevel) && newLevel == 10)
                CreatePortal(); // Second portal in level 10

            EngageStrongest();
            return GoalStep.TryAgain;
        }
    }
}