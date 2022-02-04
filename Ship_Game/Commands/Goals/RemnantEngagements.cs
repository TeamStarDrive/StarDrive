using System;
using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    public class RemnantEngagements : Goal
    {
        public const string ID = "RemnantEngagements";
        public override string UID => ID;
        private Remnants Remnants;

        public RemnantEngagements(int id, UniverseState us)
            : base(GoalType.RemnantEngagements, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateFirstPortal,
                NotifyPlayer,
                MonitorAndEngage
            };
        }

        public RemnantEngagements(Empire owner)
            : this(owner.Universum.CreateId(), owner.Universum)
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
            if (!Remnants.CanDoAnotherEngagement())
                return;

            if (!Remnants.FindValidTarget(out Empire target))
                return;

            Remnants.Goals.Add(new RemnantEngageEmpire(empire, portals.RandItem(), target));
        }

        GoalStep CreateFirstPortal()
        {
            if (!Remnants.CreatePortal())
            {
                Log.Warning($"Could not create a portal for {empire.data.Name}, they will not be activated!");
                return GoalStep.GoalFailed;
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep NotifyPlayer()
        {
            // todo need to remove this step in next remnant iteration
            return GoalStep.GoToNextStep;
        }

        GoalStep MonitorAndEngage()
        {
            if (!Remnants.GetPortals(out Ship[] portals))
            {
                empire.SetAsDefeated();
                empire.Universum.Notifications.AddEmpireDiedNotification(empire);
                return GoalStep.GoalFailed;
            }

            if (Remnants.TryLevelUpByDate(out int newLevel) && newLevel == 10)
            {
                if (Remnants.CreatePortal()) // Second portal at level 10
                    empire.Universum.Notifications.AddRemnantsNewPortal(empire);
            }

            EngageEmpire(portals);
            return GoalStep.TryAgain;
        }
    }
}