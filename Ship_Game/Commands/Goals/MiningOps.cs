using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using System;
using static Ship_Game.AI.ShipAI;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class MiningOps : Goal
    {
        [StarData] public SolarSystem TargetSystem;
        [StarData] public override Planet TargetPlanet { get; set; }
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] IShipDesign StationToBuild; // specific station to build by player from deep space build menu
        [StarData] readonly Vector2 DynamicBuildPos;
        [StarData] bool InSupplyChain; // This station started getting production
        [StarData] int NumSupplyGoals = 1;
        [StarData] float SupplyDificit;
        Ship MiningStation => TargetShip;
        string ResourceCargeName => TargetPlanet.Mining.CargoId;
        float RemainingConsumables => Owner.NonCybernetic ? MiningStation.GetFood() : MiningStation.GetProduction();

        ExoticBonusType ExoticBonusType => TargetPlanet.Mining.ExoticBonusType;

        public override bool IsMiningOpsGoal(Planet planet) => planet != null && TargetPlanet == planet;

        [StarDataConstructor]
        public MiningOps(Empire owner) : base(GoalType.MiningOps, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildStationConstructor,
                WaitForConstructor,
                Mine
            };
        }

        public MiningOps(Empire owner, Planet planet)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetPlanet = planet;
            Owner = owner;
        }

        // Deep space build orbiting a planet
        public MiningOps(Empire owner, Planet planet, IShipDesign stationToBuild, Vector2 tetherOffset)
            : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetPlanet = planet;
            Owner = owner;
            StationToBuild = stationToBuild;
            DynamicBuildPos = tetherOffset;
        }

        GoalStep BuildStationConstructor()
        {
            if (StationToBuild == null)
            {
                StationToBuild = !Owner.isPlayer || Owner.AutoPickBestMiningStation
                    ? ShipBuilder.PickResearchStation(Owner) // TODO - create mining station picker
                    : ResourceManager.Ships.GetDesign(Owner.data.MiningStation, throwIfError: true); // TODO - do this for mining
            }

            Owner.AddInProgressMiningsStation(ExoticBonusType);
            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, StationToBuild, out Planet planetToBuildAt, portQuality: 1f))
                return GoalStep.TryAgain;

            Owner.AI.AddGoal(new BuildOrbital(planetToBuildAt, TargetPlanet, StationToBuild.Name, Owner, DynamicBuildPos));
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForConstructor()
        {
            if (MiningStation != null) // Mining Station was deployed by a consturctor
            {
                if (MiningStation.IsMiningStation)
                {
                    if (Owner.isPlayer)
                        Owner.Universe.Notifications.AddMiningStationBuiltNotification(MiningStation,TargetPlanet);

                    Owner.AddBuiltMiningStation(ExoticBonusType);
                    return GoalStep.GoToNextStep; // consturction ship managed to deploy the orbital
                }

                Log.Warning($"Mining Ops Goal: {MiningStation.Name} is not a mining station");
                return GoalStep.GoalFailed;
            }
            else if (ConstructionGoalInProgress)
            {
                Owner.AddInProgressMiningsStation(ExoticBonusType);
                return GoalStep.TryAgain;
            }

            return GoalStep.GoalFailed; // construction goal was canceled
        }

        GoalStep Mine()
        {
            if (MiningStation == null || !MiningStation.Active)
                return GoalStep.GoalFailed;

            //if (WasOwnerChanged(out GoalStep ownerChanged))
                //return ownerChanged;

            // Factions do not mine (for now) and pirate owners will set scuttle (see PiratePostChangeLoyalty)
            if (!Owner.IsFaction)
            {
                RefineResources(RemainingConsumables);
                CreateSupplyGoalIfNeeded();
                RefitifNeeded();
                CallForHelpIfNeeded();
            }

            return GoalStep.TryAgain;
        }

        void RefineResources(float availableConsumables)
        {
            MiningStation.Carrier.MiningBays.UpdateIsRefining(0);
            Owner.AddBuiltMiningStation(ExoticBonusType);
            float maxRefiningPotential = MiningStation.TotalRefining * TargetPlanet.Mining.RefiningRatio * Owner.data.RefiningRatioMultiplier;
            Owner.AddMaxPotentialRefining(ExoticBonusType, maxRefiningPotential);

            if (availableConsumables <= 0)
            {
                AddMiningStationPlan(Plan.ExoticStationNoSupply);
                AddSupplyDeficit(MiningStation.TotalRefining);
                MiningStation.Carrier.MiningBays.DestroyEmmiters();
                return;
            }

            float numRawResources = MiningStation.GetOtherCargo(ResourceCargeName);
            float numRefiningNeeded = Owner.GetRefiningNeeded(ExoticBonusType);
            MiningStation.Carrier.MiningBays.ProcessMiningBays(numRawResources);

            if (numRefiningNeeded <= 0 || numRawResources <= 0 || MiningStation.Loyalty != TargetPlanet.Mining.Owner)
            {
                AddMiningStationPlan(Plan.MiningStationIdle);
                MiningStation.Carrier.MiningBays.DestroyEmmiters();
                return;
            }

            Owner.AddActiveMiningStation(ExoticBonusType);
            InSupplyChain = true;
            float maximumRawResources = MiningStation.TotalRefining.UpperBound(numRawResources);
            float consumablesNeeded = maximumRawResources * GlobalStats.Defaults.MiningStationFoodPerOneRefining;
            float consumeableRatio = availableConsumables / consumablesNeeded;
            float maxRawResourcesToRefine = maximumRawResources * consumeableRatio.UpperBound(1);
            float maxConsumablesToConsume = maxRawResourcesToRefine * GlobalStats.Defaults.MiningStationFoodPerOneRefining;
            float maxRefinedResources = maxRawResourcesToRefine * TargetPlanet.Mining.RefiningRatio * Owner.data.RefiningRatioMultiplier;
            float protuctionRatio = (numRefiningNeeded / maxRefinedResources).UpperBound(1);
            Owner.AddRefinedResource(ExoticBonusType, maxRefinedResources * protuctionRatio);
            MiningStation.Carrier.MiningBays.UpdateIsRefining(protuctionRatio);

            if (Owner.NonCybernetic)
                MiningStation.UnloadFood(maxConsumablesToConsume * protuctionRatio);
            else
                MiningStation.UnloadProduction(maxConsumablesToConsume * protuctionRatio);

            MiningStation.UnloadCargo(ResourceCargeName, maxRawResourcesToRefine * protuctionRatio);
            AddSupplyDeficit(-maxConsumablesToConsume * protuctionRatio);
            if (MiningStation.AI.OrderQueue.PeekFirst?.Plan != Plan.MiningStationRefining)
                AddMiningStationPlan(Plan.MiningStationRefining);
        }

        void CreateSupplyGoalIfNeeded()
        {
            if (MiningStation.Supply.InTradeBlockade)
                return;

            if (NeedsConsumables(RemainingConsumables)
                && Owner.AI.CountGoals(g => g.IsSupplyingGoodsToStationStationGoal(MiningStation)) < NumSupplyGoals)
            {
                Owner.AI.AddGoal(new SupplyGoodsToStation(Owner, MiningStation, Owner.IsCybernetic ? Goods.Production : Goods.Food));
            }
        }

        void RefitifNeeded()
        {
            if (NeedsRefit(out IShipDesign stationToRefit))
                Owner.AI.AddGoalAndEvaluate(new RefitOrbital(MiningStation, stationToRefit, Owner));
        }

        void AddMiningStationPlan(Plan plan)
        {
            if (!MiningStation.InCombat && !MiningStation.DoingRefit)
                MiningStation.AI.AddMiningStationPlan(plan);
        }

        bool NeedsRefit(out IShipDesign betterStation)
        {
            betterStation = null;
            if (Owner.isPlayer && !Owner.AutoBuildMiningStations)
                return false;

            string bestRefit = Owner.isPlayer && !Owner.AutoPickBestMiningStation
                ? Owner.data.CurrentMiningStation
                : Owner.BestMiningStationWeCanBuild.Name;

            if (MiningStation.Name != bestRefit && !Owner.AI.HasGoal(g => g is RefitOrbital && g.OldShip == MiningStation))
                betterStation = ResourceManager.Ships.GetDesign(bestRefit);

            return betterStation != null;
        }

        void CallForHelpIfNeeded()
        {
            SolarSystem system = TargetPlanet?.System ?? TargetSystem;
            if ((system.OwnerList.Count == 0 || system.HasPlanetsOwnedBy(Owner))
                && (MiningStation.HealthPercent < 0.95
                   || system.ShipList.Any(s => s.IsMiningStation && s.Loyalty.IsAtWarWith(MiningStation.Loyalty)))
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
                NumSupplyGoals = ((int)Math.Ceiling(SupplyDificit / Owner.AverageFreighterCargoCap)).Clamped(1, 3);
            }
        }

        bool NeedsConsumables(float availableConsumables) =>
            (MiningStation.CargoSpaceMax - MiningStation.RefiningCargoSpaceMax) - availableConsumables > Owner.AverageFreighterCargoCap
            || availableConsumables / (MiningStation.CargoSpaceMax - MiningStation.RefiningCargoSpaceMax) < 0.5f;

        bool ConstructionGoalInProgress => Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetPlanet));
    }
}
