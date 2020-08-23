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
                CreatePortal,
                NotifyPlayer,
                MonitorState
            };
        }

        public RemnantStoryBalancers(Empire owner) : this()
        {
            empire = owner;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {empire.Name} Story: Ancient Balancers for {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Remnants = empire.Remnants;
        }

        GoalStep CreatePortal()
        {
            if (!Remnants.CreatePortal(out Ship portal))
            {
                Log.Warning($"Could not create a portal for {empire.data.Name}, they will not be activated!");
                return GoalStep.GoalFailed;
            }

            // create the portal goal
            Remnants.Goals.Add(new RemnantPortal(empire, portal, portal.SystemName));
            return GoalStep.GoToNextStep;
        }

        GoalStep NotifyPlayer()
        {
            // notify player of story development
            return GoalStep.GoToNextStep;
        }

        GoalStep MonitorState()
        {
            if (Remnants.NumPortals() > 0)
                return GoalStep.TryAgain;

            empire.SetAsDefeated();
            Empire.Universe.NotificationManager.AddEmpireDiedNotification(empire);
            return GoalStep.GoalFailed;
        }
    }
}