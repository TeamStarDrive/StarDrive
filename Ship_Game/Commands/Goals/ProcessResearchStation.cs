using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class ProcessResearchStation : Goal
    {
        [StarData] public SolarSystem TargetSystem;
        [StarData] public override Planet TargetPlanet { get; set; }
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] public Vector2 StaticBuildPos { get; set; }

        Ship ResearchStation => TargetShip;

        public override bool IsResearchStationGoal(Planet planet) => TargetPlanet == planet;

        public override bool IsResearchStationGoal(SolarSystem system) => TargetSystem == system && TargetPlanet == null;

        [StarDataConstructor]
        public ProcessResearchStation(Empire owner) : base(GoalType.ProcessResearchStation, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildStationConstructor,
                WaitForConstructor,
                Research
            };
        }

        public ProcessResearchStation(Empire owner, SolarSystem system, Vector2 buildPos, Ship researchStation = null)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = system;
            Owner = owner;
            StaticBuildPos = researchStation == null ? buildPos : researchStation.Position;
            
            if (researchStation != null)
                ChangeToStep(Research);
        }

        public ProcessResearchStation(Empire owner, Planet planet, Ship researchStation = null)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = planet.ParentSystem;
            TargetPlanet = planet;
            Owner = owner;

            if (researchStation != null)
                ChangeToStep(Research);
        }

        GoalStep BuildStationConstructor()
        {
            if (ResearchStation != null) // We got
                return GoalStep.GoToNextStep;

            IShipDesign bestResearchStation = ResourceManager.Ships.GetDesign(Owner.data.ResearchStation, throwIfError: true); 
            if (!Owner.isPlayer || Owner.AutoPickBestResearchStation)
                bestResearchStation = ShipBuilder.PickResearchStation(Owner);

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, bestResearchStation, out Planet planetToBuildAt))
                return GoalStep.TryAgain;

            if (TargetPlanet != null)
                Owner.AI.AddGoal(new BuildOrbital(planetToBuildAt, TargetPlanet, bestResearchStation.Name, Owner));
            else
                Owner.AI.AddGoal(new BuildOrbital(planetToBuildAt, TargetSystem, bestResearchStation.Name, Owner, StaticBuildPos));
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForConstructor()
        {
            if (ResearchStation != null) // Research Station was deployed by a consturctor
            {
                // TODO - add ship plan so it could display text on current actions
                if (ResearchStation.IsResearchStation)
                    return GoalStep.GoToNextStep; // consturction ship managed to deploy the orbital

                Log.Warning($"Research Station Goal: {ResearchStation.Name} is not a research station");
                return GoalStep.GoalFailed;

            }
            else if (ConstructionGoalInProgress)
            {
                return GoalStep.TryAgain;
            }

            return GoalStep.GoalFailed; // construction goal was canceled
        }

        GoalStep Research()
        {
            if (ResearchStation == null || !ResearchStation.Active)
                return GoalStep.GoalFailed;

            // TODO handle multiple stations on the same planet/system

            if (ResearchStation.Loyalty != Owner) // Boarded or gifted
            {
                CreateGoalForNewOwner(ResearchStation.Loyalty);
                return GoalStep.GoalComplete;
            }

            CreateSupplyGoalIfNeeded();
            AddResearch(ResearchStation.GetProduction());
            return GoalStep.TryAgain;
        }

        void AddResearch(float availableProduction)
        {
            if (availableProduction <= 0 || !Owner.Research.HasTopic) 
                return;

            float upperbound = availableProduction / ProductionPerResearch;
            float researchToAdd = ResearchStation.ResearchPerTurn.UpperBound(upperbound);
            ResearchStation.UnloadProduction(researchToAdd * ProductionPerResearch);
            Owner.Research.AddResearchStationResearchPerTurn(researchToAdd);
        }

        void CreateGoalForNewOwner(Empire newOwner)
        {
            if (ResearchingPlanet)
                newOwner.AI.AddGoal(new ProcessResearchStation(newOwner, TargetPlanet, ResearchStation));
            else
                newOwner.AI.AddGoal(new ProcessResearchStation(newOwner, TargetSystem, ResearchStation.Position, ResearchStation));
        }

        void CreateSupplyGoalIfNeeded()
        {
            if (ResearchStation.Supply.InTradeBlockade)
                return;

            if (NeedsProduction && !Owner.AI.HasGoal(g => g.IsSupplyingGoodsToStationStationGoal(ResearchStation)))
                Owner.AI.AddGoal(new SupplyGoodsToStation(Owner, ResearchStation, Goods.Production));
        }

        float ProductionPerResearch => GlobalStats.Defaults.ResearchStationProductionPerResearch;
        bool ResearchingStar => TargetPlanet == null;
        bool ResearchingPlanet => !ResearchingStar;
        bool NeedsProduction => (ResearchStation.CargoSpaceFree > Owner.AverageFreighterCargoCap 
            || ResearchStation.CargoSpaceUsed / ResearchStation.CargoSpaceMax < 0.5f);

        bool ConstructionGoalInProgress =>
            (ResearchingPlanet && Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetPlanet))
            || (ResearchingStar && Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetSystem))));
    }
}
