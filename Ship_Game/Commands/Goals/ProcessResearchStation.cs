using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;
using static Ship_Game.AI.ShipAI;

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

        public override bool IsResearchStationGoal(ExplorableGameObject body) 
            => body != null && (TargetPlanet == body || TargetSystem == body);

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
                if (ResearchStation.IsResearchStation)
                {
                    if (Owner.isPlayer)
                    {
                        Owner.Universe.Notifications
                            .AddResearchStationBuiltNotification(ResearchStation, TargetSystem != null ? TargetSystem 
                                                                                                       : TargetPlanet);
                    }

                    return GoalStep.GoToNextStep; // consturction ship managed to deploy the orbital
                }

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

            if (WasOwnerChanged(out GoalStep ownerChanged))
                return ownerChanged;

            if (PlanetNoLongerReseachable(out GoalStep noLongerResearchable))
                return noLongerResearchable;

            CreateSupplyGoalIfNeeded();
            AddResearch(ResearchStation.GetProduction());
            return GoalStep.TryAgain;
        }

        void AddResearch(float availableProduction)
        {
            if (availableProduction <= 0)
            {
                AddResearchStationPlan(Plan.ResearchStationNoSupply);
                return;
            }

            if (!Owner.Research.HasTopic)
            {
                AddResearchStationPlan(Plan.ResearchStationIdle);
                return;
            }

            float upperbound = availableProduction / ProductionPerResearch;
            float researchToAdd = ResearchStation.ResearchPerTurn.UpperBound(upperbound);
            ResearchStation.UnloadProduction(researchToAdd * ProductionPerResearch);
            Owner.Research.AddResearchStationResearchPerTurn(researchToAdd);
            if (ResearchStation.AI.OrderQueue.PeekFirst?.Plan != Plan.ResearchStationResearching)
                AddResearchStationPlan(Plan.ResearchStationResearching);
        }

        void CreateGoalForNewOwner(Empire newOwner)
        {
            if (ResearchingPlanet)
                newOwner.AI.AddGoalAndEvaluate(new ProcessResearchStation(newOwner, TargetPlanet, ResearchStation));
            else
                newOwner.AI.AddGoalAndEvaluate(new ProcessResearchStation(newOwner, TargetSystem, ResearchStation.Position, ResearchStation));
        }

        void CreateSupplyGoalIfNeeded()
        {
            if (ResearchStation.Supply.InTradeBlockade)
                return;

            if (NeedsProduction && !Owner.AI.HasGoal(g => g.IsSupplyingGoodsToStationStationGoal(ResearchStation)))
                Owner.AI.AddGoal(new SupplyGoodsToStation(Owner, ResearchStation, Goods.Production));
        }

        bool WasOwnerChanged(out GoalStep step)
        {
            step = GoalStep.TryAgain;
            if (ResearchStation.Loyalty != Owner) // Boarded or gifted
            {
                Empire newOwner = ResearchStation.Loyalty;
                if (TargetPlanet?.CanBeResearchedBy(newOwner) == true || TargetSystem?.CanBeResearchedBy(newOwner) == true)
                {
                    CreateGoalForNewOwner(newOwner);
                }
                else
                {
                    ResearchStation.DisengageExcessTroops(ResearchStation.TroopCount);
                    ResearchStation.QueueTotalRemoval();
                    if (newOwner.isPlayer)
                        newOwner.Universe.Notifications.AddExcessResearchStationRemoved(ResearchStation);
                }

                step = GoalStep.GoalComplete;
                return true;
            }

            return false;
        }

        bool PlanetNoLongerReseachable(out GoalStep step)
        {
            step = GoalStep.TryAgain;
            if (ResearchingStar || TargetPlanet.IsResearchable)
                return false;

            ResearchStation.QueueTotalRemoval();
            if (Owner.isPlayer)
                Owner.Universe.Notifications.AddResearchStationRemoved(TargetPlanet);

            step = GoalStep.GoalComplete;
            return true;
        }

        void AddResearchStationPlan(Plan plan)
        {
            if (!ResearchStation.InCombat && !ResearchStation.DoingRefit)
                ResearchStation.AI.AddResearchStationPlan(plan);
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
