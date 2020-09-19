using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantEngagements : Goal
    {
        public const string ID = "RemnantEngagements";
        public override string UID => ID;
        private Remnants Remnants;

        public RemnantEngagements() : base(GoalType.RemnantEngagements)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateFirstPortal,
                NotifyPlayer,
                MonitorAndEngage
            };
        }

        public RemnantEngagements(Empire owner) : this()
        {
            empire = owner;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {empire.Name} Story: {empire.Remnants.Story} ----");
        }

        public sealed override void PostInit()
        {
            Remnants = empire.Remnants;
        }

        void EngageEmpire(Ship[] portals)
        {
            if (!Remnants.CanDoAnotherEngagement(out _))
                return;

            if (!Remnants.FindValidTarget(portals.RandItem(), out Empire target))
                return;

            Remnants.Goals.Add(new RemnantEngageEmpire(empire, portals.RandItem(), target));
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
            if (!Remnants.GetPortals(out Ship[] portals))
            {
                empire.SetAsDefeated();
                Empire.Universe.NotificationManager.AddEmpireDiedNotification(empire);
                return GoalStep.GoalFailed;
            }

            if (Remnants.TryLevelUpByDate(out int newLevel) && newLevel == 10)
                CreatePortal(); // Second portal at level 10

            EngageEmpire(portals);
            return GoalStep.TryAgain;
        }
    }
}