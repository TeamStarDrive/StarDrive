using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantPortal : Goal
    {
        public const string ID = "RemnantPortal";
        public override string UID => ID;
        private Remnants Remnants;
        private Ship Portal;

        public RemnantPortal() : base(GoalType.RemnantPortal)
        {
            Steps = new Func<GoalStep>[]
            {
                CallGuardians,
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

        GoalStep CallGuardians()
        {
            Remnants.CallGuardians(Portal);
            return GoalStep.GoToNextStep;
        }

        GoalStep GenerateProduction()
        {
            if (Portal == null || !Portal.Active)
                return GoalStep.GoalFailed;

            Remnants.OrderEscortPortal(Portal);
            float production = Empire.Universe.StarDate - 1000; // Stardate 1100 yields 100, 1200 yields 200, etc.
            production      *= empire.DifficultyModifiers.RemnantResourceMod;
            production      *= (int)(CurrentGame.GalaxySize + 1) * 2 * CurrentGame.StarsModifier / EmpireManager.MajorEmpires.Length;
            Remnants.TryGenerateProduction(production);
            return GoalStep.TryAgain;
        }
    }
}