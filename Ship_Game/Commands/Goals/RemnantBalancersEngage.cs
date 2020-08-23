using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantBalancersEngage : Goal
    {
        public const string ID = "RemnantBalancersEngage";
        public override string UID => ID;
        public Planet TargetPlanet;
        private Remnants Remnants;
        private Ship Portal;

        public RemnantBalancersEngage() : base(GoalType.RemnantBalancersEngage)
        {
            Steps = new Func<GoalStep>[]
            {
                SelectFirstTargetPlanet,
                SelectPortalToSpawnFrom
            };
        }

        public RemnantBalancersEngage(Empire owner, Empire target) : this()
        {
            empire       = owner;
            TargetEmpire = target;
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {empire.Name} Engagement: Ancient Balancers for {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Remnants     = empire.Remnants;
            TargetPlanet = ColonizationTarget;
            Portal       = TargetShip;

        }

        public override bool IsRaid => true;

        void EngageStrongest()
        {
            if (!Remnants.CanDoAnotherEngagement(out _))
                return;

            Empire strongest = EmpireManager.MajorEmpires.FindMax(e => e.CurrentMilitaryStrength);
        }

        GoalStep SelectFirstTargetPlanet()
        {
            int desiredPlanetLevel = (RandomMath.RollDie(5) - 5 + Remnants.Level).LowerBound(1);
            var potentialPlanets   = TargetEmpire.GetPlanets().Filter(p => p.Level == desiredPlanetLevel);
            if (potentialPlanets.Length == 0) // Try lower level planets if not found exact level
                potentialPlanets = TargetEmpire.GetPlanets().Filter(p => p.Level < desiredPlanetLevel);

            if (potentialPlanets.Length == 0)
                return GoalStep.GoalFailed; // Could not find a target planet

            ColonizationTarget = potentialPlanets.RandItem();
            TargetPlanet       = ColonizationTarget; // We will ued TargetPlanet for better readability
            return GoalStep.GoToNextStep;
        }

        GoalStep SelectPortalToSpawnFrom()
        {
            if (!Remnants.GetPortals(out Ship[] portals))
                return GoalStep.GoalFailed;

            Portal     = portals.FindMin(s => s.Center.Distance(TargetPlanet.Center));
            TargetShip = Portal; // Save compatibility
            return GoalStep.GoToNextStep;
        }
    }
}