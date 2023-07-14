using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class RemnantEngagements : Goal
    {
        Remnants Remnants => Owner.Remnants;

        [StarDataConstructor]
        public RemnantEngagements(Empire owner) : base(GoalType.RemnantEngagements, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateFirstPortal,
                NotifyPlayer,
                MonitorAndEngage
            };
            if (owner != null && Remnants.Verbose)
                Log.Info(ConsoleColor.Green, $"---- Remnants: New {Owner.Name} Story: {Remnants.Story} ----");
        }

        void EngageEmpire(Ship[] portals)
        {
            if (!Remnants.CanDoAnotherEngagement())
                return;

            if (!Remnants.FindValidTarget(out Empire target))
                return;

            Ship portal = Remnants.Owner.Random.Item(portals);
            Owner.AI.AddGoal(new RemnantEngageEmpire(Owner, portal, target));
        }

        GoalStep CreateFirstPortal()
        {
            if (!Remnants.CreatePortal())
            {
                Log.Warning($"Could not create a portal for {Owner.data.Name}, they will not be activated!");
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
                Owner.SetAsDefeated();
                Owner.Universe.Notifications.AddEmpireDiedNotification(Owner);
                return GoalStep.GoalFailed;
            }

            if (Remnants.TryLevelUpByDate(out int newLevel) 
                && newLevel % Owner.DifficultyModifiers.RemnantPortalCreationMod == 0
                && Remnants.CreatePortal())
            {
                Owner.Universe.Notifications.AddRemnantsNewPortal(Owner);
            }

            EngageEmpire(portals);
            return GoalStep.TryAgain;
        }
    }
}