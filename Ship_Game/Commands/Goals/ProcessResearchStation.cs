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
        [StarData] IShipDesign StationToBuild; // specific station to build by player from deep space build menu
        [StarData] readonly Vector2 DynamicBuildPos;
        [StarData] bool InSupplyChain; // This station started getting production
        [StarData] int NumSupplyGoals = 1;
        [StarData] float SupplyDificit;
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

        public ProcessResearchStation(Empire owner, SolarSystem system, Vector2 buildPos, IShipDesign stationToBuild = null)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = system;
            Owner = owner;
            StaticBuildPos = buildPos;
            StationToBuild = stationToBuild;
        }

        public ProcessResearchStation(Empire owner, Planet planet)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetPlanet = planet;
            Owner = owner;
        }

        // Deep space build orbiting a planet
        public ProcessResearchStation(Empire owner, Planet planet, IShipDesign stationToBuild, Vector2 tetherOffset)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetPlanet = planet;
            Owner = owner;
            StationToBuild = stationToBuild;
            DynamicBuildPos= tetherOffset;
        }
        // This is for when a research station is captured
        public ProcessResearchStation(Empire owner, Ship researchStation): this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetShip = researchStation;
            Planet planet = researchStation.GetTether();
            if (planet != null)
                TargetPlanet = planet;
            else
                TargetSystem = researchStation.System;

            owner.Universe.AddEmpireToResearchableList(owner, TargetSolarBody);
            StaticBuildPos = researchStation.Position;
            Owner = owner;
            ChangeToStep(Research);
        }

        GoalStep BuildStationConstructor()
        {
            if (ResearchStation != null) // We got
                return GoalStep.GoToNextStep;

            if (StationToBuild == null)
            {
                StationToBuild = !Owner.isPlayer || Owner.AutoPickBestResearchStation 
                    ? ShipBuilder.PickResearchStation(Owner) 
                    : ResourceManager.Ships.GetDesign(Owner.data.ResearchStation, throwIfError: true);
            }

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, StationToBuild, out Planet planetToBuildAt, portQuality: 1f))
                return GoalStep.TryAgain;

            if (TargetPlanet != null)
                Owner.AI.AddGoal(new BuildOrbital(planetToBuildAt, TargetPlanet, StationToBuild.Name, Owner, DynamicBuildPos));
            else
                Owner.AI.AddGoal(new BuildOrbital(planetToBuildAt, TargetSystem, StationToBuild.Name, Owner, StaticBuildPos));

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

            // Factions do not research and pirate owners will set scuttle (see PiratePostChangeLoyalty)
            if (!Owner.IsFaction)
            {
                AddResearch(ResearchStation.GetProduction());
                CreateSupplyGoalIfNeeded();
                RefitifNeeded();
                AiCallForHelpIfNeeded();
            }

            return GoalStep.TryAgain;
        }

        void AddResearch(float availableProduction)
        {
            if (availableProduction <= 0)
            {
                AddResearchStationPlan(Plan.ExoticStationNoSupply);
                AddSupplyDeficit(TotalProductionConsumedPerTurn);
                return;
            }

            if (!Owner.Research.HasTopic)
            {
                AddResearchStationPlan(Plan.ResearchStationIdle);
                return;
            }

            float upperbound = availableProduction / ProductionPerResearch;
            float researchToAdd = ResearchStation.ResearchPerTurn.UpperBound(upperbound);
            float prodToConsume = researchToAdd * ProductionPerResearch;
            InSupplyChain = true;
            ResearchStation.UnloadProduction(prodToConsume);
            Owner.Research.AddResearchStationResearchPerTurn(researchToAdd);
            AddSupplyDeficit(-prodToConsume);
            if (ResearchStation.AI.OrderQueue.PeekFirst?.Plan != Plan.ResearchStationResearching)
                AddResearchStationPlan(Plan.ResearchStationResearching);
        }

        void CreateSupplyGoalIfNeeded()
        {
            if (ResearchStation.Supply.InTradeBlockade)
                return;

            if (NeedsProduction
                && Owner.AI.CountGoals(g => g.IsSupplyingGoodsToStationStationGoal(ResearchStation)) < NumSupplyGoals)
            {
                Owner.AI.AddGoal(new SupplyGoodsToStation(Owner, ResearchStation, Goods.Production));
            }
        }

        void RefitifNeeded()
        {
            if (NeedsRefit(out IShipDesign stationToRefit))
                Owner.AI.AddGoalAndEvaluate(new RefitOrbital(ResearchStation, stationToRefit, Owner));
        }

        bool WasOwnerChanged(out GoalStep step)
        {
            step = GoalStep.TryAgain;
            if (ResearchStation.Loyalty != Owner) // Boarded or gifted
            {
                Owner.Universe.RemoveEmpireFromResearchableList(Owner, TargetSolarBody);
                Empire newOwner = ResearchStation.Loyalty;
                if (TargetPlanet?.CanBeResearchedBy(newOwner) == true || TargetSystem?.CanBeResearchedBy(newOwner) == true)
                {
                    newOwner.AI.AddGoalAndEvaluate(new ProcessResearchStation(newOwner, ResearchStation));
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

        bool NeedsRefit(out IShipDesign betterStation)
        {
            betterStation = null;
            if (Owner.isPlayer && !Owner.AutoBuildResearchStations)
                return false;

            string bestRefit = Owner.isPlayer && !Owner.AutoPickBestResearchStation
                ? Owner.data.CurrentResearchStation
                : Owner.BestResearchStationWeCanBuild.Name;

            if (ResearchStation.Name != bestRefit && !Owner.AI.HasGoal(g => g is RefitOrbital && g.OldShip == ResearchStation))
                betterStation = ResourceManager.Ships.GetDesign(bestRefit);

            return betterStation != null;
        }

        void AiCallForHelpIfNeeded()
        {
            if (Owner.isPlayer)
                return;

            SolarSystem system = TargetPlanet?.System ?? TargetSystem;
            if ((system.OwnerList.Count == 0 || system.HasPlanetsOwnedBy(Owner))
                && (ResearchStation.HealthPercent < 0.95 && ResearchStation.AI.BadGuysNear
                   || system.ShipList.Any(s => s.IsResearchStation && s.Loyalty.IsAtWarWith(ResearchStation.Loyalty)))
                && !Owner.HasWarTaskTargetingSystem(system))
            {
                Owner.AddDefenseSystemGoal(system, Owner.KnownEnemyStrengthIn(system), AI.Tasks.MilitaryTaskImportance.Normal);
            }
        }

        void AddSupplyDeficit(float value)
        {
            if (value < 0)
                InSupplyChain = true;

            if (InSupplyChain)
            {
                SupplyDificit = (SupplyDificit + value).LowerBound(0);
                NumSupplyGoals = ((int)Math.Ceiling(SupplyDificit / Owner.AverageFreighterCargoCap)).Clamped(1, 5);
            }
        }

        float ProductionPerResearch => GlobalStats.Defaults.ResearchStationProductionPerResearch;
        float TotalProductionConsumedPerTurn => ResearchStation.ResearchPerTurn * ProductionPerResearch;
        bool ResearchingStar => TargetPlanet == null;
        bool ResearchingPlanet => !ResearchingStar;
        bool NeedsProduction => (ResearchStation.CargoSpaceFree > Owner.AverageFreighterCargoCap 
            || ResearchStation.CargoSpaceUsed / ResearchStation.CargoSpaceMax < 0.5f);

        bool ConstructionGoalInProgress =>
            (ResearchingPlanet && Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetPlanet))
            || (ResearchingStar && Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetSystem))));

        ExplorableGameObject TargetSolarBody => TargetPlanet != null ? TargetPlanet : TargetSystem;
    }
}
