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

                    return GoalStep.GoToNextStep; // consturction ship managed to deploy the orbital
                }

                Log.Warning($"Mining Ops Goal: {MiningStation.Name} is not a mining station");
                return GoalStep.GoalFailed;
            }
            else if (ConstructionGoalInProgress)
            {
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
            if (availableConsumables <= 0)
            {
                AddMiningStationPlan(Plan.ExoticStationNoSupply);
                AddSupplyDeficit(MiningStation.TotalRefining);
                return;
            }

            float numRawResources = MiningStation.GetOtherCargo(ResourceCargeName);
            MiningStation.Carrier.MiningBays.ProcessMiningBays(numRawResources);
            if (numRawResources <= 0 || MiningStation.Loyalty != TargetPlanet.Mining.Owner)
            {
                AddMiningStationPlan(Plan.MiningStationIdle);
                return;
            }

            float maximumRawResources = MiningStation.TotalRefining.UpperBound(numRawResources);
            float consumablesNeeded = maximumRawResources * GlobalStats.Defaults.MiningStationFoodPerOneRefining;
            float consumeableRatio = availableConsumables / consumablesNeeded;
            float rawResourcesToRefine = maximumRawResources * consumeableRatio.UpperBound(1);
            float consumablesToConsume = rawResourcesToRefine * GlobalStats.Defaults.MiningStationFoodPerOneRefining;
            float refinedResources = rawResourcesToRefine * TargetPlanet.Mining.RefiningRatio;

            InSupplyChain = true;
            if (Owner.NonCybernetic)
                MiningStation.UnloadFood(consumablesToConsume);
            else
                MiningStation.UnloadProduction(consumablesToConsume);

            MiningStation.UnloadCargo(ResourceCargeName, rawResourcesToRefine);

            // TODO - add the refinedResources to the empire exotic resource class

            AddSupplyDeficit(-consumablesToConsume);
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
            MiningStation.RefiningCargoSpaceMax - availableConsumables > Owner.AverageFreighterCargoCap
            || availableConsumables / MiningStation.CargoSpaceMax < 0.25f;

        bool ConstructionGoalInProgress => Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetPlanet));
    }
}
