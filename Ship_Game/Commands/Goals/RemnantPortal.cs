using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantPortal : Goal
    {
        public const string ID     = "RemanantPortal";
        public override string UID => ID;
        private Remnants Remnants;
        private Ship     Portal;

        public RemnantPortal() : base(GoalType.PirateBase)
        {
            Steps = new Func<GoalStep>[]
            {
               NotifyPlayer,
               GenerateProduction
            };
        }

        public RemnantPortal(Empire owner, Ship portal, string systemName) : this()
        {
            empire     = owner;
            TargetShip = portal;
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {empire.Name} Portal in {systemName} ----");
        }

        public sealed override void PostInit()
        {
            Remnants = empire.Remnants;
            Portal   = TargetShip;
        }

        GoalStep NotifyPlayer()
        {
            return GoalStep.GoToNextStep;
        }

        GoalStep GenerateProduction()
        {
            if (Portal == null || !Portal.Active)
                return GoalStep.GoalFailed;

            if (!Portal.InCombat)
                Remnants.GenerateProduction(Empire.Universe.StarDate - 1000);

            return GoalStep.TryAgain;
        }
    }
}