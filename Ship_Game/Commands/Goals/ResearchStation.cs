using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class ResearchStation : Goal
    {
        [StarData] public SolarSystem TargetSystem;
        [StarData] public override Planet TargetPlanet { get; set; }
        [StarData] public sealed override Ship TargetShip { get; set; }

        Ship ResearchStationOrbital => TargetShip;
        float ProductionPerResearch => GlobalStats.Defaults.ResearchStationProductionPerResearch;
        bool ResearchingStar => TargetPlanet == null;
        bool ResearchingPlanet => !ResearchingStar;

        public override bool IsResearchStationGoal(Planet planet) => TargetPlanet == planet;

        public override bool IsResearchStationGoal(SolarSystem system) => TargetSystem == system && TargetPlanet == null;

        [StarDataConstructor]
        public ResearchStation(Empire owner) : base(GoalType.ResearchStation, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildStationConstructor,
                WaitForConstructor,
                Research
            };
        }

        public ResearchStation(Empire owner, SolarSystem system)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = system;
            Owner = owner;
        }

        public ResearchStation(Empire owner, Planet planet)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = planet.ParentSystem;
            TargetPlanet = planet;
            Owner = owner;
        }

        GoalStep BuildStationConstructor()
        {
            IShipDesign bestResearchStation = ResourceManager.Ships.GetDesign(Owner.data.ResearchStation, throwIfError: true); 
            if (!Owner.isPlayer || Owner.AutoPickBestResearchStation)
                bestResearchStation = ShipBuilder.PickResearchStation(Owner);

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, bestResearchStation, out Planet planetToBuildAt))
                return GoalStep.TryAgain;

            Owner.AI.AddGoal(new BuildOrbital(planetToBuildAt, TargetPlanet, bestResearchStation.Name, Owner));
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForConstructor()
        {
            if (ResearchStationOrbital != null && ResearchStationOrbital.IsResearchStation)
            {
                // TODO - add ship plan so it could display text on current actions
                return GoalStep.GoToNextStep; // consturction ship managed to deploy the orbital
            }
            else if (ResearchingPlanet && Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetPlanet))
                   || (ResearchingStar && Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetSystem))))
            {
                return GoalStep.TryAgain; // construction goal in progress
            }

            return GoalStep.GoalFailed; // construction goal was canceled
        }

        GoalStep Research()
        {
            if (ResearchStationOrbital == null || !ResearchStationOrbital.Active)
                return GoalStep.GoalFailed;

            // TODO handle boarding

            // do to handle multiple stations on the same planet/system

            float production = ResearchStationOrbital.GetProduction();
            if (ResearchStationOrbital.CargoSpaceFree > Owner.AverageFreighterCargoCap 
                || production / ResearchStationOrbital.CargoSpaceMax < 0.5f)
            {
                // todo - check if supply goal to this staion exists
                // Todo - create supply cargo goal
            }

            AddResearch(production);
            return GoalStep.TryAgain;
        }

        void AddResearch(float production)
        {
            if (production <= 0) 
                return;

            float upperbound = production / ProductionPerResearch;
            float researchToAdd = ResearchStationOrbital.ResearchPerTurn.UpperBound(upperbound);
            ResearchStationOrbital.UnloadProduction(researchToAdd * ProductionPerResearch);
            Owner.Research.AddResearchStationResearchPerTurn(researchToAdd);
        }
    }
}
